using UnityEngine;
using LevelUp.Core;
using LevelUp.Utils;

namespace LevelUp.UI
{
    /// <summary>
    /// Gère la musique de fond et les effets sonores du jeu.
    ///
    /// <para>Câblage scène attendu :</para>
    /// <list type="number">
    ///   <item><description>Ajouter un GameObject "AudioManager" avec deux <see cref="AudioSource"/>
    ///   enfants : MusicSource (loop, playOnAwake si <see cref="_musicClip"/> assigné)
    ///   et SfxSource.</description></item>
    ///   <item><description>Assigner les clips dans l'inspecteur.</description></item>
    ///   <item><description>Le composant s'abonne automatiquement aux events de jeu et à
    ///   <see cref="GameSettings.SettingsChanged"/> pour appliquer les volumes.</description></item>
    /// </list>
    ///
    /// <para>Tant que <see cref="_musicSource"/> et <see cref="_sfxSource"/> ne sont
    /// pas assignés (ou si les clips sont nuls), l'AudioManager reste silencieux —
    /// pas de NullReference, juste pas de son. Ça permet de l'ajouter en scène
    /// sans bloquer le jeu si les assets audio ne sont pas encore là.</para>
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        [Header("Sources")]
        [Tooltip("AudioSource pour la musique de fond (loop)")]
        [SerializeField] private AudioSource? _musicSource;

        [Tooltip("AudioSource pour les effets sonores ponctuels")]
        [SerializeField] private AudioSource? _sfxSource;

        [Header("Music")]
        [SerializeField] private AudioClip? _musicClip;

        [Header("SFX")]
        [SerializeField] private AudioClip? _drawClip;
        [SerializeField] private AudioClip? _discardClip;
        [SerializeField] private AudioClip? _layDownClip;
        [SerializeField] private AudioClip? _addToMeldClip;
        [SerializeField] private AudioClip? _levelUpClip;
        [SerializeField] private AudioClip? _roundEndClip;
        [SerializeField] private AudioClip? _gameOverClip;
        [SerializeField] private AudioClip? _actionCardClip;
        [SerializeField] private AudioClip? _invalidActionClip;

        [Header("Tuning")]
        [Tooltip("Variation aléatoire du pitch pour les SFX (0 = désactivé)")]
        [Range(0f, 0.3f)]
        [SerializeField] private float _pitchJitter = 0.05f;

        private void Awake()
        {
            GameSettings.Initialize();
            ApplyVolumes();

            if (_musicSource != null && _musicClip != null)
            {
                _musicSource.clip = _musicClip;
                _musicSource.loop = true;
                _musicSource.playOnAwake = false;
                if (!_musicSource.isPlaying) _musicSource.Play();
            }
        }

        private void OnEnable()
        {
            GameSettings.SettingsChanged += ApplyVolumes;

            EventBus.Subscribe<CardDrawnEvent>(OnDraw);
            EventBus.Subscribe<CardDiscardedEvent>(OnDiscard);
            EventBus.Subscribe<LevelLaidDownEvent>(OnLayDown);
            EventBus.Subscribe<CardAddedToMeldEvent>(OnAddToMeld);
            EventBus.Subscribe<LevelCompletedEvent>(OnLevelCompleted);
            EventBus.Subscribe<RoundEndedEvent>(OnRoundEnded);
            EventBus.Subscribe<GameOverEvent>(OnGameOver);
            EventBus.Subscribe<ActionCardPlayedEvent>(OnActionCardPlayed);
            EventBus.Subscribe<InvalidActionEvent>(OnInvalidAction);
        }

        private void OnDisable()
        {
            GameSettings.SettingsChanged -= ApplyVolumes;

            EventBus.Unsubscribe<CardDrawnEvent>(OnDraw);
            EventBus.Unsubscribe<CardDiscardedEvent>(OnDiscard);
            EventBus.Unsubscribe<LevelLaidDownEvent>(OnLayDown);
            EventBus.Unsubscribe<CardAddedToMeldEvent>(OnAddToMeld);
            EventBus.Unsubscribe<LevelCompletedEvent>(OnLevelCompleted);
            EventBus.Unsubscribe<RoundEndedEvent>(OnRoundEnded);
            EventBus.Unsubscribe<GameOverEvent>(OnGameOver);
            EventBus.Unsubscribe<ActionCardPlayedEvent>(OnActionCardPlayed);
            EventBus.Unsubscribe<InvalidActionEvent>(OnInvalidAction);
        }

        /// <summary>
        /// Applique les volumes courants sur les deux sources. Appelé à l'init et
        /// à chaque modification de <see cref="GameSettings"/>.
        /// </summary>
        private void ApplyVolumes()
        {
            if (_musicSource != null) _musicSource.volume = GameSettings.MusicVolume;
            if (_sfxSource != null)   _sfxSource.volume   = GameSettings.SfxVolume;
        }

        // ──────────────────────────────────────────────
        //  Helpers
        // ──────────────────────────────────────────────

        private void Play(AudioClip? clip)
        {
            if (clip == null || _sfxSource == null) return;

            // Pitch jitter : casse la répétitivité quand on enchaîne le même SFX.
            float originalPitch = _sfxSource.pitch;
            if (_pitchJitter > 0f)
            {
                _sfxSource.pitch = 1f + Random.Range(-_pitchJitter, _pitchJitter);
            }
            _sfxSource.PlayOneShot(clip);
            _sfxSource.pitch = originalPitch;
        }

        /// <summary>Joue un SFX ponctuel. API publique pour les autres systèmes.</summary>
        public void PlaySfx(AudioClip? clip) => Play(clip);

        /// <summary>Coupe la musique (ex: écran game over avec stinger).</summary>
        public void StopMusic()
        {
            if (_musicSource != null && _musicSource.isPlaying)
                _musicSource.Stop();
        }

        // ──────────────────────────────────────────────
        //  Handlers d'event
        // ──────────────────────────────────────────────

        private void OnDraw(CardDrawnEvent _)              => Play(_drawClip);
        private void OnDiscard(CardDiscardedEvent _)       => Play(_discardClip);
        private void OnLayDown(LevelLaidDownEvent _)       => Play(_layDownClip);
        private void OnAddToMeld(CardAddedToMeldEvent _)   => Play(_addToMeldClip);
        private void OnLevelCompleted(LevelCompletedEvent _)=> Play(_levelUpClip);
        private void OnRoundEnded(RoundEndedEvent _)       => Play(_roundEndClip);

        private void OnGameOver(GameOverEvent _)
        {
            Play(_gameOverClip);
            StopMusic();
        }

        private void OnActionCardPlayed(ActionCardPlayedEvent _) => Play(_actionCardClip);

        private void OnInvalidAction(InvalidActionEvent _) => Play(_invalidActionClip);
    }
}
