using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using LevelUp.Core;
using NgoManager = Unity.Netcode.NetworkManager;

namespace LevelUp.Network
{
    /// <summary>Données de démarrage d'une partie en ligne, identiques pour tous.</summary>
    public struct OnlineStartData
    {
        public int Seed;
        public int LocalSeat;
        public bool RandomContracts;
        public PlayerSetup[] Players;
    }

    /// <summary>
    /// Données de reprise (reconnexion ou resynchronisation) : de quoi
    /// reconstruire la partie à l'identique en rejouant tout son historique.
    /// </summary>
    public struct OnlineResumeData
    {
        public OnlineStartData Start;
        public List<IGameCommand> History;
        public int ExpectedHash;
    }

    /// <summary>
    /// Session multijoueur en ligne via Unity Relay (code de partie à 6 lettres)
    /// et Netcode for GameObjects, sans objet réseau ni prefab : uniquement des
    /// messages nommés. Relay traverse les NAT/box/4G : PC ↔ téléphone ou
    /// téléphone ↔ téléphone fonctionnent depuis n'importe quel réseau.
    ///
    /// Modèle "simulation miroir" : chaque appareil fait tourner la MÊME partie
    /// (mêmes sièges, même seed de mélange). Une action locale est validée et
    /// exécutée sur place par <see cref="GameManager.ExecuteCommand"/>, puis la
    /// commande est relayée ; les autres appareils la rejouent à l'identique.
    /// L'IA ne tourne QUE chez l'hôte — ses commandes suivent le même chemin.
    ///
    /// Robustesse :
    ///  - Version : l'hôte refuse un appareil dont la version du jeu diffère.
    ///  - Désync : chaque commande porte l'empreinte d'état de l'émetteur ; un
    ///    écart déclenche une resynchronisation complète (historique rejoué).
    ///  - Reconnexion : un client qui perd le réseau (tunnel, Wi-Fi→4G, appli en
    ///    arrière-plan) se reconnecte seul et reprend son siège. Côté hôte, le
    ///    siège attend <see cref="ReconnectGraceSeconds"/> avant qu'un bot le prenne,
    ///    et le joueur le récupère même après.
    /// </summary>
    public class OnlineSession : MonoBehaviour
    {
        private const string MsgStart = "LU_START";
        private const string MsgCommand = "LU_CMD";
        private const string MsgToAI = "LU_TOAI";
        private const string MsgToHuman = "LU_TOHUMAN";
        private const string MsgInfo = "LU_INFO";
        private const string MsgResyncRequest = "LU_RESYNC_REQ";
        private const string MsgResumeBegin = "LU_RESUME_BEGIN";
        private const string MsgResumeChunk = "LU_RESUME_CHUNK";
        private const string MsgResumeEnd = "LU_RESUME_END";

        private const int MaxPlayers = 4;

        /// <summary>
        /// Version du protocole réseau : à incrémenter dès que le format des
        /// messages OU les règles changent (deux versions différentes divergeraient).
        /// </summary>
        private const int ProtocolVersion = 3;

        private const float ReconnectGraceSeconds = 45f;
        private const float ReconnectWindowSeconds = 90f;
        private const int ResumeChunkBytes = 3000;
        private const string PrefDeviceToken = "levelup.net.token";
        private const string PrefLastJoinCode = "levelup.net.lastcode";

        private NgoManager? _ngo;
        private GameManager? _gameManager;
        private bool _applyingRemote;
        private bool _matchRunning;
        private bool _leaving;

        // Hôte : lobby et sièges
        private readonly List<ulong> _lobbyClients = new();               // ordre de connexion
        private readonly Dictionary<ulong, string> _tokenByClient = new();
        private readonly Dictionary<ulong, int> _seatByClient = new();
        private readonly Dictionary<int, string> _tokenBySeat = new();
        private readonly Dictionary<ulong, int> _pendingReconnectSeat = new();
        private readonly HashSet<int> _disconnectedSeats = new();
        private readonly HashSet<int> _seatsTakenByAI = new();

        // Hôte : historique sérialisé de la partie (copie figée de chaque commande)
        private readonly List<byte[]> _history = new();
        private int _matchSeed;
        private bool _matchRandomContracts;

        // Client : reconnexion et reprise
        private string? _clientJoinCode;
        private bool _reconnecting;
        private bool _awaitingResume;
        private TaskCompletionSource<bool>? _connectAttempt;
        private OnlineStartData _resumeHeader;
        private int _resumeExpectedCount;
        private int _resumeHash;
        private readonly List<byte[]> _resumeBuffer = new();
        private float _pausedAt = -1f;

        /// <summary>Code de partie à partager (hôte uniquement).</summary>
        public string? JoinCode { get; private set; }

        /// <summary>Vrai si une session réseau est active (hôte ou client).</summary>
        public bool IsActive => _ngo != null && _ngo.IsListening;

        /// <summary>Vrai si cet appareil héberge la partie.</summary>
        public bool IsSessionHost => _ngo != null && _ngo.IsHost;

        /// <summary>Humains actuellement dans le lobby (hôte inclus).</summary>
        public int LobbyHumanCount => 1 + _lobbyClients.Count;

        /// <summary>Dernier code rejoint sur cet appareil (pour revenir après un crash).</summary>
        public static string LastJoinCode => PlayerPrefs.GetString(PrefLastJoinCode, "");

        /// <summary>Hôte : le nombre d'humains dans le lobby a changé.</summary>
        public event Action<int>? LobbyCountChanged;

        /// <summary>Client : connexion à l'hôte établie (en attente du lancement).</summary>
        public event Action? ConnectedToHost;

        /// <summary>Client : l'hôte a lancé la partie.</summary>
        public event Action<OnlineStartData>? MatchStartReceived;

        /// <summary>Client : partie à reconstruire (reconnexion ou désync corrigée).</summary>
        public event Action<OnlineResumeData>? ResumeReceived;

        /// <summary>Un joueur humain a quitté ; son siège devient un bot.</summary>
        public event Action<int>? PlayerReplacedByAI;

        /// <summary>Un joueur revenu reprend son siège à la place du bot.</summary>
        public event Action<int>? PlayerRestoredToHuman;

        /// <summary>Message d'information à afficher (déconnexion, retour d'un joueur…).</summary>
        public event Action<string>? Notice;

        /// <summary>Client : connexion perdue, reconnexion en cours (texte de statut).</summary>
        public event Action<string>? Reconnecting;

        /// <summary>Client : reconnecté à l'hôte (la reprise suit).</summary>
        public event Action? Reconnected;

        /// <summary>La session est terminée (déconnexion définitive, hôte parti…).</summary>
        public event Action<string>? SessionEnded;

        /// <summary>Une désynchronisation a été détectée (une resync est lancée).</summary>
        public event Action<string>? Desynchronized;

        // ────────────────────────────────────────────────────
        //  CONNEXION
        // ────────────────────────────────────────────────────

        /// <summary>
        /// Héberge une partie : alloue un relay Unity et retourne le code à partager.
        /// Nécessite un projet lié à Unity Gaming Services (service Relay activé).
        /// </summary>
        public async Task<string> HostAsync()
        {
            await EnsureServicesAsync();
            Debug.Log($"[OnlineSession] Services OK (joueur {AuthenticationService.Instance.PlayerId})");

            Allocation allocation =
                await RelayService.Instance.CreateAllocationAsync(MaxPlayers - 1);
            string code = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            NgoManager ngo = EnsureNetworkManager();
            await WaitForShutdownAsync(ngo);
            UnityTransport transport = ConfigureTransportCommon(ngo);
            (string host, ushort port, bool secure) = PickEndpoint(
                allocation.ServerEndpoints, allocation.RelayServer);
            Debug.Log($"[OnlineSession] Hôte : relay {host}:{port} (dtls={secure})");
            transport.SetHostRelayData(host, port,
                allocation.AllocationIdBytes, allocation.Key,
                allocation.ConnectionData, secure);

            ResetHostState();
            ngo.NetworkConfig.ConnectionData = BuildConnectionPayload();
            ngo.ConnectionApprovalCallback = ApproveConnection;

            if (!ngo.StartHost())
                throw new Exception("Impossible de démarrer l'hôte réseau");
            Debug.Log("[OnlineSession] Hôte démarré, en attente de clients");

            // L'hôte doit rester actif même fenêtre réduite (PC) : sinon tout le
            // monde attend. Sur mobile, l'OS suspend quand même l'appli en fond.
            Application.runInBackground = true;

            RegisterHandlers(ngo);
            JoinCode = code;
            return code;
        }

        /// <summary>Rejoint une partie hébergée par un ami via son code.</summary>
        public async Task JoinAsync(string joinCode)
        {
            string code = joinCode.Trim().ToUpperInvariant();
            _clientJoinCode = code;
            PlayerPrefs.SetString(PrefLastJoinCode, code);
            PlayerPrefs.Save();

            await StartClientAsync(code);
        }

        private async Task StartClientAsync(string code)
        {
            await EnsureServicesAsync();
            Debug.Log($"[OnlineSession] Services OK (joueur {AuthenticationService.Instance.PlayerId})");

            JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(code);
            Debug.Log("[OnlineSession] Allocation rejointe, configuration du transport");

            NgoManager ngo = EnsureNetworkManager();
            await WaitForShutdownAsync(ngo);
            UnityTransport transport = ConfigureTransportCommon(ngo);
            (string host, ushort port, bool secure) = PickEndpoint(
                allocation.ServerEndpoints, allocation.RelayServer);
            Debug.Log($"[OnlineSession] Client : relay {host}:{port} (dtls={secure})");
            transport.SetClientRelayData(host, port,
                allocation.AllocationIdBytes, allocation.Key,
                allocation.ConnectionData, allocation.HostConnectionData, secure);

            ngo.NetworkConfig.ConnectionData = BuildConnectionPayload();

            if (!ngo.StartClient())
                throw new Exception("Impossible de se connecter");
            Debug.Log("[OnlineSession] Client démarré, connexion à l'hôte en cours…");

            Application.runInBackground = true;
            RegisterHandlers(ngo);
        }

        /// <summary>Quitte proprement la session réseau (idempotent).</summary>
        public void Leave()
        {
            if (_leaving) return;
            _leaving = true;

            StopAllCoroutines();
            UnbindGameManager();
            if (_ngo != null)
            {
                UnregisterCallbacks(_ngo);
                if (_ngo.IsListening) _ngo.Shutdown();
            }

            ResetHostState();
            _matchRunning = false;
            _reconnecting = false;
            _awaitingResume = false;
            _clientJoinCode = null;
            _resumeBuffer.Clear();
            _connectAttempt?.TrySetResult(false);
            JoinCode = null;
            Application.runInBackground = false;
            _leaving = false;
        }

        private void ResetHostState()
        {
            _lobbyClients.Clear();
            _tokenByClient.Clear();
            _seatByClient.Clear();
            _tokenBySeat.Clear();
            _pendingReconnectSeat.Clear();
            _disconnectedSeats.Clear();
            _seatsTakenByAI.Clear();
            _history.Clear();
        }

        private static async Task EnsureServicesAsync()
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                await UnityServices.InitializeAsync();
            }
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }
        }

        private static async Task WaitForShutdownAsync(NgoManager ngo)
        {
            // Un StartClient/StartHost pendant un Shutdown en cours échoue.
            float deadline = Time.realtimeSinceStartup + 5f;
            while ((ngo.ShutdownInProgress || ngo.IsListening)
                   && Time.realtimeSinceStartup < deadline)
            {
                if (ngo.IsListening && !ngo.ShutdownInProgress) ngo.Shutdown();
                await Task.Yield();
            }
        }

        /// <summary>
        /// Traduit une exception réseau en message actionnable pour le joueur,
        /// suivi du détail technique en petit — indispensable pour diagnostiquer
        /// à distance un appareil qu'on n'a pas en main (capture d'écran).
        /// </summary>
        public static string DescribeError(Exception e)
        {
            while (e is AggregateException agg && agg.InnerException != null)
            {
                e = agg.InnerException;
            }

            string friendly;
            if (e is RelayServiceException relayEx)
            {
                friendly = relayEx.Reason switch
                {
                    RelayExceptionReason.JoinCodeNotFound =>
                        "Ce code ne correspond à aucune partie en cours.\nVérifie le code avec l'hôte (partie toujours ouverte ?)",
                    RelayExceptionReason.InvalidRequest =>
                        "Code invalide — vérifie les lettres avec l'hôte.",
                    RelayExceptionReason.NetworkError =>
                        "Serveurs Unity injoignables.\nVérifie ta connexion, essaie en 4G/5G au lieu du Wi-Fi.",
                    _ => "Erreur du service de mise en relation."
                };
            }
            else if (e is RequestFailedException reqEx)
            {
                bool offline = reqEx.ErrorCode == CommonErrorCodes.TransportError
                    || reqEx.ErrorCode == CommonErrorCodes.Timeout
                    || reqEx.ErrorCode == CommonErrorCodes.ServiceUnavailable;
                friendly = offline
                    ? "Impossible de contacter les serveurs Unity.\nVérifie : internet actif, date/heure du téléphone\nen automatique, données mobiles autorisées pour l'app."
                    : "Connexion aux services Unity refusée.";
            }
            else
            {
                friendly = "Erreur réseau inattendue.";
            }

            string detail = e.Message ?? "";
            if (detail.Length > 140) detail = detail.Substring(0, 140) + "…";
            return $"{friendly}\n<size=12><color=#9CA3AF>{e.GetType().Name} : {detail}</color></size>";
        }

        private static bool IsJoinCodeGone(Exception e)
        {
            while (e is AggregateException agg && agg.InnerException != null)
            {
                e = agg.InnerException;
            }
            return e is RelayServiceException relayEx
                && (relayEx.Reason == RelayExceptionReason.JoinCodeNotFound
                    || relayEx.Reason == RelayExceptionReason.AllocationNotFound);
        }

        /// <summary>
        /// Crée (ou récupère) le NetworkManager NGO à la volée — aucun objet de
        /// scène ni prefab requis, cohérent avec l'UI 100 % procédurale du projet.
        /// </summary>
        private NgoManager EnsureNetworkManager()
        {
            if (_ngo != null) return _ngo;

            if (NgoManager.Singleton != null)
            {
                _ngo = NgoManager.Singleton;
                return _ngo;
            }

            // À la racine de la scène : NGO gère son propre cycle de vie et
            // n'accepte pas d'être un enfant d'un autre objet.
            GameObject go = new("NetcodeManager");
            _ngo = go.AddComponent<NgoManager>();
            UnityTransport transport = go.AddComponent<UnityTransport>();

            _ngo.NetworkConfig = new NetworkConfig
            {
                NetworkTransport = transport,
                EnableSceneManagement = false,
                // Vérifie la version et identifie les joueurs qui reviennent.
                ConnectionApproval = true
            };
            _ngo.LogLevel = LogLevel.Normal;

            return _ngo;
        }

        private static UnityTransport ConfigureTransportCommon(NgoManager ngo)
        {
            UnityTransport transport = (UnityTransport)ngo.NetworkConfig.NetworkTransport;
            // Assez court pour détecter vite une coupure (la reconnexion prend le
            // relais), assez long pour encaisser un hoquet réseau mobile.
            transport.DisconnectTimeoutMS = 20000;
            transport.HeartbeatTimeoutMS = 500;
            return transport;
        }

        /// <summary>Choisit l'endpoint relay chiffré (dtls) si disponible.</summary>
        private static (string host, ushort port, bool secure) PickEndpoint(
            List<RelayServerEndpoint> endpoints, RelayServer fallback)
        {
            if (endpoints != null)
            {
                foreach (RelayServerEndpoint e in endpoints)
                {
                    if (e.ConnectionType == "dtls")
                        return (e.Host, (ushort)e.Port, true);
                }
            }
            return (fallback.IpV4, (ushort)fallback.Port, false);
        }

        private void RegisterHandlers(NgoManager ngo)
        {
            // Le NetworkManager survit aux sessions : pas de double abonnement.
            UnregisterCallbacks(ngo);
            ngo.OnClientConnectedCallback += OnClientConnected;
            ngo.OnClientDisconnectCallback += OnClientDisconnected;
            ngo.OnTransportFailure += OnTransportFailure;

            // Le CustomMessagingManager est recréé à chaque démarrage NGO.
            CustomMessagingManager msg = ngo.CustomMessagingManager;
            msg.RegisterNamedMessageHandler(MsgStart, OnStartMessage);
            msg.RegisterNamedMessageHandler(MsgCommand, OnCommandMessage);
            msg.RegisterNamedMessageHandler(MsgToAI, OnToAIMessage);
            msg.RegisterNamedMessageHandler(MsgToHuman, OnToHumanMessage);
            msg.RegisterNamedMessageHandler(MsgInfo, OnInfoMessage);
            msg.RegisterNamedMessageHandler(MsgResyncRequest, OnResyncRequestMessage);
            msg.RegisterNamedMessageHandler(MsgResumeBegin, OnResumeBeginMessage);
            msg.RegisterNamedMessageHandler(MsgResumeChunk, OnResumeChunkMessage);
            msg.RegisterNamedMessageHandler(MsgResumeEnd, OnResumeEndMessage);
        }

        private void UnregisterCallbacks(NgoManager ngo)
        {
            ngo.OnClientConnectedCallback -= OnClientConnected;
            ngo.OnClientDisconnectCallback -= OnClientDisconnected;
            ngo.OnTransportFailure -= OnTransportFailure;
        }

        // ────────────────────────────────────────────────────
        //  IDENTITÉ & APPROBATION (version + reconnexion)
        // ────────────────────────────────────────────────────

        /// <summary>
        /// Jeton stable de l'appareil : c'est lui qui permet à l'hôte de rendre
        /// son siège à un joueur qui se reconnecte (même après un redémarrage de l'appli).
        /// </summary>
        private static string DeviceToken
        {
            get
            {
                string token = PlayerPrefs.GetString(PrefDeviceToken, "");
                if (string.IsNullOrEmpty(token))
                {
                    token = Guid.NewGuid().ToString("N");
                    PlayerPrefs.SetString(PrefDeviceToken, token);
                    PlayerPrefs.Save();
                }
                return token;
            }
        }

        private static string VersionTag => $"{ProtocolVersion}/{Application.version}";

        private static byte[] BuildConnectionPayload()
        {
            return Encoding.UTF8.GetBytes($"LU|{VersionTag}|{DeviceToken}");
        }

        private void ApproveConnection(NgoManager.ConnectionApprovalRequest request,
            NgoManager.ConnectionApprovalResponse response)
        {
            response.CreatePlayerObject = false;

            if (_ngo != null && request.ClientNetworkId == NgoManager.ServerClientId)
            {
                response.Approved = true;
                return;
            }

            string[] parts = request.Payload != null
                ? Encoding.UTF8.GetString(request.Payload).Split('|')
                : Array.Empty<string>();

            if (parts.Length != 3 || parts[0] != "LU" || parts[1] != VersionTag)
            {
                string theirs = parts.Length == 3 ? parts[1] : "?";
                response.Approved = false;
                response.Reason =
                    $"Versions du jeu différentes (hôte {VersionTag}, toi {theirs}).\n" +
                    "Installe la même version sur tous les appareils.";
                return;
            }

            string token = parts[2];

            if (_matchRunning)
            {
                // Seul un joueur de la partie peut revenir, sur SON siège.
                foreach (KeyValuePair<int, string> entry in _tokenBySeat)
                {
                    if (entry.Value != token) continue;
                    _pendingReconnectSeat[request.ClientNetworkId] = entry.Key;
                    response.Approved = true;
                    return;
                }
                response.Approved = false;
                response.Reason = "La partie a déjà commencé.";
                return;
            }

            if (LobbyHumanCount >= MaxPlayers)
            {
                response.Approved = false;
                response.Reason = "La partie est complète (4 joueurs).";
                return;
            }

            _tokenByClient[request.ClientNetworkId] = token;
            response.Approved = true;
        }

        // ────────────────────────────────────────────────────
        //  CALLBACKS CONNEXION
        // ────────────────────────────────────────────────────

        private void OnClientConnected(ulong clientId)
        {
            Debug.Log($"[OnlineSession] Client connecté : {clientId} (local={_ngo?.LocalClientId})");
            if (_ngo == null) return;

            if (_ngo.IsHost)
            {
                if (clientId == _ngo.LocalClientId) return;

                if (_pendingReconnectSeat.TryGetValue(clientId, out int seat))
                {
                    _pendingReconnectSeat.Remove(clientId);
                    HandleSeatReconnected(clientId, seat);
                    return;
                }

                if (_matchRunning)
                {
                    _ngo.DisconnectClient(clientId, "La partie a déjà commencé.");
                    return;
                }

                if (!_lobbyClients.Contains(clientId))
                {
                    _lobbyClients.Add(clientId);
                }
                LobbyCountChanged?.Invoke(LobbyHumanCount);
            }
            else if (clientId == _ngo.LocalClientId)
            {
                _connectAttempt?.TrySetResult(true);

                if (_matchRunning)
                {
                    // Reconnexion : on redemande l'état complet à l'hôte. C'est le
                    // client qui le demande, une fois SA connexion confirmée.
                    RequestResync();
                }
                else
                {
                    ConnectedToHost?.Invoke();
                }
            }
        }

        private void OnClientDisconnected(ulong clientId)
        {
            Debug.Log($"[OnlineSession] Client déconnecté : {clientId} " +
                      $"(raison : « {_ngo?.DisconnectReason} »)");
            if (_ngo == null || _leaving) return;

            if (_ngo.IsHost)
            {
                _pendingReconnectSeat.Remove(clientId);
                _tokenByClient.Remove(clientId);

                if (_matchRunning && _seatByClient.TryGetValue(clientId, out int seat))
                {
                    _seatByClient.Remove(clientId);
                    _disconnectedSeats.Add(seat);
                    string name = SeatName(seat);
                    BroadcastNotice($"{name} a perdu la connexion — on l'attend…");
                    StartCoroutine(GraceThenBot(seat));
                }
                else if (_lobbyClients.Remove(clientId))
                {
                    LobbyCountChanged?.Invoke(LobbyHumanCount);
                }
            }
            else if (clientId == _ngo.LocalClientId || clientId == NgoManager.ServerClientId)
            {
                string reason = _ngo.DisconnectReason;
                _connectAttempt?.TrySetResult(false);

                bool refused = !string.IsNullOrEmpty(reason);
                if (_matchRunning && !refused && !_reconnecting)
                {
                    StartReconnect("Connexion perdue");
                    return;
                }
                if (_reconnecting && !refused) return; // la boucle gère l'échec

                _reconnecting = false;
                SessionEnded?.Invoke(refused ? reason : "Connexion à l'hôte perdue");
            }
        }

        private void OnTransportFailure()
        {
            Debug.LogError("[OnlineSession] Échec du transport réseau (relay injoignable ?)");
            if (_ngo == null || _leaving) return;

            if (!_ngo.IsHost && _matchRunning && !_reconnecting)
            {
                StartReconnect("Réseau perdu");
            }
            else if (_ngo.IsHost)
            {
                SessionEnded?.Invoke("Connexion au serveur de jeu perdue.\nLa partie en ligne est terminée.");
            }
        }

        /// <summary>
        /// Retour au premier plan après une pause (écran verrouillé, autre appli) :
        /// au-delà de quelques secondes, le relay a probablement coupé le lien —
        /// le client se reconnecte immédiatement au lieu d'attendre le timeout.
        /// </summary>
        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                _pausedAt = Time.realtimeSinceStartup;
                return;
            }

            float away = _pausedAt < 0f ? 0f : Time.realtimeSinceStartup - _pausedAt;
            _pausedAt = -1f;

            if (_ngo == null || _ngo.IsHost || !_matchRunning || _reconnecting) return;
            if (away > 6f)
            {
                Debug.Log($"[OnlineSession] Retour après {away:0}s en arrière-plan → reconnexion");
                StartReconnect("Reprise de la partie");
            }
        }

        // ────────────────────────────────────────────────────
        //  RECONNEXION (CLIENT)
        // ────────────────────────────────────────────────────

        private async void StartReconnect(string why)
        {
            if (_reconnecting || _clientJoinCode == null || _ngo == null) return;
            _reconnecting = true;

            float deadline = Time.realtimeSinceStartup + ReconnectWindowSeconds;
            int attempt = 0;

            while (_reconnecting && Time.realtimeSinceStartup < deadline)
            {
                attempt++;
                Reconnecting?.Invoke($"{why}\nReconnexion… (essai {attempt})");

                try
                {
                    if (_ngo.IsListening) _ngo.Shutdown();
                    await WaitForShutdownAsync(_ngo);
                    if (!_reconnecting) return;

                    _connectAttempt = new TaskCompletionSource<bool>();
                    await StartClientAsync(_clientJoinCode);

                    Task finished = await Task.WhenAny(_connectAttempt.Task, Task.Delay(12000));
                    if (!_reconnecting) return; // refus de l'hôte déjà signalé, ou Leave()
                    if (finished == _connectAttempt.Task && _connectAttempt.Task.Result)
                    {
                        _reconnecting = false;
                        Reconnected?.Invoke();
                        return;
                    }

                    // Refus explicite de l'hôte (partie terminée, version…) : inutile d'insister.
                    if (_ngo != null && !string.IsNullOrEmpty(_ngo.DisconnectReason))
                    {
                        _reconnecting = false;
                        SessionEnded?.Invoke(_ngo.DisconnectReason);
                        return;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[OnlineSession] Reconnexion échouée : {e.Message}");
                    if (IsJoinCodeGone(e))
                    {
                        _reconnecting = false;
                        SessionEnded?.Invoke("L'hôte a quitté : la partie est terminée.");
                        return;
                    }
                }

                await Task.Delay(Math.Min(2000 * attempt, 6000));
            }

            if (_reconnecting)
            {
                _reconnecting = false;
                SessionEnded?.Invoke("Impossible de rejoindre la partie.\nVérifie ta connexion internet.");
            }
        }

        // ────────────────────────────────────────────────────
        //  RECONNEXION (HÔTE)
        // ────────────────────────────────────────────────────

        private IEnumerator GraceThenBot(int seat)
        {
            float end = Time.realtimeSinceStartup + ReconnectGraceSeconds;
            while (Time.realtimeSinceStartup < end)
            {
                if (!_disconnectedSeats.Contains(seat) || !_matchRunning) yield break;
                yield return null;
            }

            if (!_disconnectedSeats.Contains(seat) || !_matchRunning) yield break;

            // Le siège devient un bot chez tout le monde pour ne pas bloquer la
            // partie ; le joueur pourra le récupérer s'il revient.
            _seatsTakenByAI.Add(seat);
            BroadcastSeatMessage(MsgToAI, seat, excludeClientId: null);
            PlayerReplacedByAI?.Invoke(seat);
        }

        private void HandleSeatReconnected(ulong clientId, int seat)
        {
            _seatByClient[clientId] = seat;
            _disconnectedSeats.Remove(seat);

            if (_seatsTakenByAI.Remove(seat))
            {
                // Rendre la main AVANT d'envoyer l'état : le client reçoit son
                // siège marqué humain, les autres sont prévenus.
                PlayerRestoredToHuman?.Invoke(seat);
                BroadcastSeatMessage(MsgToHuman, seat, excludeClientId: clientId);
            }

            BroadcastNotice($"{SeatName(seat)} est de retour !");
            // L'état complet part sur demande du client (MsgResyncRequest).
        }

        private string SeatName(int seat)
        {
            if (_gameManager != null && seat >= 0 && seat < _gameManager.Players.Count)
                return _gameManager.Players[seat].Name;
            return $"Joueur {seat + 1}";
        }

        // ────────────────────────────────────────────────────
        //  LANCEMENT DE PARTIE (HÔTE)
        // ────────────────────────────────────────────────────

        /// <summary>
        /// Hôte : fige les sièges (hôte = 0, clients dans l'ordre de connexion,
        /// bots en complément), envoie le message de départ à chaque client et
        /// retourne les données de démarrage pour la partie locale de l'hôte.
        /// </summary>
        public OnlineStartData BuildAndBroadcastStart(int requestedSeats, bool randomContracts)
        {
            if (_ngo == null || !_ngo.IsHost)
                throw new InvalidOperationException("Seul l'hôte peut lancer la partie");

            StopAllCoroutines();

            // Rejouer : les clients partis entre deux parties sont oubliés.
            HashSet<ulong> connected = new(_ngo.ConnectedClientsIds);
            _lobbyClients.RemoveAll(id => !connected.Contains(id));
            foreach (ulong id in _seatByClient.Keys)
            {
                if (!_lobbyClients.Contains(id) && connected.Contains(id))
                    _lobbyClients.Add(id);
            }

            int humans = LobbyHumanCount;
            int totalSeats = Mathf.Clamp(Mathf.Max(requestedSeats, humans), 2, MaxPlayers);
            int seed = new System.Random().Next();

            PlayerSetup[] players = new PlayerSetup[totalSeats];
            players[0] = new PlayerSetup("Joueur 1", false);

            Dictionary<ulong, string> tokens = new(_tokenByClient);
            _seatByClient.Clear();
            _tokenBySeat.Clear();
            _disconnectedSeats.Clear();
            _seatsTakenByAI.Clear();
            _pendingReconnectSeat.Clear();
            _tokenBySeat[0] = DeviceToken;

            for (int i = 0; i < _lobbyClients.Count; i++)
            {
                ulong clientId = _lobbyClients[i];
                _seatByClient[clientId] = i + 1;
                if (tokens.TryGetValue(clientId, out string? token))
                    _tokenBySeat[i + 1] = token;
                players[i + 1] = new PlayerSetup($"Joueur {i + 2}", false);
            }
            for (int s = humans; s < totalSeats; s++)
            {
                players[s] = new PlayerSetup($"Bot {s - humans + 1}", true);
            }

            _history.Clear();
            _matchSeed = seed;
            _matchRandomContracts = randomContracts;

            foreach (KeyValuePair<ulong, int> entry in _seatByClient)
            {
                using FastBufferWriter writer = new(512, Allocator.Temp, 64 * 1024);
                WriteStartHeader(writer, new OnlineStartData
                {
                    Seed = seed,
                    LocalSeat = entry.Value,
                    RandomContracts = randomContracts,
                    Players = players
                });
                _ngo.CustomMessagingManager.SendNamedMessage(MsgStart, entry.Key,
                    writer, NetworkDelivery.ReliableFragmentedSequenced);
            }

            _matchRunning = true;

            return new OnlineStartData
            {
                Seed = seed,
                LocalSeat = 0,
                RandomContracts = randomContracts,
                Players = players
            };
        }

        private static void WriteStartHeader(FastBufferWriter writer, OnlineStartData data)
        {
            writer.WriteValueSafe(data.Seed);
            writer.WriteValueSafe(data.LocalSeat);
            writer.WriteValueSafe(data.RandomContracts);
            writer.WriteValueSafe(data.Players.Length);
            foreach (PlayerSetup p in data.Players)
            {
                writer.WriteValueSafe(p.Name);
                writer.WriteValueSafe(p.IsAI);
            }
        }

        private static OnlineStartData ReadStartHeader(FastBufferReader reader)
        {
            reader.ReadValueSafe(out int seed);
            reader.ReadValueSafe(out int localSeat);
            reader.ReadValueSafe(out bool randomContracts);
            reader.ReadValueSafe(out int totalSeats);

            PlayerSetup[] players = new PlayerSetup[totalSeats];
            for (int i = 0; i < totalSeats; i++)
            {
                reader.ReadValueSafe(out string name);
                reader.ReadValueSafe(out bool isAI);
                players[i] = new PlayerSetup(name, isAI);
            }

            return new OnlineStartData
            {
                Seed = seed,
                LocalSeat = localSeat,
                RandomContracts = randomContracts,
                Players = players
            };
        }

        private void OnStartMessage(ulong senderId, FastBufferReader reader)
        {
            OnlineStartData data = ReadStartHeader(reader);
            _matchRunning = true;
            _awaitingResume = false;
            MatchStartReceived?.Invoke(data);
        }

        // ────────────────────────────────────────────────────
        //  RELAIS DE COMMANDES
        // ────────────────────────────────────────────────────

        /// <summary>
        /// Accroche la partie locale : toute commande exécutée avec succès sur
        /// cet appareil est relayée aux autres.
        /// </summary>
        public void BindGameManager(GameManager gameManager)
        {
            UnbindGameManager();
            _gameManager = gameManager;
            _gameManager.CommandExecuted += OnLocalCommandExecuted;
        }

        private void UnbindGameManager()
        {
            if (_gameManager != null)
            {
                _gameManager.CommandExecuted -= OnLocalCommandExecuted;
                _gameManager = null;
            }
        }

        private static byte[] Serialize(IGameCommand command)
        {
            using FastBufferWriter writer = new(256, Allocator.Temp, 64 * 1024);
            NetCommandCodec.Write(writer, command);
            return writer.ToArray();
        }

        private static IGameCommand? Deserialize(byte[] bytes)
        {
            using FastBufferReader reader = new(bytes, Allocator.Temp);
            return NetCommandCodec.Read(reader);
        }

        private void OnLocalCommandExecuted(IGameCommand command, CommandResult result)
        {
            if (_ngo == null || !_ngo.IsListening || _gameManager == null) return;

            // Historique de l'hôte : copie sérialisée immédiate (les listes de
            // cartes d'une commande sont ensuite partagées avec le modèle).
            byte[] bytes = Serialize(command);
            if (_ngo.IsHost && _matchRunning) _history.Add(bytes);

            // Une commande reçue du réseau est déjà connue de l'émetteur.
            if (_applyingRemote) return;

            int hash = _gameManager.ComputeStateHash();
            if (_ngo.IsHost)
            {
                SendCommandToClients(bytes, hash, excludeClientId: null);
            }
            else
            {
                SendCommand(NgoManager.ServerClientId, bytes, hash);
            }
        }

        private void SendCommand(ulong clientId, byte[] bytes, int hash)
        {
            if (_ngo == null) return;
            using FastBufferWriter writer = new(bytes.Length + 16, Allocator.Temp, 64 * 1024);
            writer.WriteValueSafe(hash);
            writer.WriteValueSafe(bytes.Length);
            writer.WriteBytesSafe(bytes);
            _ngo.CustomMessagingManager.SendNamedMessage(MsgCommand, clientId,
                writer, NetworkDelivery.ReliableFragmentedSequenced);
        }

        private void SendCommandToClients(byte[] bytes, int hash, ulong? excludeClientId)
        {
            if (_ngo == null) return;

            foreach (ulong clientId in _ngo.ConnectedClientsIds)
            {
                if (clientId == _ngo.LocalClientId) continue;
                if (excludeClientId.HasValue && clientId == excludeClientId.Value) continue;
                // Un client en cours de reconnexion recevra tout via la reprise.
                if (!_seatByClient.ContainsKey(clientId)) continue;
                SendCommand(clientId, bytes, hash);
            }
        }

        private void OnCommandMessage(ulong senderId, FastBufferReader reader)
        {
            reader.ReadValueSafe(out int senderHash);
            reader.ReadValueSafe(out int length);
            byte[] bytes = new byte[length];
            reader.ReadBytesSafe(ref bytes, length);

            if (_gameManager == null || _ngo == null) return;

            // Client en attente d'un état complet : ces commandes y seront incluses.
            if (!_ngo.IsHost && _awaitingResume) return;

            IGameCommand? command = Deserialize(bytes);
            if (command == null) return;

            if (_ngo.IsHost)
            {
                // Anti-usurpation : le client ne peut agir que pour SON siège.
                if (!_seatByClient.TryGetValue(senderId, out int seat) ||
                    command.PlayerIndex != seat)
                {
                    Debug.LogWarning($"[OnlineSession] Commande rejetée : siège invalide (client {senderId})");
                    SendResume(senderId);
                    return;
                }
            }

            _applyingRemote = true;
            CommandResult result;
            try
            {
                result = _gameManager.ExecuteCommand(command);
            }
            finally
            {
                _applyingRemote = false;
            }

            int localHash = _gameManager.ComputeStateHash();
            bool desync = !result.Success || localHash != senderHash;

            if (_ngo.IsHost)
            {
                if (result.Success)
                {
                    // L'hôte relaie la commande validée aux autres clients.
                    SendCommandToClients(bytes, localHash, excludeClientId: senderId);
                }
                if (desync)
                {
                    // L'hôte fait foi : il renvoie son état à l'appareil divergent.
                    Debug.LogWarning($"[OnlineSession] Désync client {senderId} " +
                                     $"({(result.Success ? "empreinte" : result.Message)}) → resync");
                    SendResume(senderId);
                }
            }
            else if (desync)
            {
                Debug.LogWarning($"[OnlineSession] Désync détectée " +
                                 $"({(result.Success ? "empreinte" : result.Message)}) → resync");
                Desynchronized?.Invoke("Resynchronisation…");
                RequestResync();
            }
        }

        // ────────────────────────────────────────────────────
        //  RESYNCHRONISATION (historique complet)
        // ────────────────────────────────────────────────────

        private void RequestResync()
        {
            if (_ngo == null || _ngo.IsHost || !_ngo.IsConnectedClient) return;
            _awaitingResume = true;
            using FastBufferWriter writer = new(8, Allocator.Temp);
            writer.WriteValueSafe(0);
            _ngo.CustomMessagingManager.SendNamedMessage(MsgResyncRequest,
                NgoManager.ServerClientId, writer, NetworkDelivery.ReliableFragmentedSequenced);
        }

        private void OnResyncRequestMessage(ulong senderId, FastBufferReader reader)
        {
            if (_ngo == null || !_ngo.IsHost || !_matchRunning) return;
            if (!_seatByClient.ContainsKey(senderId)) return;
            SendResume(senderId);
        }

        /// <summary>
        /// Hôte : envoie au client l'état complet sous forme d'en-tête de partie
        /// (sièges à jour) + historique de toutes les commandes, découpé en
        /// morceaux pour rester sous la taille max d'un message.
        /// </summary>
        private void SendResume(ulong clientId)
        {
            if (_ngo == null || _gameManager == null || !_matchRunning) return;
            if (!_seatByClient.TryGetValue(clientId, out int seat)) return;

            PlayerSetup[] players = new PlayerSetup[_gameManager.Players.Count];
            for (int i = 0; i < players.Length; i++)
            {
                PlayerModel p = _gameManager.Players[i];
                players[i] = new PlayerSetup(p.Name, p.IsAI);
            }

            using (FastBufferWriter header = new(512, Allocator.Temp, 64 * 1024))
            {
                WriteStartHeader(header, new OnlineStartData
                {
                    Seed = _matchSeed,
                    LocalSeat = seat,
                    RandomContracts = _matchRandomContracts,
                    Players = players
                });
                header.WriteValueSafe(_history.Count);
                header.WriteValueSafe(_gameManager.ComputeStateHash());
                _ngo.CustomMessagingManager.SendNamedMessage(MsgResumeBegin, clientId,
                    header, NetworkDelivery.ReliableFragmentedSequenced);
            }

            int index = 0;
            while (index < _history.Count)
            {
                using FastBufferWriter chunk = new(ResumeChunkBytes + 512, Allocator.Temp, 64 * 1024);
                int countPos = chunk.Position;
                chunk.WriteValueSafe(0);
                int inChunk = 0;
                int size = 0;
                while (index < _history.Count && (inChunk == 0 || size + _history[index].Length < ResumeChunkBytes))
                {
                    byte[] cmd = _history[index];
                    chunk.WriteValueSafe(cmd.Length);
                    chunk.WriteBytesSafe(cmd);
                    size += cmd.Length + 4;
                    inChunk++;
                    index++;
                }
                int end = chunk.Position;
                chunk.Seek(countPos);
                chunk.WriteValueSafe(inChunk);
                chunk.Seek(end);
                _ngo.CustomMessagingManager.SendNamedMessage(MsgResumeChunk, clientId,
                    chunk, NetworkDelivery.ReliableFragmentedSequenced);
            }

            using FastBufferWriter tail = new(8, Allocator.Temp);
            tail.WriteValueSafe(_history.Count);
            _ngo.CustomMessagingManager.SendNamedMessage(MsgResumeEnd, clientId,
                tail, NetworkDelivery.ReliableFragmentedSequenced);

            Debug.Log($"[OnlineSession] Reprise envoyée au client {clientId} ({_history.Count} commandes)");
        }

        private void OnResumeBeginMessage(ulong senderId, FastBufferReader reader)
        {
            _resumeHeader = ReadStartHeader(reader);
            reader.ReadValueSafe(out _resumeExpectedCount);
            reader.ReadValueSafe(out _resumeHash);
            _resumeBuffer.Clear();
            _awaitingResume = true;
            _matchRunning = true;
        }

        private void OnResumeChunkMessage(ulong senderId, FastBufferReader reader)
        {
            reader.ReadValueSafe(out int count);
            for (int i = 0; i < count; i++)
            {
                reader.ReadValueSafe(out int length);
                byte[] bytes = new byte[length];
                reader.ReadBytesSafe(ref bytes, length);
                _resumeBuffer.Add(bytes);
            }
        }

        private void OnResumeEndMessage(ulong senderId, FastBufferReader reader)
        {
            reader.ReadValueSafe(out int total);
            if (total != _resumeBuffer.Count || total != _resumeExpectedCount)
            {
                Debug.LogError($"[OnlineSession] Reprise incomplète ({_resumeBuffer.Count}/{total}) → nouvel essai");
                RequestResync();
                return;
            }

            List<IGameCommand> history = new(_resumeBuffer.Count);
            foreach (byte[] bytes in _resumeBuffer)
            {
                IGameCommand? cmd = Deserialize(bytes);
                if (cmd != null) history.Add(cmd);
            }
            _resumeBuffer.Clear();
            _awaitingResume = false;

            ResumeReceived?.Invoke(new OnlineResumeData
            {
                Start = _resumeHeader,
                History = history,
                ExpectedHash = _resumeHash
            });
        }

        // ────────────────────────────────────────────────────
        //  SIÈGES BOT / HUMAIN & INFOS
        // ────────────────────────────────────────────────────

        private void BroadcastSeatMessage(string message, int seat, ulong? excludeClientId)
        {
            if (_ngo == null) return;

            foreach (ulong clientId in _ngo.ConnectedClientsIds)
            {
                if (clientId == _ngo.LocalClientId) continue;
                if (excludeClientId.HasValue && clientId == excludeClientId.Value) continue;
                using FastBufferWriter writer = new(8, Allocator.Temp);
                writer.WriteValueSafe(seat);
                _ngo.CustomMessagingManager.SendNamedMessage(message, clientId,
                    writer, NetworkDelivery.ReliableFragmentedSequenced);
            }
        }

        private void OnToAIMessage(ulong senderId, FastBufferReader reader)
        {
            reader.ReadValueSafe(out int seat);
            PlayerReplacedByAI?.Invoke(seat);
        }

        private void OnToHumanMessage(ulong senderId, FastBufferReader reader)
        {
            reader.ReadValueSafe(out int seat);
            PlayerRestoredToHuman?.Invoke(seat);
        }

        /// <summary>Hôte : affiche un message chez lui et chez tous les clients.</summary>
        private void BroadcastNotice(string text)
        {
            Notice?.Invoke(text);
            if (_ngo == null) return;

            foreach (ulong clientId in _ngo.ConnectedClientsIds)
            {
                if (clientId == _ngo.LocalClientId) continue;
                using FastBufferWriter writer = new(256, Allocator.Temp, 4096);
                writer.WriteValueSafe(text);
                _ngo.CustomMessagingManager.SendNamedMessage(MsgInfo, clientId,
                    writer, NetworkDelivery.ReliableFragmentedSequenced);
            }
        }

        private void OnInfoMessage(ulong senderId, FastBufferReader reader)
        {
            reader.ReadValueSafe(out string text);
            Notice?.Invoke(text);
        }

        private void OnDestroy()
        {
            Leave();
        }
    }
}
