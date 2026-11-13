using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using LevelUp.Core;
using LevelUp.Utils;

namespace LevelUp.UI
{
    /// <summary>
    /// Plusieurs humains sur un même appareil : quand le tour passe à un autre
    /// joueur humain, la main affichée est cachée et un écran « Passe l'appareil
    /// à X » couvre le jeu. La main du nouveau joueur n'apparaît qu'après son
    /// accord. Aucune règle ici : seul le siège AFFICHÉ change
    /// (<see cref="GameSession.LocalPlayerIndex"/>).
    /// </summary>
    public class PassAndPlayController : MonoBehaviour
    {
        private GameManager? _gameManager;
        private HandView? _handView;
        private GameObject? _cover;
        private TextMeshProUGUI? _name;
        private int _pendingSeat = -1;
        private bool _active;
        private bool _handHidden;

        /// <summary>Vrai quand la partie en cours se joue à plusieurs sur cet appareil.</summary>
        public bool Active => _active;

        /// <summary>Le siège affiché a changé : les vues dépendantes du joueur local se recalent.</summary>
        public event Action<int>? SeatShown;

        public void Setup(Canvas canvas, GameManager gameManager, HandView? handView)
        {
            _gameManager = gameManager;
            _handView = handView;

            Image cover = UISkin.Rect(canvas.transform, "PassCover", UISkin.Ground, 0f, raycast: true);
            cover.gameObject.AddComponent<UIInputBlocker>();
            UISkin.Stretch(cover.rectTransform);

            TextMeshProUGUI caption = UISkin.Text(cover.transform, "Caption", "PASSE L'APPAREIL À", 15f,
                UISkin.TextMuted, TextWeight.SemiBold, TextAlignmentOptions.Center);
            caption.characterSpacing = 8f;
            UISkin.Place(caption.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 110f), new Vector2(700f, 24f));

            _name = UISkin.Text(cover.transform, "Name", "", 64f, UISkin.TextColor, TextWeight.Display,
                TextAlignmentOptions.Center);
            UISkin.Place(_name.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 40f), new Vector2(900f, 84f));

            TextMeshProUGUI hint = UISkin.Text(cover.transform, "Hint",
                "Les autres ne regardent pas : ta main s'affiche quand tu touches le bouton.", 16f,
                UISkin.TextMuted, TextWeight.Regular, TextAlignmentOptions.Center);
            UISkin.Place(hint.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -30f), new Vector2(900f, 24f));

            Button ready = UIFactory.CreateButton(cover.transform, "BtnPassReady", "C'EST MOI",
                new Vector2(320f, 68f), Confirm, primary: true);
            UISkin.Place((RectTransform)ready.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -120f), new Vector2(320f, 68f));

            _cover = cover.gameObject;
            _cover.SetActive(false);

            EventBus.Subscribe<TurnStartedEvent>(OnTurnStarted);
            EventBus.Subscribe<RoundStartedEvent>(OnRoundStarted);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<TurnStartedEvent>(OnTurnStarted);
            EventBus.Unsubscribe<RoundStartedEvent>(OnRoundStarted);
        }

        /// <summary>À appeler au lancement d'une partie : active le mode si plusieurs humains jouent ici.</summary>
        public void Begin()
        {
            _pendingSeat = -1;
            _handHidden = false;
            if (_cover != null) _cover.SetActive(false);

            int humans = 0;
            if (_gameManager != null && !GameSession.IsOnline)
            {
                foreach (PlayerModel p in _gameManager.Players)
                {
                    if (!p.IsAI) humans++;
                }
            }
            _active = humans > 1;

            // Première manche : la donne est cachée jusqu'à ce que le premier joueur prenne l'appareil.
            if (_active) RequestHandoffForCurrent(force: true);
        }

        public void End()
        {
            _active = false;
            _pendingSeat = -1;
            if (_cover != null) _cover.SetActive(false);
        }

        // Nouvelle manche : la donne ne doit pas s'afficher pour celui qui tient l'appareil.
        private void OnRoundStarted(RoundStartedEvent evt)
        {
            if (_active) HideHand();
        }

        private void OnTurnStarted(TurnStartedEvent evt)
        {
            if (!_active || _gameManager == null) return;
            RequestHandoffForCurrent(force: _handHidden);
        }

        private void RequestHandoffForCurrent(bool force)
        {
            TurnManager? tm = _gameManager?.TurnManager;
            if (tm == null || _gameManager!.State != GameState.PlayerTurn) return;
            PlayerModel current = tm.CurrentPlayer;
            if (current.IsAI) return; // pendant le tour d'un bot, la dernière main reste en place
            if (!force && current.Index == GameSession.LocalPlayerIndex) return;

            HideHand();
            _pendingSeat = current.Index;
            if (_name != null) _name.text = current.Name;
            if (_cover != null)
            {
                _cover.SetActive(true);
                _cover.transform.SetAsLastSibling();
            }
        }

        private void HideHand()
        {
            _handHidden = true;
            _handView?.RefreshHand(Array.Empty<CardModel>());
        }

        private void Confirm()
        {
            if (_cover != null) _cover.SetActive(false);
            if (_gameManager == null || _pendingSeat < 0 || _pendingSeat >= _gameManager.Players.Count) return;

            int seat = _pendingSeat;
            _pendingSeat = -1;
            _handHidden = false;
            GameSession.LocalPlayerIndex = seat;
            if (_handView != null)
            {
                _handView.SetPlayerIndex(seat);
                _handView.RefreshHand(_gameManager.Players[seat].Hand, dealAnimation: true);
            }
            SeatShown?.Invoke(seat);
        }

        /// <summary>Vrai si la donne doit rester cachée (le bootstrapper n'affiche pas la main lui-même).</summary>
        public bool HidesDeal => _active;
    }
}
