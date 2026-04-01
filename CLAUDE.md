# CLAUDE.md — Level Up (Level 8 Ravensburger) Unity Project

## Rôle de Claude dans ce projet
Tu es un développeur Unity senior C# travaillant sur l'adaptation digitale du jeu de
cartes Level Up (Level 8) de Ravensburger. Ce fichier est ta référence absolue : lis-le
en entier avant d'écrire la moindre ligne de code.

---

## Stack & contraintes techniques

| Élément | Choix |
|---|---|
| Moteur | Unity 2022.3 LTS |
| Rendu | URP (Universal Render Pipeline) |
| Input | New Input System (pas l'ancien) |
| UI | UI Toolkit **ou** Canvas avec Safe Area — adaptatif portrait/paysage |
| Langage | C# 9, nullable enabled, pas de code unsafe |
| Architecture | ScriptableObject-driven + C# events (pas de singletons directs) |
| Packages tiers | Aucun sauf packages Unity officiels |
| Cibles | iOS, Android, Windows, macOS, Linux |

---

## Règles du jeu (source de vérité)

### Contenu du deck
- 108 cartes  (peut etre qu'on pourra modifier par des paramètres) : valeurs 1–18, 6 couleurs (Rouge, Bleu, Vert, Jaune, Violet, Orange)

### Déroulement d'un tour
1. **Piocher** : top du deck OU top de n'importe quelle défausse
2. **Poser son niveau** (optionnel) : si la main valide le
   niveau actuel, étaler les cartes requises face visible
3. **Ajouter aux combinaisons** (optionnel, uniquement après avoir posé son niveau) :
   ajouter des cartes aux combinaisons déjà posées (les siennes ou celles des adversaires)
4. **Défausser** : obligatoire, 1 carte sur sa propre défausse. Jouer une carte action
   compte comme défausse.

### Fin de round
Le round se termine quand un joueur vide sa main (défausse sa dernière carte).
- Le vainqueur du round **saute un niveau** (bonus : passe directement au niveau suivant)
- Les autres joueurs qui avaient posé leur niveau avancent d'un niveau
- Les joueurs qui n'ont pas posé leur niveau restent au même niveau

### Les 8 niveaux (ordre croissant de difficulté, pareil on doit pourvoir les modifier par des paramètres)
```
1 → 2 suites de 3 cartes consécutives
2 → 1 suite de 3 + 1 brelan (3 cartes identiques)
3 → 2 brelans
4 → 1 suite de 4 + 1 paire
5 → 1 flush de 5 (5 cartes même couleur)
6 → 1 suite de 5
7 → 1 carré (4 identiques) + 1 paire
8 → 1 flush de 7 (7 cartes même couleur)
```
> Une suite : cartes de valeurs consécutives (les Wilds peuvent combler un écart).
> Un brelan/carré : même valeur (les Wilds peuvent compléter).
> Un flush : même couleur (les Wilds comptent comme n'importe quelle couleur).

### Victoire
Premier joueur à compléter le Niveau 8 **et** vider sa main dans le même round.

---

## Architecture cible

```
Assets/Scripts/
├── Core/
│   ├── CardModel.cs          # struct immuable : value, color, type
│   ├── DeckManager.cs        # shuffle (Fisher-Yates), deal, draw, discard
│   ├── PlayerModel.cs        # hand, currentLevel, hasLaidThisRound
│   ├── LevelValidator.cs     # static IsLevelComplete(cards, level) — 8 règles
│   ├── TurnManager.cs        # ordre des tours, gestion des actions
│   ├── GameManager.cs        # FSM : Setup→PlayerTurn→Validate→EndRound→GameOver
│   └── ActionCardHandler.cs  # effets Skip, Draw2, Wild, WildDraw2
├── UI/
│   ├── CardView.cs           # affichage d'une carte (couleur, valeur, animation)
│   ├── HandView.cs           # main du joueur local, drag & drop
│   ├── TableView.cs          # combinaisons posées sur la table
│   ├── HUDView.cs            # scores, niveau actuel, deck restant
│   ├── LevelProgressView.cs  # barre de progression des 8 niveaux
│   └── AnimationController.cs# tweens pioche/pose/défausse (DOTween-free)
├── AI/
│   └── AIPlayer.cs           # heuristique : évalue main, pose niveau, défausse utile
├── Network/
│   └── NetworkManager.cs     # stub multijoueur local (pass-and-play)
└── Utils/
    ├── Constants.cs          # toutes les constantes du jeu
    ├── CardExtensions.cs     # méthodes utilitaires sur List<CardModel>
    └── EventBus.cs           # système d'événements découplé
```

---

## Conventions de code

```csharp
// Namespace obligatoire sur chaque fichier
namespace LevelUp.Core { }
namespace LevelUp.UI    { }
namespace LevelUp.AI    { }

// Summary XML sur toutes les classes et méthodes publiques
/// <summary>
/// Valide si une main satisfait les exigences d'un niveau donné.
/// </summary>

// Events typés, pas de SendMessage ni de Find
public static event Action<PlayerModel> OnLevelCompleted;

// ScriptableObjects pour les données configurables (pas de magic numbers dans le code)
[CreateAssetMenu(fileName = "GameConfig", menuName = "LevelUp/GameConfig")]
public class GameConfig : ScriptableObject { ... }

// CardModel est une struct immuable
public readonly struct CardModel { ... }
```

---

## Règles de style visuel

- **Palette** : fond sombre (navy #1A1F3C), cartes colorées vives (rouge #E84545,
  bleu #4585E8, vert #45C878, jaune #F5C842)
- **Typographie** : fonte bold, chiffres larges et centrés sur les cartes
- **Cartes** : coins arrondis, ombre portée légère, ratio 2:3
- **Animations** : pioche (carte surgit du deck), pose (carte glisse vers la table),
  défausse (carte tombe en fondu) — durée max 0.3s chacune
- **Responsive** : portrait sur mobile (main en bas, table au centre, HUD en haut),
  paysage sur PC/tablette (main à gauche, table au centre, HUD à droite)
- **Safe Area** : toujours respectée sur iOS/Android

---

## Fichiers à créer (dans l'ordre)

1. `.gitignore` — standard Unity
2. `Packages/manifest.json` — com.unity.inputsystem, com.unity.render-pipelines.universal, com.unity.textmeshpro
3. `Assets/Scripts/Utils/Constants.cs`
4. `Assets/Scripts/Utils/EventBus.cs`
5. `Assets/Scripts/Core/CardModel.cs`
6. `Assets/Scripts/Core/DeckManager.cs`
7. `Assets/Scripts/Core/PlayerModel.cs`
8. `Assets/Scripts/Core/LevelValidator.cs`
9. `Assets/Scripts/Core/ActionCardHandler.cs`
10. `Assets/Scripts/Core/TurnManager.cs`
11. `Assets/Scripts/Core/GameManager.cs`
12. `Assets/Scripts/UI/CardView.cs`
13. `Assets/Scripts/UI/HandView.cs`
14. `Assets/Scripts/UI/TableView.cs`
15. `Assets/Scripts/UI/HUDView.cs`
16. `Assets/Scripts/UI/LevelProgressView.cs`
17. `Assets/Scripts/UI/AnimationController.cs`
18. `Assets/Scripts/AI/AIPlayer.cs`
19. `Assets/Scripts/Network/NetworkManager.cs`
20. `Assets/Scripts/Core/Core.asmdef` + `UI.asmdef` + `AI.asmdef`
21. `README.md`

> **Règle absolue** : génère le contenu complet de chaque fichier. Aucun placeholder,
> aucun `// TODO`, aucun résumé. Du code C# compilable dans Unity 2022.3.

---

## Ce que Claude NE doit PAS faire

- ❌ Utiliser `GameObject.Find()` ou `FindObjectOfType()` dans la logique Core
- ❌ Utiliser des singletons classiques (utiliser EventBus + injection)
- ❌ Importer DOTween ou asset store non officiel
- ❌ Écrire du pseudo-code ou des stubs vides
- ❌ Ignorer le responsive mobile/PC
- ❌ Oublier les namespaces ou les summaries XML

---

## Démarrage rapide pour Claude Code

```bash
# Après clonage du repo :
# 1. Ouvrir dans Unity Hub → Unity 2022.3 LTS
# 2. Package Manager → importer depuis manifest.json
# 3. Project Settings → Player → Company "Indie" / Product "LevelUp"
# 4. Build Settings → ajouter les scènes MainMenu et Game
# 5. Mobile : activer "Auto Rotation" (portrait + landscape)
```
