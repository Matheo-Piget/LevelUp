namespace LevelUp.Core
{
    /// <summary>
    /// État de session partagé entre Core, UI et Network : qui est le joueur
    /// local sur cet appareil, et la partie est-elle en ligne.
    ///
    /// En solo/pass-and-play : IsOnline = false et tout humain est "local".
    /// En ligne : chaque appareil ne contrôle QUE le siège LocalPlayerIndex —
    /// l'UI doit refuser d'émettre des commandes pour les autres sièges.
    /// </summary>
    public static class GameSession
    {
        /// <summary>Vrai quand une partie multijoueur en ligne est active.</summary>
        public static bool IsOnline { get; set; }

        /// <summary>Vrai si cet appareil est l'hôte de la partie en ligne.</summary>
        public static bool IsHost { get; set; }

        /// <summary>Index du joueur contrôlé par cet appareil (0 hors ligne).</summary>
        public static int LocalPlayerIndex { get; set; }

        /// <summary>
        /// Contrats de la partie en cours (fixés par <see cref="GameManager.StartGame"/>).
        /// Lu par <see cref="LevelValidator"/> : règles, IA et UI voient le même plan.
        /// </summary>
        public static ContractPlan Contracts { get; set; } = ContractPlan.Classic;

        /// <summary>
        /// Règles de la partie en cours (fixées par <see cref="GameManager.StartGame(System.Collections.Generic.IReadOnlyList{PlayerSetup}, int, MatchRules)"/>) :
        /// niveau final, minuteur, cartes spéciales… Lues par l'UI (barre de niveau, minuteur).
        /// </summary>
        public static MatchRules Rules { get; set; } = MatchRules.Classic;

        // Sans rechargement de domaine (Enter Play Mode Options), les statiques
        // survivent entre deux Play : on repart d'un état propre.
        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Reset();
            Contracts = ContractPlan.Classic;
            Rules = MatchRules.Classic;
        }

        /// <summary>Retour à l'état hors ligne (solo).</summary>
        public static void Reset()
        {
            IsOnline = false;
            IsHost = false;
            LocalPlayerIndex = 0;
        }

        /// <summary>
        /// Vrai si ce joueur est un humain contrôlable depuis CET appareil.
        /// Hors ligne, tout humain est local (comportement solo inchangé).
        /// </summary>
        public static bool IsLocalHuman(PlayerModel player)
        {
            if (player == null || player.IsAI) return false;
            return !IsOnline || player.Index == LocalPlayerIndex;
        }

        /// <summary>
        /// Le joueur contrôlé par cet appareil. Hors ligne : premier humain
        /// de la liste (index 0 en pratique).
        /// </summary>
        public static PlayerModel? GetLocalPlayer(GameManager? gameManager)
        {
            if (gameManager == null) return null;

            if (IsOnline)
            {
                var players = gameManager.Players;
                return LocalPlayerIndex >= 0 && LocalPlayerIndex < players.Count
                    ? players[LocalPlayerIndex]
                    : null;
            }

            // Hors ligne : le siège affiché sur cet appareil s'il est humain
            // (pass-and-play : il change à chaque passage), sinon le premier humain.
            var all = gameManager.Players;
            if (LocalPlayerIndex >= 0 && LocalPlayerIndex < all.Count && !all[LocalPlayerIndex].IsAI)
                return all[LocalPlayerIndex];
            foreach (PlayerModel p in all)
            {
                if (!p.IsAI) return p;
            }
            return null;
        }
    }
}
