using UnityEngine;
using LevelUp.Core;
using LevelUp.Utils;

namespace LevelUp.UI
{
    /// <summary>
    /// Son du jeu, sans aucun fichier audio : les effets (carte piochée, posée,
    /// défaussée, erreur, montée de niveau…) et une nappe d'ambiance discrète
    /// sont synthétisés une fois au démarrage, puis joués sur les événements du
    /// Core. Volumes musique / effets réglés dans les options.
    /// Ajouté au runtime par GameBootstrapper ; déclenche aussi les vibrations.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        private const int Rate = 44100;
        private const float MusicGain = 0.55f;

        private AudioSource? _music;
        private AudioSource? _sfx;
        private GameManager? _gameManager;

        private AudioClip? _draw;
        private AudioClip? _place;
        private AudioClip? _discard;
        private AudioClip? _layDown;
        private AudioClip? _levelUp;
        private AudioClip? _invalid;
        private AudioClip? _turn;
        private AudioClip? _roundEnd;
        private AudioClip? _win;
        private AudioClip? _lose;
        private AudioClip? _click;

        public static AudioManager? Instance { get; private set; }

        public void Initialize(GameManager? gameManager)
        {
            Instance = this;
            _gameManager = gameManager;
            GameSettings.Initialize();

            _music = gameObject.AddComponent<AudioSource>();
            _music.loop = true;
            _music.playOnAwake = false;
            _music.spatialBlend = 0f;
            _sfx = gameObject.AddComponent<AudioSource>();
            _sfx.playOnAwake = false;
            _sfx.spatialBlend = 0f;

            BuildClips();
            ApplyVolumes();
            // La nappe (~3 millions de sinus) est calculée par tranches sur
            // plusieurs frames : le démarrage n'est plus bloqué sur mobile.
            StartCoroutine(BuildAmbientRoutine());
        }

        private System.Collections.IEnumerator BuildAmbientRoutine()
        {
            const int rate = 22050;
            const float chordLength = 4f;
            const int sliceSamples = rate / 4; // ~0,25 s de son par frame et par accord
            float[][] chords =
            {
                new[] { 220f, 261.63f, 329.63f, 392f },
                new[] { 174.61f, 220f, 261.63f, 329.63f },
                new[] { 130.81f, 196f, 246.94f, 329.63f },
                new[] { 196f, 246.94f, 293.66f, 392f }
            };
            int total = Mathf.RoundToInt(chordLength * chords.Length * rate);
            float[] data = new float[total];
            int chordSamples = Mathf.RoundToInt((chordLength + 1.6f) * rate);

            for (int c = 0; c < chords.Length; c++)
            {
                int start = Mathf.RoundToInt(c * chordLength * rate);
                for (int from = 0; from < chordSamples; from += sliceSamples)
                {
                    int to = Mathf.Min(chordSamples, from + sliceSamples);
                    for (int i = from; i < to; i++)
                    {
                        float t = i / (float)rate;
                        float env = Mathf.Clamp01(t / 1.4f) * Mathf.Clamp01((chordLength + 1.6f - t) / 1.6f);
                        if (env <= 0f) continue;
                        float v = 0f;
                        foreach (float f in chords[c])
                        {
                            v += Mathf.Sin(2f * Mathf.PI * f * t) + 0.5f * Mathf.Sin(2f * Mathf.PI * f * 1.003f * t);
                        }
                        data[(start + i) % total] += v * env * 0.035f;
                    }
                    yield return null;
                }
            }

            AudioClip clip = AudioClip.Create("music_ambient", total, 1, rate, false);
            clip.SetData(data, 0);
            if (_music == null) yield break;
            _music.clip = clip;
            _music.Play();
        }

        private void OnEnable()
        {
            GameSettings.SettingsChanged += ApplyVolumes;
            EventBus.Subscribe<CardDrawnEvent>(OnDraw);
            EventBus.Subscribe<CardDiscardedEvent>(OnDiscard);
            EventBus.Subscribe<LevelLaidDownEvent>(OnLayDown);
            EventBus.Subscribe<CardAddedToMeldEvent>(OnAddToMeld);
            EventBus.Subscribe<RoundSummaryEvent>(OnRoundSummary);
            EventBus.Subscribe<GameOverEvent>(OnGameOver);
            EventBus.Subscribe<InvalidActionEvent>(OnInvalidAction);
            EventBus.Subscribe<TurnStartedEvent>(OnTurnStarted);
            EventBus.Subscribe<ActionCardPlayedEvent>(OnActionCard);
        }

        private void OnDisable()
        {
            GameSettings.SettingsChanged -= ApplyVolumes;
            EventBus.Unsubscribe<CardDrawnEvent>(OnDraw);
            EventBus.Unsubscribe<CardDiscardedEvent>(OnDiscard);
            EventBus.Unsubscribe<LevelLaidDownEvent>(OnLayDown);
            EventBus.Unsubscribe<CardAddedToMeldEvent>(OnAddToMeld);
            EventBus.Unsubscribe<RoundSummaryEvent>(OnRoundSummary);
            EventBus.Unsubscribe<GameOverEvent>(OnGameOver);
            EventBus.Unsubscribe<InvalidActionEvent>(OnInvalidAction);
            EventBus.Unsubscribe<TurnStartedEvent>(OnTurnStarted);
            EventBus.Unsubscribe<ActionCardPlayedEvent>(OnActionCard);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void ApplyVolumes()
        {
            if (_music != null) _music.volume = GameSettings.MusicVolume * MusicGain;
            if (_sfx != null) _sfx.volume = GameSettings.SfxVolume;
        }

        /// <summary>Petit clic d'interface (boutons de tri, choix).</summary>
        public void PlayClick() => Play(_click, 0.6f);

        private void Play(AudioClip? clip, float volume = 1f, float pitch = 1f)
        {
            if (clip == null || _sfx == null) return;
            _sfx.pitch = pitch * (1f + Random.Range(-0.03f, 0.03f));
            _sfx.PlayOneShot(clip, volume);
        }

        private bool IsLocal(int playerIndex)
        {
            if (_gameManager == null || playerIndex < 0 || playerIndex >= _gameManager.Players.Count) return false;
            return GameSession.IsLocalHuman(_gameManager.Players[playerIndex]);
        }

        // ─── Événements ──────────────────────────────────────────────────

        private void OnDraw(CardDrawnEvent e) => Play(_draw, IsLocal(e.PlayerIndex) ? 0.9f : 0.35f);

        private void OnDiscard(CardDiscardedEvent e) => Play(_discard, IsLocal(e.PlayerIndex) ? 0.9f : 0.45f);

        private void OnAddToMeld(CardAddedToMeldEvent e)
        {
            bool local = IsLocal(e.PlayerIndex);
            Play(_place, local ? 0.9f : 0.5f);
            if (local) Haptics.Light();
        }

        private void OnLayDown(LevelLaidDownEvent e)
        {
            bool local = IsLocal(e.PlayerIndex);
            Play(_layDown, local ? 1f : 0.55f);
            if (local) Haptics.Success();
        }

        private void OnRoundSummary(RoundSummaryEvent e)
        {
            if (e.GameOver) return; // l'écran de fin a son propre son
            bool localWon = IsLocal(e.WinnerIndex);
            Play(localWon ? _levelUp : _roundEnd, 0.9f);
            if (localWon) Haptics.Success();
        }

        private void OnGameOver(GameOverEvent e)
        {
            bool localWon = IsLocal(e.WinnerIndex);
            Play(localWon ? _win : _lose, 1f);
            if (localWon) Haptics.Success();
        }

        private void OnInvalidAction(InvalidActionEvent e)
        {
            Play(_invalid, 0.8f);
            Haptics.Error();
        }

        private void OnTurnStarted(TurnStartedEvent e)
        {
            if (!IsLocal(e.PlayerIndex)) return;
            Play(_turn, 0.55f);
            Haptics.Turn();
        }

        private void OnActionCard(ActionCardPlayedEvent e)
        {
            if (e.Card.IsWild) return;
            Play(_invalid, 0.45f, 1.6f);
        }

        // ═══════════════════════════════════════════════════════════
        //  SYNTHÈSE (une fois au démarrage, ~30 ms)
        // ═══════════════════════════════════════════════════════════

        private void BuildClips()
        {
            _draw = Make("sfx_draw", 0.12f, (t, n) => Noise(n) * Env(t, 0.004f, 0.11f) * 0.35f * (0.4f + 4f * t));
            _place = Make("sfx_place", 0.09f, (t, n) =>
                (Sine(t, 1320f) * 0.5f + Sine(t, 2640f) * 0.15f) * Env(t, 0.002f, 0.08f) * 0.5f
                + Noise(n) * Env(t, 0.001f, 0.02f) * 0.2f);
            _discard = Make("sfx_discard", 0.16f, (t, n) =>
                Sine(t, Mathf.Lerp(170f, 90f, t / 0.16f)) * Env(t, 0.003f, 0.15f) * 0.7f
                + Noise(n) * Env(t, 0.001f, 0.03f) * 0.25f);
            _layDown = Arpeggio("sfx_laydown", new[] { 523.25f, 659.25f, 783.99f }, 0.07f, 0.55f, 0.45f);
            _levelUp = Arpeggio("sfx_levelup", new[] { 523.25f, 659.25f, 783.99f, 1046.5f }, 0.08f, 0.7f, 0.45f);
            _roundEnd = Arpeggio("sfx_round", new[] { 392f, 523.25f }, 0.12f, 0.6f, 0.4f);
            _win = Arpeggio("sfx_win", new[] { 523.25f, 659.25f, 783.99f, 1046.5f, 1318.5f }, 0.1f, 1.1f, 0.42f);
            _lose = Arpeggio("sfx_lose", new[] { 440f, 392f, 329.6f }, 0.16f, 0.9f, 0.35f);
            _turn = Make("sfx_turn", 0.35f, (t, n) =>
                (Sine(t, 880f) * 0.6f + Sine(t, 1760f) * 0.12f) * Env(t, 0.005f, 0.33f) * 0.35f);
            _invalid = Make("sfx_invalid", 0.22f, (t, n) =>
            {
                float f = t < 0.1f ? 196f : 165f;
                float gate = t < 0.09f || t > 0.11f ? 1f : 0f;
                return Soft(Sine(t, f) * 1.8f) * Env(t, 0.004f, 0.21f) * gate * 0.3f;
            });
            _click = Make("sfx_click", 0.03f, (t, n) => Sine(t, 1800f) * Env(t, 0.001f, 0.028f) * 0.35f);
        }

        private delegate float Generator(float t, int n);

        private static AudioClip Make(string name, float duration, Generator gen)
        {
            int samples = Mathf.CeilToInt(duration * Rate);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++) data[i] = gen(i / (float)Rate, i);
            AudioClip clip = AudioClip.Create(name, samples, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip Arpeggio(string name, float[] notes, float step, float duration, float gain)
        {
            return Make(name, duration, (t, n) =>
            {
                float v = 0f;
                for (int k = 0; k < notes.Length; k++)
                {
                    float start = k * step;
                    if (t < start) break;
                    float lt = t - start;
                    v += (Sine(lt, notes[k]) + 0.25f * Sine(lt, notes[k] * 2f)) * Env(lt, 0.004f, duration - start);
                }
                return v * gain / Mathf.Sqrt(notes.Length);
            });
        }


        private static float Sine(float t, float freq) => Mathf.Sin(2f * Mathf.PI * freq * t);

        private static float Soft(float x) => x / (1f + Mathf.Abs(x));

        /// <summary>Enveloppe attaque linéaire puis décroissance exponentielle jusqu'à ~0 à <paramref name="length"/>.</summary>
        private static float Env(float t, float attack, float length)
        {
            if (t < attack) return t / attack;
            if (t >= length) return 0f;
            float k = (t - attack) / Mathf.Max(0.0001f, length - attack);
            return Mathf.Exp(-5f * k) * (1f - k);
        }

        private static uint _noiseState = 22222u;

        private static float Noise(int n)
        {
            _ = n;
            _noiseState ^= _noiseState << 13;
            _noiseState ^= _noiseState >> 17;
            _noiseState ^= _noiseState << 5;
            return (_noiseState / (float)uint.MaxValue) * 2f - 1f;
        }
    }
}
