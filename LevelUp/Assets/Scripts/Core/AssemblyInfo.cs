using System.Runtime.CompilerServices;

// Donne accès aux membres internal du Core à l'assembly de tests EditMode.
// Permet aux tests d'écrire dans les setters internal (PlayerModel.SkipCount,
// HasLaidDownThisRound, etc.) et d'appeler les méthodes internal (AddToHand,
// RemoveFromHand…) sans relâcher leur visibilité pour le reste du projet.
[assembly: InternalsVisibleTo("LevelUp.Tests.EditMode")]
