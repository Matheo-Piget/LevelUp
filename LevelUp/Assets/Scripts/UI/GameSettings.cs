using System;
using UnityEngine;

namespace LevelUp.UI
{
    /// <summary>
    /// Paramètres globaux du jeu, persistants via PlayerPrefs.
    /// Toute modification déclenche <see cref="SettingsChanged"/> pour que les écouteurs
    /// (AudioManager, CardView pour le daltonisme, QualitySettings) s'adaptent.
    /// </summary>
    public static class GameSettings
    {
        private const string KeyMusic = "levelup.music";
        private const string KeySfx = "levelup.sfx";
        private const string KeyQuality = "levelup.quality";
        private const string KeyColorblind = "levelup.colorblind";

        private static float _musicVolume = 0.6f;
        private static float _sfxVolume = 0.8f;
        private static int _qualityIndex = 2;
        private static bool _colorblindMode;
        private static bool _loaded;

        /// <summary>Déclenché à chaque modification de paramètre.</summary>
        public static event Action? SettingsChanged;

        /// <summary>Volume musique (0–1).</summary>
        public static float MusicVolume
        {
            get { EnsureLoaded(); return _musicVolume; }
            set
            {
                EnsureLoaded();
                _musicVolume = Mathf.Clamp01(value);
                PlayerPrefs.SetFloat(KeyMusic, _musicVolume);
                Notify();
            }
        }

        /// <summary>Volume effets sonores (0–1).</summary>
        public static float SfxVolume
        {
            get { EnsureLoaded(); return _sfxVolume; }
            set
            {
                EnsureLoaded();
                _sfxVolume = Mathf.Clamp01(value);
                PlayerPrefs.SetFloat(KeySfx, _sfxVolume);
                Notify();
            }
        }

        /// <summary>Indice de qualité (0=Bas, 1=Moyen, 2=Elevé).</summary>
        public static int QualityIndex
        {
            get { EnsureLoaded(); return _qualityIndex; }
            set
            {
                EnsureLoaded();
                _qualityIndex = Mathf.Clamp(value, 0, 2);
                PlayerPrefs.SetInt(KeyQuality, _qualityIndex);
                ApplyQuality();
                Notify();
            }
        }

        /// <summary>Mode daltonisme (icônes distinctes par couleur de carte).</summary>
        public static bool ColorblindMode
        {
            get { EnsureLoaded(); return _colorblindMode; }
            set
            {
                EnsureLoaded();
                _colorblindMode = value;
                PlayerPrefs.SetInt(KeyColorblind, value ? 1 : 0);
                Notify();
            }
        }

        private static void EnsureLoaded()
        {
            if (_loaded) return;
            _musicVolume = PlayerPrefs.GetFloat(KeyMusic, 0.6f);
            _sfxVolume = PlayerPrefs.GetFloat(KeySfx, 0.8f);
            _qualityIndex = PlayerPrefs.GetInt(KeyQuality, 2);
            _colorblindMode = PlayerPrefs.GetInt(KeyColorblind, 0) == 1;
            _loaded = true;
            ApplyQuality();
        }

        private static void Notify()
        {
            PlayerPrefs.Save();
            SettingsChanged?.Invoke();
        }

        private static void ApplyQuality()
        {
            int target = Mathf.Clamp(_qualityIndex, 0, QualitySettings.names.Length - 1);
            QualitySettings.SetQualityLevel(target, applyExpensiveChanges: true);
        }

        /// <summary>Force le chargement initial (appelé au démarrage).</summary>
        public static void Initialize()
        {
            EnsureLoaded();
        }
    }
}
