using UnityEngine;
using UnityEngine.Rendering;

namespace LevelUp.UI
{
    /// <summary>
    /// Rendu à la demande : un jeu de cartes est immobile l'essentiel du temps,
    /// inutile de redessiner l'écran 60 ou 120 fois par seconde pendant que le
    /// joueur réfléchit. Tant que rien ne bouge, l'écran n'est redessiné qu'à
    /// ~30 images/s (les petites animations d'ambiance restent fluides) ; au
    /// moindre toucher ou dès qu'une animation signale son activité
    /// (<see cref="Ping"/>), le rendu repasse à pleine cadence.
    ///
    /// Seul le RENDU est espacé : la logique (Update, lecture du pointeur)
    /// tourne toujours à pleine cadence, la réactivité au toucher est intacte.
    /// </summary>
    public sealed class FrameBudget : MonoBehaviour
    {
        private const float DefaultHold = 0.5f;
        private const int IdleFramesPerSecond = 30;

        private static float _busyUntil;
        private static FrameBudget? _instance;

        /// <summary>Faux = toujours pleine cadence (captures, diagnostics).</summary>
        public static bool Enabled { get; set; } = true;

        /// <summary>Signale une animation en cours : pleine cadence pendant encore <paramref name="seconds"/>.</summary>
        public static void Ping(float seconds = DefaultHold)
        {
            float until = Time.unscaledTime + seconds;
            if (until > _busyUntil) _busyUntil = until;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _busyUntil = 0f;
            _instance = null;
            Enabled = true;
            OnDemandRendering.renderFrameInterval = 1;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (_instance != null) return;
            GameObject go = new("FrameBudget");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<FrameBudget>();
            Ping(2f);
        }

        private void Update()
        {
            // Tout toucher (ou déplacement de souris) relance la pleine cadence.
            if (PointerInput.TryRead(out _) && PointerInput.LastActivityFrame == Time.frameCount) Ping();

            int interval = 1;
            if (Enabled && Time.unscaledTime >= _busyUntil)
            {
                int target = Application.targetFrameRate > 0 ? Application.targetFrameRate : 60;
                interval = Mathf.Max(1, Mathf.RoundToInt(target / (float)IdleFramesPerSecond));
            }
            if (OnDemandRendering.renderFrameInterval != interval) OnDemandRendering.renderFrameInterval = interval;
        }

        private void OnDestroy()
        {
            if (_instance == this) OnDemandRendering.renderFrameInterval = 1;
        }
    }
}
