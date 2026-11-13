using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace LevelUp.Core
{
    /// <summary>Contenu d'une sauvegarde : de quoi reconstruire la partie à l'identique.</summary>
    public sealed class SavedGame
    {
        public int Seed;
        public MatchRules Rules = MatchRules.Classic;
        public PlayerSetup[] Players = Array.Empty<PlayerSetup>();

        /// <summary>Niveau des bots (0 facile, 1 normal, 2 expert).</summary>
        public int BotDifficulty;

        /// <summary>0 = partie libre, 1 = donne du jour.</summary>
        public int Mode;

        /// <summary>Jour de la donne (aaaammjj) quand <see cref="Mode"/> vaut 1.</summary>
        public int DailyKey;

        public readonly List<IGameCommand> History = new();
    }

    /// <summary>
    /// Sauvegarde automatique de la partie solo en cours : l'en-tête (graine,
    /// sièges, règles) puis une ligne par commande réussie. Comme la partie est
    /// déterministe, la rejouer avec <see cref="GameManager.RebuildFromHistory(IReadOnlyList{PlayerSetup}, int, MatchRules, IReadOnlyList{IGameCommand})"/>
    /// redonne exactement le même état — fermer l'application ne perd plus rien.
    ///
    /// Tolérante : une fin de fichier tronquée (appli tuée pendant l'écriture)
    /// est ignorée, une sauvegarde d'une autre version des règles est refusée.
    /// Aucune erreur d'entrée/sortie ne remonte au jeu.
    /// </summary>
    public static class SaveGame
    {
        private const uint Magic = 0x5653554C; // "LUSV"
        private const int FormatVersion = 1;
        private const string FileName = "partie-solo.sav";
        private const int MaxPlayers = 8;

        /// <summary>Chemin imposé (tests) ; null = dossier de données de l'application.</summary>
        public static string? PathOverride { get; set; }

        private static string FilePath =>
            PathOverride ?? Path.Combine(Application.persistentDataPath, FileName);

        /// <summary>Vrai si un fichier de sauvegarde est présent (pas forcément valide).</summary>
        public static bool Exists
        {
            get
            {
                try { return File.Exists(FilePath); }
                catch (Exception) { return false; }
            }
        }

        /// <summary>Commence une nouvelle sauvegarde (remplace la précédente).</summary>
        public static bool Begin(IReadOnlyList<PlayerSetup> players, int seed, MatchRules rules,
            int botDifficulty, int mode = 0, int dailyKey = 0)
        {
            try
            {
                using FileStream stream = new(FilePath, FileMode.Create, FileAccess.Write);
                using BinaryWriter w = new(stream);
                WriteHeader(w, players, seed, rules, botDifficulty, mode, dailyKey);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveGame] Sauvegarde impossible : {e.Message}");
                return false;
            }
        }

        /// <summary>Ajoute une commande réussie à la sauvegarde en cours.</summary>
        public static void Append(IGameCommand command)
        {
            try
            {
                byte[] bytes = GameCommandCodec.ToBytes(command);
                using FileStream stream = new(FilePath, FileMode.Append, FileAccess.Write);
                using BinaryWriter w = new(stream);
                w.Write((ushort)bytes.Length);
                w.Write(bytes);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveGame] Écriture impossible : {e.Message}");
            }
        }

        /// <summary>
        /// Lit la sauvegarde. Retourne null si elle est absente, illisible ou
        /// d'une autre version des règles. Une fin tronquée est coupée (et le
        /// fichier réécrit proprement pour que les ajouts suivants restent valides).
        /// </summary>
        public static SavedGame? Load()
        {
            byte[] data;
            try
            {
                if (!File.Exists(FilePath)) return null;
                data = File.ReadAllBytes(FilePath);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveGame] Lecture impossible : {e.Message}");
                return null;
            }

            SavedGame? saved = Parse(data, out bool truncated);
            if (saved != null && truncated) Rewrite(saved);
            return saved;
        }

        /// <summary>Supprime la sauvegarde (partie terminée ou illisible).</summary>
        public static void Delete()
        {
            try
            {
                if (File.Exists(FilePath)) File.Delete(FilePath);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveGame] Suppression impossible : {e.Message}");
            }
        }

        // ────────────────────────────────────────────────────
        //  FORMAT
        // ────────────────────────────────────────────────────

        private static void WriteHeader(BinaryWriter w, IReadOnlyList<PlayerSetup> players, int seed,
            MatchRules rules, int botDifficulty, int mode, int dailyKey)
        {
            w.Write(Magic);
            w.Write(FormatVersion);
            w.Write(GameManager.RulesVersion);
            w.Write(seed);
            w.Write(rules.RandomContracts);
            w.Write((byte)rules.SpecialCards);
            w.Write(rules.HandSize);
            w.Write(rules.LevelsToWin);
            w.Write(rules.TurnTimerSeconds);
            w.Write(botDifficulty);
            w.Write(mode);
            w.Write(dailyKey);
            w.Write((byte)players.Count);
            foreach (PlayerSetup p in players)
            {
                w.Write(p.Name ?? "");
                w.Write(p.IsAI);
            }
        }

        private static SavedGame? Parse(byte[] data, out bool truncated)
        {
            truncated = false;
            try
            {
                using MemoryStream stream = new(data, false);
                using BinaryReader r = new(stream);

                if (r.ReadUInt32() != Magic) return null;
                if (r.ReadInt32() != FormatVersion) return null;
                if (r.ReadInt32() != GameManager.RulesVersion) return null;

                SavedGame saved = new() { Seed = r.ReadInt32() };
                bool randomContracts = r.ReadBoolean();
                byte special = r.ReadByte();
                int handSize = r.ReadInt32();
                int levelsToWin = r.ReadInt32();
                int turnTimer = r.ReadInt32();
                saved.Rules = new MatchRules(randomContracts, (SpecialCardsMode)special, handSize,
                    levelsToWin, turnTimer);
                saved.BotDifficulty = r.ReadInt32();
                saved.Mode = r.ReadInt32();
                saved.DailyKey = r.ReadInt32();

                int playerCount = r.ReadByte();
                if (playerCount < 2 || playerCount > MaxPlayers) return null;
                saved.Players = new PlayerSetup[playerCount];
                for (int i = 0; i < playerCount; i++)
                {
                    string name = r.ReadString();
                    bool isAI = r.ReadBoolean();
                    saved.Players[i] = new PlayerSetup(name, isAI);
                }

                // Commandes : [longueur ushort][octets]. La première anomalie
                // arrête la lecture — ce qui précède reste une partie valide.
                while (stream.Position < stream.Length)
                {
                    if (stream.Length - stream.Position < 2) { truncated = true; break; }
                    int length = r.ReadUInt16();
                    if (length == 0 || length > GameCommandCodec.MaxCommandBytes
                        || stream.Length - stream.Position < length)
                    {
                        truncated = true;
                        break;
                    }

                    IGameCommand? command = GameCommandCodec.FromBytes(r.ReadBytes(length));
                    if (command == null) { truncated = true; break; }
                    saved.History.Add(command);
                }

                return saved;
            }
            catch (Exception)
            {
                return null; // en-tête illisible
            }
        }

        private static void Rewrite(SavedGame saved)
        {
            try
            {
                using FileStream stream = new(FilePath, FileMode.Create, FileAccess.Write);
                using BinaryWriter w = new(stream);
                WriteHeader(w, saved.Players, saved.Seed, saved.Rules, saved.BotDifficulty,
                    saved.Mode, saved.DailyKey);
                foreach (IGameCommand command in saved.History)
                {
                    byte[] bytes = GameCommandCodec.ToBytes(command);
                    w.Write((ushort)bytes.Length);
                    w.Write(bytes);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveGame] Réécriture impossible : {e.Message}");
            }
        }
    }
}
