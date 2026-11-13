using System;
using UnityEngine;
using LevelUp.Core;
using LevelUp.Utils;

namespace LevelUp.UI
{
    /// <summary>
    /// Paramètres globaux du jeu, persistants via PlayerPrefs.
    /// Toute modification déclenche <see cref="SettingsChanged"/> pour que les écouteurs
    /// (AudioManager, CardView pour le daltonisme, QualitySettings) s'adaptent.
    ///
    /// Deux familles :
    ///  - préférences de l'appareil (volumes, qualité, daltonisme, vitesse des bots,
    ///    aides, vibrations, animations, taille d'interface, cosmétiques) ;
    ///  - réglages de la prochaine partie (joueurs, bots, règles) → <see cref="BuildMatchRules"/>.
    ///    En ligne, seuls ceux de l'hôte comptent (ils sont envoyés à tous).
    ///
    /// L'écriture disque est différée (<see cref="Flush"/> : pause / fermeture de
    /// l'appli, fermeture d'un panneau) : déplacer un curseur ne sauve plus à chaque cran.
    /// </summary>
    public static class GameSettings
    {
        private const string KeyMusic = "levelup.music";
        private const string KeySfx = "levelup.sfx";
        private const string KeyQuality = "levelup.quality";
        private const string KeyColorblind = "levelup.colorblind";
        private const string KeyPlayerCount = "levelup.playercount";
        private const string KeyDifficulty = "levelup.difficulty";
        private const string KeyRandomContracts = "levelup.randomcontracts";
        private const string KeySpecialCards = "levelup.specialcards";
        private const string KeyHandSize = "levelup.handsize";
        private const string KeyLevelsToWin = "levelup.levelstowin";
        private const string KeyTurnTimer = "levelup.turntimer";
        private const string KeyBotSpeed = "levelup.botspeed";
        private const string KeyHints = "levelup.hints";
        private const string KeyHintContract = "levelup.hint.contract";
        private const string KeyHintPlayable = "levelup.hint.playable";
        private const string KeyQuickDiscard = "levelup.quickdiscard";
        private const string KeyHandSort = "levelup.handsort";
        private const string KeyHaptics = "levelup.haptics";
        private const string KeyReducedMotion = "levelup.reducedmotion";
        private const string KeyUiScale = "levelup.uiscale";
        private const string KeyHighFps = "levelup.highfps";
        private const string KeyCardBack = "levelup.cardback";
        private const string KeyFelt = "levelup.felt";

        /// <summary>Tailles de main proposées.</summary>
        public static readonly int[] HandSizeOptions = { 8, 10, 12 };

        /// <summary>Niveaux finaux proposés (durée de partie).</summary>
        public static readonly int[] LevelsToWinOptions = { 4, 6, 8 };

        /// <summary>Minuteurs de tour proposés (secondes, 0 = sans limite).</summary>
        public static readonly int[] TurnTimerOptions = { 0, 30, 60 };

        /// <summary>Victoires nécessaires pour débloquer chaque dos / tapis (index = variante).</summary>
        public static readonly int[] CosmeticUnlockWins = { 0, 3, 10 };

        private static float _musicVolume = 0.35f;
        private static float _sfxVolume = 0.8f;
        private static int _qualityIndex = 2;
        private static bool _colorblindMode;
        private static int _playerCount = 4;
        private static int _difficulty = 1;
        private static bool _randomContracts;
        private static int _specialCards;
        private static int _handSize = MatchRules.DefaultHandSize;
        private static int _levelsToWin = Constants.MaxLevel;
        private static int _turnTimer;
        private static int _botSpeed = 1;
        private static bool _showHints = true;
        private static bool _hintContract;
        private static bool _hintPlayable;
        private static bool _quickDiscard;
        private static int _handSort;
        private static bool _haptics = true;
        private static bool _reducedMotion;
        private static int _uiScale;
        private static bool _highFps = true;
        private static int _cardBack;
        private static int _felt;
        private static bool _loaded;
        private static bool _dirty;

        /// <summary>Déclenché à chaque modification de paramètre.</summary>
        public static event Action? SettingsChanged;

        /// <summary>Volume musique (0–1).</summary>
        public static float MusicVolume
        {
            get { EnsureLoaded(); return _musicVolume; }
            set { EnsureLoaded(); _musicVolume = Mathf.Clamp01(value); PlayerPrefs.SetFloat(KeyMusic, _musicVolume); Notify(); }
        }

        /// <summary>Volume effets sonores (0–1).</summary>
        public static float SfxVolume
        {
            get { EnsureLoaded(); return _sfxVolume; }
            set { EnsureLoaded(); _sfxVolume = Mathf.Clamp01(value); PlayerPrefs.SetFloat(KeySfx, _sfxVolume); Notify(); }
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

        /// <summary>Mode daltonisme (symboles renforcés sur chaque carte).</summary>
        public static bool ColorblindMode
        {
            get { EnsureLoaded(); return _colorblindMode; }
            set { EnsureLoaded(); _colorblindMode = value; PlayerPrefs.SetInt(KeyColorblind, value ? 1 : 0); Notify(); }
        }

        /// <summary>Nombre total de joueurs (humain + bots), 2–4. Appliqué à la prochaine partie.</summary>
        public static int PlayerCount
        {
            get { EnsureLoaded(); return _playerCount; }
            set { EnsureLoaded(); _playerCount = Mathf.Clamp(value, 2, 4); PlayerPrefs.SetInt(KeyPlayerCount, _playerCount); Notify(); }
        }

        /// <summary>Difficulté des bots (0=Facile, 1=Normal, 2=Expert). Appliquée à la prochaine partie.</summary>
        public static int Difficulty
        {
            get { EnsureLoaded(); return _difficulty; }
            set { EnsureLoaded(); _difficulty = Mathf.Clamp(value, 0, 2); PlayerPrefs.SetInt(KeyDifficulty, _difficulty); Notify(); }
        }

        /// <summary>Contrats variés (une variante de difficulté équivalente par niveau).</summary>
        public static bool RandomContracts
        {
            get { EnsureLoaded(); return _randomContracts; }
            set { EnsureLoaded(); _randomContracts = value; PlayerPrefs.SetInt(KeyRandomContracts, value ? 1 : 0); Notify(); }
        }

        /// <summary>
        /// Cartes spéciales (0 = aucune, 1 = jokers, 2 = jokers + Passe + +2).
        /// Désactivées par défaut : le paquet reste classique tant que le joueur
        /// ne les met pas en jeu lui-même.
        /// </summary>
        public static int SpecialCards
        {
            get { EnsureLoaded(); return _specialCards; }
            set { EnsureLoaded(); _specialCards = Mathf.Clamp(value, 0, 2); PlayerPrefs.SetInt(KeySpecialCards, _specialCards); Notify(); }
        }

        /// <summary>Cartes distribuées en début de manche (8, 10 ou 12).</summary>
        public static int HandSize
        {
            get { EnsureLoaded(); return _handSize; }
            set
            {
                EnsureLoaded();
                _handSize = Mathf.Clamp(value, MatchRules.MinHandSize, MatchRules.MaxHandSize);
                PlayerPrefs.SetInt(KeyHandSize, _handSize);
                Notify();
            }
        }

        /// <summary>Niveau final de la partie (durée) : 4, 6 ou 8.</summary>
        public static int LevelsToWin
        {
            get { EnsureLoaded(); return _levelsToWin; }
            set
            {
                EnsureLoaded();
                _levelsToWin = Mathf.Clamp(value, MatchRules.MinLevelsToWin, Constants.MaxLevel);
                PlayerPrefs.SetInt(KeyLevelsToWin, _levelsToWin);
                Notify();
            }
        }

        /// <summary>Minuteur de tour en secondes (0 = sans limite).</summary>
        public static int TurnTimer
        {
            get { EnsureLoaded(); return _turnTimer; }
            set
            {
                EnsureLoaded();
                _turnTimer = Mathf.Clamp(value, 0, MatchRules.MaxTurnTimerSeconds);
                PlayerPrefs.SetInt(KeyTurnTimer, _turnTimer);
                Notify();
            }
        }

        /// <summary>Vitesse de jeu des bots (0 = posée, 1 = normale, 2 = rapide).</summary>
        public static int BotSpeed
        {
            get { EnsureLoaded(); return _botSpeed; }
            set { EnsureLoaded(); _botSpeed = Mathf.Clamp(value, 0, 2); PlayerPrefs.SetInt(KeyBotSpeed, _botSpeed); Notify(); }
        }

        /// <summary>Multiplicateur des temps de réflexion des bots.</summary>
        public static float BotDelayFactor => BotSpeed switch
        {
            0 => 1.7f,
            2 => 0.45f,
            _ => 1f
        };

        // ─── Aides de jeu (chacune indépendante) ─────────────────────────
        // Les aides qui donnent une information de jeu (contrat prêt, cartes
        // jouables) sont DÉSACTIVÉES par défaut : un contrat qu'on ne voit pas
        // reste en main. Seule la consigne d'étape (où on en est du tour) est active.

        /// <summary>Consigne d'étape sous la table, pioche qui pulse, aide de la table.</summary>
        public static bool HintSteps
        {
            get { EnsureLoaded(); return _showHints; }
            set { EnsureLoaded(); _showHints = value; PlayerPrefs.SetInt(KeyHints, value ? 1 : 0); Notify(); }
        }

        /// <summary>
        /// Détection du contrat : progression "2/3" sur la carte contrat, « contrat
        /// prêt », sélection validée en direct, « Préparer la pose », pose automatique
        /// sans sélection. Désactivée : le joueur repère et sélectionne son contrat seul.
        /// </summary>
        public static bool HintContract
        {
            get { EnsureLoaded(); return _hintContract; }
            set { EnsureLoaded(); _hintContract = value; PlayerPrefs.SetInt(KeyHintContract, value ? 1 : 0); Notify(); }
        }

        /// <summary>
        /// Cartes jouables : pastille « + » sur les cartes qui complètent la table,
        /// aimant limité aux combinaisons compatibles, pose d'un simple toucher.
        /// </summary>
        public static bool HintPlayable
        {
            get { EnsureLoaded(); return _hintPlayable; }
            set { EnsureLoaded(); _hintPlayable = value; PlayerPrefs.SetInt(KeyHintPlayable, value ? 1 : 0); Notify(); }
        }

        /// <summary>
        /// Défausse en un toucher. Désactivée (défaut) : un premier toucher soulève la
        /// carte, un second la défausse (glisser sur la défausse reste immédiat).
        /// </summary>
        public static bool QuickDiscard
        {
            get { EnsureLoaded(); return _quickDiscard; }
            set { EnsureLoaded(); _quickDiscard = value; PlayerPrefs.SetInt(KeyQuickDiscard, value ? 1 : 0); Notify(); }
        }

        /// <summary>
        /// Tri de main mémorisé (0 = ordre libre, 1 = par valeur, 2 = par couleur) :
        /// réappliqué à chaque donne et à chaque pioche. Un réordonnement à la
        /// main repasse en ordre libre.
        /// </summary>
        public static int HandSort
        {
            get { EnsureLoaded(); return _handSort; }
            set { EnsureLoaded(); _handSort = Mathf.Clamp(value, 0, 2); PlayerPrefs.SetInt(KeyHandSort, _handSort); Notify(); }
        }

        /// <summary>Retour haptique (vibrations courtes) sur mobile.</summary>
        public static bool Haptics
        {
            get { EnsureLoaded(); return _haptics; }
            set { EnsureLoaded(); _haptics = value; PlayerPrefs.SetInt(KeyHaptics, value ? 1 : 0); Notify(); }
        }

        /// <summary>Animations réduites : pas de flash plein écran ni de particules, trajets plus courts.</summary>
        public static bool ReducedMotion
        {
            get { EnsureLoaded(); return _reducedMotion; }
            set { EnsureLoaded(); _reducedMotion = value; PlayerPrefs.SetInt(KeyReducedMotion, value ? 1 : 0); Notify(); }
        }

        /// <summary>
        /// Taille de l'interface (0 = normale, 1 = grande). Par défaut « grande »
        /// sur les petits écrans (téléphones), où la maquette 1920×1080 donne des
        /// textes et des boutons trop petits.
        /// </summary>
        public static int UiScale
        {
            get { EnsureLoaded(); return _uiScale; }
            set { EnsureLoaded(); _uiScale = Mathf.Clamp(value, 0, 1); PlayerPrefs.SetInt(KeyUiScale, _uiScale); Notify(); }
        }

        /// <summary>Facteur d'agrandissement de l'interface correspondant à <see cref="UiScale"/>.</summary>
        public static float UiScaleFactor => UiScale == 1 ? 1.3f : 1f;

        /// <summary>Fluidité maximale (120 i/s si l'écran le permet) ; sinon 60 i/s (batterie).</summary>
        public static bool HighFrameRate
        {
            get { EnsureLoaded(); return _highFps; }
            set { EnsureLoaded(); _highFps = value; PlayerPrefs.SetInt(KeyHighFps, value ? 1 : 0); ApplyFrameRate(); Notify(); }
        }

        /// <summary>Dos de carte choisi (0 champagne, 1 jade, 2 bordeaux).</summary>
        public static int CardBack
        {
            get { EnsureLoaded(); return _cardBack; }
            set { EnsureLoaded(); _cardBack = Mathf.Clamp(value, 0, 2); PlayerPrefs.SetInt(KeyCardBack, _cardBack); Notify(); }
        }

        /// <summary>Tapis choisi (0 vert, 1 nuit, 2 bordeaux).</summary>
        public static int Felt
        {
            get { EnsureLoaded(); return _felt; }
            set { EnsureLoaded(); _felt = Mathf.Clamp(value, 0, 2); PlayerPrefs.SetInt(KeyFelt, _felt); Notify(); }
        }

        /// <summary>Vrai si la variante cosmétique est débloquée (victoires sur cet appareil).</summary>
        public static bool IsCosmeticUnlocked(int variant)
        {
            if (variant < 0 || variant >= CosmeticUnlockWins.Length) return false;
            return PlayerProfile.GamesWon >= CosmeticUnlockWins[variant];
        }

        /// <summary>Règles de la prochaine partie, d'après les réglages enregistrés.</summary>
        public static MatchRules BuildMatchRules()
        {
            EnsureLoaded();
            return new MatchRules(_randomContracts, (SpecialCardsMode)_specialCards, _handSize,
                _levelsToWin, _turnTimer);
        }

        private static void EnsureLoaded()
        {
            if (_loaded) return;
            _musicVolume = PlayerPrefs.GetFloat(KeyMusic, 0.35f);
            _sfxVolume = PlayerPrefs.GetFloat(KeySfx, 0.8f);
            _qualityIndex = PlayerPrefs.GetInt(KeyQuality, 2);
            _colorblindMode = PlayerPrefs.GetInt(KeyColorblind, 0) == 1;
            _playerCount = Mathf.Clamp(PlayerPrefs.GetInt(KeyPlayerCount, 4), 2, 4);
            _difficulty = Mathf.Clamp(PlayerPrefs.GetInt(KeyDifficulty, 1), 0, 2);
            _randomContracts = PlayerPrefs.GetInt(KeyRandomContracts, 0) == 1;
            _specialCards = Mathf.Clamp(PlayerPrefs.GetInt(KeySpecialCards, 0), 0, 2);
            _handSize = Mathf.Clamp(PlayerPrefs.GetInt(KeyHandSize, MatchRules.DefaultHandSize),
                MatchRules.MinHandSize, MatchRules.MaxHandSize);
            _levelsToWin = Mathf.Clamp(PlayerPrefs.GetInt(KeyLevelsToWin, Constants.MaxLevel),
                MatchRules.MinLevelsToWin, Constants.MaxLevel);
            _turnTimer = Mathf.Clamp(PlayerPrefs.GetInt(KeyTurnTimer, 0), 0, MatchRules.MaxTurnTimerSeconds);
            _botSpeed = Mathf.Clamp(PlayerPrefs.GetInt(KeyBotSpeed, 1), 0, 2);
            _showHints = PlayerPrefs.GetInt(KeyHints, 1) == 1;
            _hintContract = PlayerPrefs.GetInt(KeyHintContract, 0) == 1;
            _hintPlayable = PlayerPrefs.GetInt(KeyHintPlayable, 0) == 1;
            _quickDiscard = PlayerPrefs.GetInt(KeyQuickDiscard, 0) == 1;
            _handSort = Mathf.Clamp(PlayerPrefs.GetInt(KeyHandSort, 1), 0, 2);
            _haptics = PlayerPrefs.GetInt(KeyHaptics, 1) == 1;
            _reducedMotion = PlayerPrefs.GetInt(KeyReducedMotion, 0) == 1;
            _uiScale = Mathf.Clamp(PlayerPrefs.GetInt(KeyUiScale, DefaultUiScale()), 0, 1);
            // 60 i/s par défaut sur mobile (batterie, chauffe) ; 120 reste un choix.
            _highFps = PlayerPrefs.GetInt(KeyHighFps, Application.isMobilePlatform ? 0 : 1) == 1;
            _cardBack = Mathf.Clamp(PlayerPrefs.GetInt(KeyCardBack, 0), 0, 2);
            _felt = Mathf.Clamp(PlayerPrefs.GetInt(KeyFelt, 0), 0, 2);
            _loaded = true;
            ApplyQuality();
        }

        /// <summary>Grande interface par défaut sur un écran de moins de 8 pouces (téléphone).</summary>
        private static int DefaultUiScale()
        {
            float dpi = Screen.dpi;
            if (dpi <= 0f || Screen.width <= 0 || Screen.height <= 0)
                return Application.isMobilePlatform ? 1 : 0;
            float diagonal = Mathf.Sqrt(Screen.width * (float)Screen.width + Screen.height * (float)Screen.height) / dpi;
            return diagonal < 8f ? 1 : 0;
        }

        private static void Notify()
        {
            _dirty = true;
            SettingsChanged?.Invoke();
        }

        /// <summary>Écrit les réglages sur le disque s'ils ont changé (pause, fermeture d'un panneau…).</summary>
        public static void Flush()
        {
            if (!_dirty) return;
            _dirty = false;
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Qualité : BAS / MOYEN / ÉLEVÉ visent les niveaux Low / High / Ultra
        /// (anticrénelage, synchro verticale) — l'ancien mapping 0-1-2 tombait sur
        /// Very Low / Low / Medium, sans différence visible.
        /// </summary>
        private static void ApplyQuality()
        {
            int levels = QualitySettings.names.Length;
            if (levels == 0) return;
            int target = _qualityIndex switch
            {
                0 => 1,
                1 => levels / 2,
                _ => levels - 1
            };
            QualitySettings.SetQualityLevel(Mathf.Clamp(target, 0, levels - 1), applyExpensiveChanges: true);
            ApplyFrameRate();
        }

        /// <summary>Cadence d'affichage (la synchro verticale des niveaux de qualité est coupée : elle plafonnerait à 60).</summary>
        public static void ApplyFrameRate()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = _highFps ? 120 : 60;
        }

        /// <summary>Force le chargement initial (appelé au démarrage).</summary>
        public static void Initialize()
        {
            EnsureLoaded();
            ApplyFrameRate();
        }
    }
}
