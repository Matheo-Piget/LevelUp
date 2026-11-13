using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LevelUp.UI
{
    /// <summary>
    /// Comportements de l'application, hors partie :
    ///  - bouton Retour Android / Échap : ferme la fenêtre la plus récente
    ///    (<see cref="UIBackStack"/>), sinon ouvre la pause en jeu ;
    ///  - mise en arrière-plan / fermeture : écrit les réglages sur le disque.
    /// </summary>
    public class AppLifecycle : MonoBehaviour
    {
        private PauseMenuController? _pause;
        private Func<bool>? _inGame;

        public void Initialize(Func<bool> inGame) => _inGame = inGame;

        public void BindPause(PauseMenuController pause) => _pause = pause;

        private void Update()
        {
            Keyboard? keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) OnBack();
        }

        private void OnBack()
        {
            if (UIBackStack.HandleBack()) return;
            if (_pause != null && _pause.PauseAllowed && _inGame != null && _inGame()) _pause.Show();
        }

        private void OnApplicationPause(bool paused)
        {
            if (!paused) return;
            GameSettings.Flush();
            PlayerPrefs.Save();
        }

        private void OnApplicationQuit()
        {
            GameSettings.Flush();
        }
    }
}
