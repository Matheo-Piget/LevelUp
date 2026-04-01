using System.Collections.Generic;
using UnityEngine;
using LevelUp.Core;
using LevelUp.Utils;

namespace LevelUp.Network
{
    /// <summary>
    /// Stub multijoueur local en pass-and-play.
    /// Gère la transition entre joueurs humains sur le même appareil.
    /// </summary>
    public class NetworkManager : MonoBehaviour
    {
        [SerializeField] private GameObject? _passScreenPanel;
        [SerializeField] private TMPro.TextMeshProUGUI? _passScreenText;
        [SerializeField] private float _passScreenDuration = 1.5f;

        private GameManager? _gameManager;
        private int _localPlayerCount;
        private bool _isPassScreenActive;

        /// <summary>Indique si l'écran de transition est actif.</summary>
        public bool IsPassScreenActive => _isPassScreenActive;

        /// <summary>Événement quand l'écran de transition est fermé.</summary>
        public event System.Action? OnPassScreenDismissed;

        private void OnEnable()
        {
            EventBus.Subscribe<TurnStartedEvent>(OnTurnStarted);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<TurnStartedEvent>(OnTurnStarted);
        }

        /// <summary>
        /// Initialise le network manager.
        /// </summary>
        public void Initialize(GameManager gameManager, int localPlayerCount)
        {
            _gameManager = gameManager;
            _localPlayerCount = localPlayerCount;

            if (_passScreenPanel != null)
            {
                _passScreenPanel.SetActive(false);
            }
        }

        /// <summary>
        /// Appelé quand un nouveau tour commence.
        /// Si le joueur est humain en mode pass-and-play, affiche l'écran de transition.
        /// </summary>
        private void OnTurnStarted(TurnStartedEvent evt)
        {
            if (_gameManager == null) return;

            PlayerModel? player = _gameManager.GetCurrentPlayer();
            if (player == null || player.IsAI) return;

            // En pass-and-play avec plusieurs joueurs humains, montrer l'écran de transition
            if (_localPlayerCount > 1)
            {
                ShowPassScreen(player);
            }
        }

        /// <summary>
        /// Affiche l'écran "Passez l'appareil" entre deux joueurs humains.
        /// </summary>
        private void ShowPassScreen(PlayerModel nextPlayer)
        {
            _isPassScreenActive = true;

            if (_passScreenPanel != null)
            {
                _passScreenPanel.SetActive(true);
            }

            if (_passScreenText != null)
            {
                _passScreenText.text = $"Passez l'appareil à\n{nextPlayer.Name}\n\nTapez pour continuer";
            }
        }

        /// <summary>
        /// Ferme l'écran de transition (appelé par un bouton ou un tap).
        /// </summary>
        public void DismissPassScreen()
        {
            _isPassScreenActive = false;

            if (_passScreenPanel != null)
            {
                _passScreenPanel.SetActive(false);
            }

            OnPassScreenDismissed?.Invoke();
        }

        /// <summary>
        /// En mode pass-and-play, masque la main des autres joueurs.
        /// </summary>
        public bool ShouldShowHand(int playerIndex)
        {
            if (_gameManager?.TurnManager == null) return false;
            return _gameManager.TurnManager.CurrentPlayerIndex == playerIndex;
        }

        /// <summary>
        /// Retourne le type de réseau actif.
        /// </summary>
        public NetworkMode GetCurrentMode()
        {
            return _localPlayerCount > 1 ? NetworkMode.PassAndPlay : NetworkMode.SinglePlayer;
        }
    }

    /// <summary>
    /// Mode de jeu réseau.
    /// </summary>
    public enum NetworkMode
    {
        SinglePlayer,
        PassAndPlay
    }
}
