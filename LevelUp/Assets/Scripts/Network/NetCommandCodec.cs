using System.Collections.Generic;
using Unity.Netcode;
using LevelUp.Core;
using LevelUp.Utils;

namespace LevelUp.Network
{
    /// <summary>
    /// Sérialisation binaire des <see cref="IGameCommand"/> pour le transport réseau.
    /// Les cartes sont encodées avec leurs 4 champs (Id, Value, Color, Type) pour
    /// une reconstruction sans état ; l'égalité de CardModel repose sur Id.
    /// </summary>
    public static class NetCommandCodec
    {
        private enum CmdKind : byte
        {
            DrawFromDeck = 0,
            DrawFromDiscard = 1,
            LayDown = 2,
            AddToMeld = 3,
            SkipPhase = 4,
            Discard = 5
        }

        // ────────────────────────────────────────────────────
        //  ÉCRITURE
        // ────────────────────────────────────────────────────

        public static void Write(FastBufferWriter writer, IGameCommand command)
        {
            switch (command)
            {
                case DrawFromDeckCommand c:
                    writer.WriteValueSafe((byte)CmdKind.DrawFromDeck);
                    writer.WriteValueSafe(c.PlayerIndex);
                    break;

                case DrawFromDiscardCommand c:
                    writer.WriteValueSafe((byte)CmdKind.DrawFromDiscard);
                    writer.WriteValueSafe(c.PlayerIndex);
                    writer.WriteValueSafe(c.DiscardPileIndex);
                    break;

                case LayDownLevelCommand c:
                    writer.WriteValueSafe((byte)CmdKind.LayDown);
                    writer.WriteValueSafe(c.PlayerIndex);
                    writer.WriteValueSafe((byte)c.Melds.Count);
                    foreach (Meld meld in c.Melds)
                    {
                        writer.WriteValueSafe((byte)meld.Type);
                        writer.WriteValueSafe((byte)meld.Cards.Count);
                        foreach (CardModel card in meld.Cards)
                        {
                            WriteCard(writer, card);
                        }
                    }
                    break;

                case AddToMeldCommand c:
                    writer.WriteValueSafe((byte)CmdKind.AddToMeld);
                    writer.WriteValueSafe(c.PlayerIndex);
                    WriteCard(writer, c.Card);
                    writer.WriteValueSafe(c.MeldOwnerIndex);
                    writer.WriteValueSafe(c.MeldIndex);
                    break;

                case SkipPhaseCommand c:
                    writer.WriteValueSafe((byte)CmdKind.SkipPhase);
                    writer.WriteValueSafe(c.PlayerIndex);
                    writer.WriteValueSafe((byte)c.PhaseToSkip);
                    break;

                case DiscardCommand c:
                    writer.WriteValueSafe((byte)CmdKind.Discard);
                    writer.WriteValueSafe(c.PlayerIndex);
                    WriteCard(writer, c.Card);
                    writer.WriteValueSafe(c.TargetPlayerIndex);
                    break;
            }
        }

        // ────────────────────────────────────────────────────
        //  LECTURE
        // ────────────────────────────────────────────────────

        public static IGameCommand? Read(FastBufferReader reader)
        {
            reader.ReadValueSafe(out byte kindByte);
            reader.ReadValueSafe(out int playerIndex);

            switch ((CmdKind)kindByte)
            {
                case CmdKind.DrawFromDeck:
                    return new DrawFromDeckCommand(playerIndex);

                case CmdKind.DrawFromDiscard:
                {
                    reader.ReadValueSafe(out int pileIndex);
                    return new DrawFromDiscardCommand(playerIndex, pileIndex);
                }

                case CmdKind.LayDown:
                {
                    reader.ReadValueSafe(out byte meldCount);
                    List<Meld> melds = new(meldCount);
                    for (int m = 0; m < meldCount; m++)
                    {
                        reader.ReadValueSafe(out byte meldType);
                        reader.ReadValueSafe(out byte cardCount);
                        List<CardModel> cards = new(cardCount);
                        for (int i = 0; i < cardCount; i++)
                        {
                            cards.Add(ReadCard(reader));
                        }
                        melds.Add(new Meld((MeldType)meldType, cards, playerIndex));
                    }
                    return new LayDownLevelCommand(playerIndex, melds);
                }

                case CmdKind.AddToMeld:
                {
                    CardModel card = ReadCard(reader);
                    reader.ReadValueSafe(out int ownerIndex);
                    reader.ReadValueSafe(out int meldIndex);
                    return new AddToMeldCommand(playerIndex, card, ownerIndex, meldIndex);
                }

                case CmdKind.SkipPhase:
                {
                    reader.ReadValueSafe(out byte phase);
                    return new SkipPhaseCommand(playerIndex, (TurnPhase)phase);
                }

                case CmdKind.Discard:
                {
                    CardModel card = ReadCard(reader);
                    reader.ReadValueSafe(out int targetIndex);
                    return new DiscardCommand(playerIndex, card, targetIndex);
                }

                default:
                    return null;
            }
        }

        // ────────────────────────────────────────────────────
        //  CARTES
        // ────────────────────────────────────────────────────

        private static void WriteCard(FastBufferWriter writer, CardModel card)
        {
            writer.WriteValueSafe(card.Id);
            writer.WriteValueSafe(card.Value);
            writer.WriteValueSafe((byte)card.Color);
            writer.WriteValueSafe((byte)card.Type);
        }

        private static CardModel ReadCard(FastBufferReader reader)
        {
            reader.ReadValueSafe(out int id);
            reader.ReadValueSafe(out int value);
            reader.ReadValueSafe(out byte color);
            reader.ReadValueSafe(out byte type);

            CardType cardType = (CardType)type;
            return cardType == CardType.Normal
                ? new CardModel(id, value, (CardColor)color)
                : new CardModel(id, cardType);
        }
    }
}
