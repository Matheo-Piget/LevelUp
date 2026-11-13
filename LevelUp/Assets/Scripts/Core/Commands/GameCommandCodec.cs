using System;
using System.Collections.Generic;
using System.IO;
using LevelUp.Utils;

namespace LevelUp.Core
{
    /// <summary>
    /// Sérialisation binaire des <see cref="IGameCommand"/>, sans dépendance :
    /// la même pour le relais réseau, l'historique de l'hôte et la sauvegarde
    /// d'une partie solo. Petit-boutiste (BinaryWriter), donc identique sur
    /// tous les appareils.
    ///
    /// La lecture ne fait JAMAIS confiance aux octets reçus : tailles bornées,
    /// énumérations vérifiées, et toute anomalie retourne null au lieu de lever
    /// une exception ou d'allouer un tableau géant. Les cartes voyagent avec
    /// leurs 4 champs, mais seul l'identifiant fait foi : l'exécuteur retrouve
    /// la vraie carte dans la main du joueur.
    ///
    /// Changer ce format = incrémenter <see cref="GameManager.RulesVersion"/>.
    /// </summary>
    public static class GameCommandCodec
    {
        /// <summary>Taille maximum d'une commande sérialisée (garde-fou réseau / fichier).</summary>
        public const int MaxCommandBytes = 2048;

        private const int MaxMelds = 8;
        private const int MaxCardsPerMeld = 40;

        private enum Kind : byte
        {
            DrawFromDeck = 0,
            DrawFromDiscard = 1,
            LayDown = 2,
            AddToMeld = 3,
            SkipPhase = 4,
            Discard = 5,
            StartNextRound = 6
        }

        // ────────────────────────────────────────────────────
        //  ÉCRITURE
        // ────────────────────────────────────────────────────

        public static byte[] ToBytes(IGameCommand command)
        {
            using MemoryStream stream = new(64);
            using (BinaryWriter writer = new(stream))
            {
                Write(writer, command);
            }
            return stream.ToArray();
        }

        public static void Write(BinaryWriter w, IGameCommand command)
        {
            switch (command)
            {
                case DrawFromDeckCommand c:
                    w.Write((byte)Kind.DrawFromDeck);
                    w.Write(c.PlayerIndex);
                    break;

                case DrawFromDiscardCommand c:
                    w.Write((byte)Kind.DrawFromDiscard);
                    w.Write(c.PlayerIndex);
                    w.Write(c.DiscardPileIndex);
                    break;

                case LayDownLevelCommand c:
                    w.Write((byte)Kind.LayDown);
                    w.Write(c.PlayerIndex);
                    w.Write((byte)c.Melds.Count);
                    foreach (Meld meld in c.Melds)
                    {
                        w.Write((byte)meld.Type);
                        w.Write((byte)meld.Cards.Count);
                        foreach (CardModel card in meld.Cards) WriteCard(w, card);
                    }
                    break;

                case AddToMeldCommand c:
                    w.Write((byte)Kind.AddToMeld);
                    w.Write(c.PlayerIndex);
                    WriteCard(w, c.Card);
                    w.Write(c.MeldOwnerIndex);
                    w.Write(c.MeldIndex);
                    break;

                case SkipPhaseCommand c:
                    w.Write((byte)Kind.SkipPhase);
                    w.Write(c.PlayerIndex);
                    w.Write((byte)c.PhaseToSkip);
                    break;

                case DiscardCommand c:
                    w.Write((byte)Kind.Discard);
                    w.Write(c.PlayerIndex);
                    WriteCard(w, c.Card);
                    w.Write(c.TargetPlayerIndex);
                    break;

                case StartNextRoundCommand c:
                    w.Write((byte)Kind.StartNextRound);
                    w.Write(c.PlayerIndex);
                    w.Write(c.FinishedRound);
                    break;

                default:
                    throw new ArgumentException($"Commande non sérialisable : {command?.GetType().Name}");
            }
        }

        // ────────────────────────────────────────────────────
        //  LECTURE
        // ────────────────────────────────────────────────────

        /// <summary>
        /// Reconstruit une commande. Retourne null si les octets sont invalides
        /// (tronqués, trop longs, valeurs hors bornes, octets en trop).
        /// </summary>
        public static IGameCommand? FromBytes(byte[]? bytes)
        {
            if (bytes == null || bytes.Length == 0 || bytes.Length > MaxCommandBytes) return null;

            try
            {
                using MemoryStream stream = new(bytes, false);
                using BinaryReader reader = new(stream);
                IGameCommand? command = Read(reader);
                return stream.Position == stream.Length ? command : null;
            }
            catch (Exception)
            {
                // Fin de flux prématurée ou donnée illisible : commande refusée.
                return null;
            }
        }

        private static IGameCommand? Read(BinaryReader r)
        {
            byte kind = r.ReadByte();
            int playerIndex = r.ReadInt32();

            switch ((Kind)kind)
            {
                case Kind.DrawFromDeck:
                    return new DrawFromDeckCommand(playerIndex);

                case Kind.DrawFromDiscard:
                    return new DrawFromDiscardCommand(playerIndex, r.ReadInt32());

                case Kind.LayDown:
                {
                    int meldCount = r.ReadByte();
                    if (meldCount > MaxMelds) return null;
                    List<Meld> melds = new(meldCount);
                    for (int m = 0; m < meldCount; m++)
                    {
                        byte meldType = r.ReadByte();
                        int cardCount = r.ReadByte();
                        if (meldType > (byte)MeldType.Flush || cardCount > MaxCardsPerMeld) return null;
                        List<CardModel> cards = new(cardCount);
                        for (int i = 0; i < cardCount; i++)
                        {
                            if (!TryReadCard(r, out CardModel card)) return null;
                            cards.Add(card);
                        }
                        melds.Add(new Meld((MeldType)meldType, cards, playerIndex));
                    }
                    return new LayDownLevelCommand(playerIndex, melds);
                }

                case Kind.AddToMeld:
                {
                    if (!TryReadCard(r, out CardModel card)) return null;
                    int ownerIndex = r.ReadInt32();
                    int meldIndex = r.ReadInt32();
                    return new AddToMeldCommand(playerIndex, card, ownerIndex, meldIndex);
                }

                case Kind.SkipPhase:
                {
                    byte phase = r.ReadByte();
                    if (phase > (byte)TurnPhase.Discard) return null;
                    return new SkipPhaseCommand(playerIndex, (TurnPhase)phase);
                }

                case Kind.Discard:
                {
                    if (!TryReadCard(r, out CardModel card)) return null;
                    return new DiscardCommand(playerIndex, card, r.ReadInt32());
                }

                case Kind.StartNextRound:
                    return new StartNextRoundCommand(playerIndex, r.ReadInt32());

                default:
                    return null;
            }
        }

        // ────────────────────────────────────────────────────
        //  CARTES
        // ────────────────────────────────────────────────────

        private static void WriteCard(BinaryWriter w, CardModel card)
        {
            w.Write(card.Id);
            w.Write(card.Value);
            w.Write((byte)card.Color);
            w.Write((byte)card.Type);
        }

        private static bool TryReadCard(BinaryReader r, out CardModel card)
        {
            int id = r.ReadInt32();
            int value = r.ReadInt32();
            byte color = r.ReadByte();
            byte type = r.ReadByte();

            if (color > (byte)CardColor.Wild || type > (byte)CardType.WildDraw2)
            {
                card = default;
                return false;
            }

            CardType cardType = (CardType)type;
            card = cardType == CardType.Normal
                ? new CardModel(id, value, (CardColor)color)
                : new CardModel(id, cardType);
            return true;
        }
    }
}
