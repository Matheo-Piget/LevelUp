using System;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using LevelUp.Core;
using NgoManager = Unity.Netcode.NetworkManager;

namespace LevelUp.Network
{
    /// <summary>
    /// Volet « social » de la session en ligne : réactions (emotes) et vote de
    /// revanche. L'hôte fait foi : il vérifie le siège de l'émetteur, applique
    /// l'anti-spam et rediffuse. Les règles et l'encodage sont dans
    /// <see cref="SocialProtocol"/> (testés sans réseau).
    /// </summary>
    public partial class OnlineSession
    {
        private const string MsgEmote = "LU_EMOTE";
        private const string MsgRematch = "LU_REMATCH";

        private readonly EmoteLimiter _emoteLimiter = new();
        private readonly RematchVote _rematch = new();
        private float _lastLocalEmote = -100f;

        /// <summary>Une réaction à afficher : (siège, identifiant dans <see cref="SocialProtocol.Emotes"/>).</summary>
        public event Action<int, int>? EmoteReceived;

        /// <summary>État du vote de revanche : (sièges qui ont voté, sièges attendus), en masques de bits.</summary>
        public event Action<int, int>? RematchChanged;

        /// <summary>Hôte : tous les joueurs présents veulent une revanche.</summary>
        public event Action? RematchAgreed;

        /// <summary>Hôte : au moins un autre humain est connecté à la partie.</summary>
        public bool HasRemotePlayers => _seatByClient.Count > 0;

        private void RegisterSocialHandlers(CustomMessagingManager msg)
        {
            msg.RegisterNamedMessageHandler(MsgEmote, OnEmoteMessage);
            msg.RegisterNamedMessageHandler(MsgRematch, OnRematchMessage);
        }

        private void ResetSocialState()
        {
            _emoteLimiter.Clear();
            _rematch.Reset();
        }

        // ────────────────────────────────────────────────────
        //  EMOTES
        // ────────────────────────────────────────────────────

        /// <summary>Envoie une réaction du joueur local (ignorée si trop rapprochée de la précédente).</summary>
        public void SendEmote(int emoteId)
        {
            if (_ngo == null || !_ngo.IsListening || !SocialProtocol.IsValidEmote(emoteId)) return;
            if (Time.unscaledTime - _lastLocalEmote < SocialProtocol.EmoteCooldownSeconds) return;
            _lastLocalEmote = Time.unscaledTime;

            if (_ngo.IsHost)
            {
                BroadcastEmote(GameSession.LocalPlayerIndex, emoteId);
            }
            else
            {
                // Le siège n'est pas envoyé : l'hôte le déduit de l'émetteur.
                SendSocial(MsgEmote, NgoManager.ServerClientId, new[] { (byte)emoteId });
            }
        }

        private void BroadcastEmote(int seat, int emoteId)
        {
            if (_ngo == null) return;
            byte[] bytes = SocialProtocol.EncodeEmote(seat, emoteId);
            foreach (ulong clientId in _ngo.ConnectedClientsIds)
            {
                if (clientId != _ngo.LocalClientId) SendSocial(MsgEmote, clientId, bytes);
            }
            EmoteReceived?.Invoke(seat, emoteId);
        }

        private void OnEmoteMessage(ulong senderId, FastBufferReader reader)
        {
            byte[]? bytes = ReadSocial(reader);
            if (bytes == null || _ngo == null) return;

            if (_ngo.IsHost)
            {
                if (bytes.Length != 1 || !_seatByClient.TryGetValue(senderId, out int seat)) return;
                if (!SocialProtocol.IsValidEmote(bytes[0])) return;
                if (!_emoteLimiter.TryAccept(seat, Time.unscaledTime)) return;
                BroadcastEmote(seat, bytes[0]);
                return;
            }

            if (!IsFromHost(senderId)) return;
            if (SocialProtocol.TryDecodeEmote(bytes, SocialProtocol.MaxSeats, out int from, out int emoteId))
            {
                EmoteReceived?.Invoke(from, emoteId);
            }
        }

        // ────────────────────────────────────────────────────
        //  REVANCHE
        // ────────────────────────────────────────────────────

        /// <summary>Le joueur local vote pour une revanche (fin de partie).</summary>
        public void VoteRematch()
        {
            if (_ngo == null || !_ngo.IsListening) return;
            if (_ngo.IsHost) RegisterRematchVote(GameSession.LocalPlayerIndex);
            else SendSocial(MsgRematch, NgoManager.ServerClientId, new byte[] { 1 });
        }

        /// <summary>Hôte : sièges humains présents (lui-même + les clients qui ont un siège).</summary>
        private int HumanSeatsMask()
        {
            int mask = 1 << GameSession.LocalPlayerIndex;
            foreach (int seat in _seatByClient.Values)
            {
                if (seat >= 0 && seat < SocialProtocol.MaxSeats) mask |= 1 << seat;
            }
            return mask;
        }

        private void RegisterRematchVote(int seat)
        {
            _rematch.SetExpected(HumanSeatsMask());
            if (!_rematch.Vote(seat)) return;
            BroadcastRematch();
            if (_rematch.IsAgreed)
            {
                _rematch.Reset();
                RematchAgreed?.Invoke();
            }
        }

        /// <summary>Hôte : un joueur est parti — le vote se recompte sans lui.</summary>
        private void RefreshRematchAfterLeave()
        {
            if (_ngo == null || !_ngo.IsHost || _rematch.VotesMask == 0) return;
            _rematch.SetExpected(HumanSeatsMask());
            BroadcastRematch();
            if (_rematch.IsAgreed)
            {
                _rematch.Reset();
                RematchAgreed?.Invoke();
            }
        }

        private void BroadcastRematch()
        {
            if (_ngo == null) return;
            byte[] bytes = SocialProtocol.EncodeRematch(_rematch.VotesMask, _rematch.ExpectedMask);
            foreach (ulong clientId in _ngo.ConnectedClientsIds)
            {
                if (clientId != _ngo.LocalClientId) SendSocial(MsgRematch, clientId, bytes);
            }
            RematchChanged?.Invoke(_rematch.VotesMask, _rematch.ExpectedMask);
        }

        private void OnRematchMessage(ulong senderId, FastBufferReader reader)
        {
            byte[]? bytes = ReadSocial(reader);
            if (bytes == null || _ngo == null) return;

            if (_ngo.IsHost)
            {
                if (_seatByClient.TryGetValue(senderId, out int seat)) RegisterRematchVote(seat);
                return;
            }

            if (!IsFromHost(senderId)) return;
            if (SocialProtocol.TryDecodeRematch(bytes, out int votes, out int expected))
            {
                RematchChanged?.Invoke(votes, expected);
            }
        }

        // ────────────────────────────────────────────────────
        //  TRANSPORT (bloc d'octets court, taille bornée)
        // ────────────────────────────────────────────────────

        private void SendSocial(string message, ulong clientId, byte[] bytes)
        {
            if (_ngo == null) return;
            using FastBufferWriter writer = new(16, Allocator.Temp);
            writer.WriteValueSafe((byte)bytes.Length);
            writer.WriteBytesSafe(bytes);
            _ngo.CustomMessagingManager.SendNamedMessage(message, clientId, writer,
                NetworkDelivery.ReliableSequenced);
        }

        private static byte[]? ReadSocial(FastBufferReader reader)
        {
            reader.ReadValueSafe(out byte length);
            if (length == 0 || length > 8) return null;
            byte[] bytes = new byte[length];
            reader.ReadBytesSafe(ref bytes, length);
            return bytes;
        }
    }
}
