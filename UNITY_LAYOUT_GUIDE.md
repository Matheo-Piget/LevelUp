# Guide de layout Unity — LevelUp

Guide précis pour positionner chaque élément de `Game.unity` sans chevauchement, avec un rendu propre en 1920×1080 (s'adapte automatiquement aux autres résolutions grâce au Canvas Scaler).

---

## 0. Pré-requis : Canvas Scaler

Avant tout, sélectionne **Canvas** dans la Hierarchy et configure :

| Propriété | Valeur |
|---|---|
| `UI Scale Mode` | Scale With Screen Size |
| `Reference Resolution` | 1920 × 1080 |
| `Screen Match Mode` | Match Width Or Height |
| `Match` | 0.5 |

Tous les pixels ci-dessous sont exprimés en coordonnées de référence (1920×1080). Unity gère la mise à l'échelle automatiquement.

---

## 1. Vue d'ensemble du layout

```
┌──────────────────────────────────────────────────────┐  y = +540 (top)
│  HUD : Round │ Deck │ Joueur │ Phase │ Niveau        │
├──────────────────────────────────────────────────────┤  y = +440
│                                                       │
│       DISCARD PILES DES 4 JOUEURS (en ligne)          │
│                                                       │
├──┬─────────────────────────────────────────────┬─────┤  y = +230
│  │                                              │     │
│  │   ┌─────┐  ┌─────┐    ┌─────────────────┐   │     │
│  │L │DECK │  │DISC │    │ TABLE (melds    │   │ L   │  y = 0
│  │P │     │  │     │    │  du joueur)     │   │ P   │
│  │  └─────┘  └─────┘    └─────────────────┘   │     │
│  │                                              │     │
├──┴─────────────────────────────────────────────┴─────┤  y = -140
│                                                       │
│    🂠 🂡 🂢 🂣 🂤 🂥 🂦 🂧 🂨 🂩   (main du joueur en éventail) │
│                                                       │
└──────────────────────────────────────────────────────┘  y = -540 (bottom)
```

---

## 2. Hiérarchie cible

```
Canvas
├── HUD                      (top bar)
├── DiscardPileView          (ligne de défausses)
├── LevelProgress            (colonne gauche)
├── PlayZone
│   ├── DeckArea
│   ├── DiscardArea
│   └── Table / TableContainer
├── PlayerHand
│   └── HandContainer
├── TurnBannerView           (overlay temporaire)
└── BalatroEffect            (overlay effets)
```

---

## 3. Configuration détaillée par GameObject

Pour chaque GameObject : sélectionne-le, puis dans le **Rect Transform**, clique sur l'icône d'ancres (carré en haut à gauche du Rect Transform) et choisis le **preset d'ancre** indiqué.

### 3.1 HUD (barre du haut)

| Champ | Valeur |
|---|---|
| Parent | `Canvas` |
| Anchor preset | **top-stretch** (top, horizontal stretch) |
| Left | 20 |
| Right | 20 |
| Pos Y | -10 |
| Height | 90 |
| Pivot | X: 0.5, Y: 1 |

**Enfants du HUD** (tous ancrés par rapport au HUD lui-même) :

| Enfant | Anchor | Pos X | Pos Y | Width | Height | Font size | Color |
|---|---|---|---|---|---|---|---|
| `RounNumberText` | middle-left | 20 | 10 | 180 | 30 | 16 | Gris clair |
| `DeckLabel` (+ icône) | middle-left | 210 | 10 | 60 | 30 | 14 | Gris |
| `DeckCountText` | middle-left | 280 | 10 | 80 | 40 | 26 | Jaune doré |
| `CurrentPlayerText` | middle-center | 0 | 15 | 400 | 36 | 22 | Couleur joueur |
| `CurrentPhaseText` | middle-center | 0 | -20 | 700 | 28 | 16 | Couleur phase |
| `CurrentLevelText` | middle-right | -20 | 0 | 500 | 50 | 22 | Jaune doré |
| `StatusText` | middle-center | 0 | 0 | 900 | 60 | 28 | Blanc (alpha géré par script) |

> **Note** : `StatusText` est un overlay qui apparaît/disparaît. Il peut recouvrir `CurrentPhaseText` temporairement, c'est normal.

---

### 3.2 DiscardPileView (défausses de tous les joueurs)

Affiche la dernière carte défaussée par chaque joueur en ligne horizontale.

| Champ | Valeur |
|---|---|
| Parent | `Canvas` |
| Anchor preset | **top-center** |
| Pos X | 0 |
| Pos Y | -110 |
| Width | 1200 |
| Height | 200 |
| Pivot | X: 0.5, Y: 1 |

**Composant à ajouter** : `Add Component → DiscardPileView`

> Ce GameObject couvre y = [230, 430]. Aucun chevauchement avec le HUD (qui descend à y=440) ni avec la zone de jeu (qui commence à y=230).

---

### 3.3 LevelProgress (colonne gauche)

| Champ | Valeur |
|---|---|
| Parent | `Canvas` |
| Anchor preset | **middle-left** |
| Pos X | 160 |
| Pos Y | -50 |
| Width | 280 |
| Height | 520 |
| Pivot | X: 0.5, Y: 0.5 |

---

### 3.4 PlayZone (conteneur central)

Regroupe Deck, Discard single pile et Table. Crée un GameObject vide nommé `PlayZone` sous Canvas.

| Champ | Valeur |
|---|---|
| Parent | `Canvas` |
| Anchor preset | **middle-center** |
| Pos X | 60 |
| Pos Y | 45 |
| Width | 1100 |
| Height | 340 |
| Pivot | X: 0.5, Y: 0.5 |

#### 3.4.a DeckArea (pile de pioche)

| Champ | Valeur |
|---|---|
| Parent | `PlayZone` |
| Anchor preset | **middle-left** |
| Pos X | 90 |
| Pos Y | 0 |
| Width | 130 |
| Height | 190 |

#### 3.4.b DiscardArea (défausse unique au centre)

| Champ | Valeur |
|---|---|
| Parent | `PlayZone` |
| Anchor preset | **middle-left** |
| Pos X | 240 |
| Pos Y | 0 |
| Width | 130 |
| Height | 190 |

#### 3.4.c Table / TableContainer (melds du joueur local)

| Champ | Valeur |
|---|---|
| Parent | `PlayZone` |
| Anchor preset | **middle-right** |
| Pos X | -50 |
| Pos Y | 0 |
| Width | 680 |
| Height | 280 |

> À l'intérieur de `Table`, le `TableContainer` doit remplir son parent : anchor stretch-stretch, tous offsets à 0.

---

### 3.5 PlayerHand (main du joueur)

| Champ | Valeur |
|---|---|
| Parent | `Canvas` |
| Anchor preset | **bottom-stretch** |
| Left | 40 |
| Right | 40 |
| Pos Y | 140 |
| Height | 260 |
| Pivot | X: 0.5, Y: 0 |

#### 3.5.a HandContainer (enfant de PlayerHand)

| Champ | Valeur |
|---|---|
| Parent | `PlayerHand` |
| Anchor preset | **stretch-stretch** (fill) |
| Left | 0, Right | 0 |
| Top | 0, Bottom | 0 |
| Pivot | X: 0.5, Y: 0.5 |

> Dans le composant `HandView`, référence bien `HandContainer` dans le champ `_handContainer`.

---

### 3.6 TurnBannerView (bannière temporaire "Tour de X")

| Champ | Valeur |
|---|---|
| Parent | `Canvas` |
| Anchor preset | **middle-center** |
| Pos X | 0 |
| Pos Y | 0 |
| Width | 800 |
| Height | 120 |
| Pivot | X: 0.5, Y: 0.5 |

**Composant à ajouter** : `Add Component → TurnBannerView`

> Par défaut invisible. Le script l'anime à l'événement `TurnStartedEvent`.

---

### 3.7 BalatroEffect (overlay effets visuels)

| Champ | Valeur |
|---|---|
| Parent | `Canvas` |
| Anchor preset | **stretch-stretch** (fill) |
| Left, Right, Top, Bottom | 0 |

> Déjà existant dans la scène. Vérifie juste qu'il couvre tout le canvas.

---

## 4. Vérification finale des zones (pas de chevauchement)

| Zone | Plage Y | Plage X |
|---|---|---|
| HUD | 440 → 540 | -960 → +960 |
| DiscardPileView | 230 → 430 | -600 → +600 |
| LevelProgress | -310 → +210 | -860 → -580 |
| PlayZone (deck+discard+table) | -125 → +215 | -490 → +610 |
| PlayerHand | -400 → -140 | -920 → +920 |
| TurnBannerView | -60 → +60 | -400 → +400 (overlay temporaire) |

Gaps sains :
- HUD ↔ DiscardPileView : 10 px (y=430 → 440)
- DiscardPileView ↔ PlayZone : 15 px (y=215 → 230)
- PlayZone ↔ PlayerHand : 15 px (y=-140 → -125)

---

## 5. Références à assigner sur GameBootstrapper

Dans l'Inspector du GameObject racine qui porte `GameBootstrapper`, **drag & drop** :

| Champ | GameObject à glisser |
|---|---|
| `Game Manager` | GameManager |
| `Hand View` | PlayerHand |
| `Table View` | Table |
| `Hud View` | HUD |
| `Level Progress View` | LevelProgress |
| `Anim Controller` | GameObject qui porte AnimationController |
| `Input Controller` | GameObject qui porte GameInputController |
| `Balatro Effects` | BalatroEffect |
| `Discard Pile View` | DiscardPileView |
| `Turn Banner View` | TurnBannerView |
| `Main Canvas` | Canvas |
| `Network Manager` | GameObject avec NetworkManager (optionnel) |

---

## 6. Références à assigner sur AnimationController

| Champ | GameObject à glisser |
|---|---|
| `Deck Position` | DeckArea |
| `Discard Position` | DiscardArea |
| `Table Center` | Table |
| `Effect Canvas` | Canvas |

---

## 7. Références à assigner sur HandView (GameObject PlayerHand)

| Champ | Valeur |
|---|---|
| `Hand Container` | HandContainer (enfant) |
| `Card Prefab` | ton prefab de carte (ex: Assets/Prefabs/Card) |
| `Card Spacing` | 100 |
| `Max Hand Width` | 1400 |
| `Anim Controller` | GameObject AnimationController |

---

## 8. Références à assigner sur HUDView (GameObject HUD)

| Champ | GameObject enfant |
|---|---|
| `Current Player Text` | CurrentPlayerText |
| `Current Phase Text` | CurrentPhaseText |
| `Current Level Text` | CurrentLevelText |
| `Deck Count Text` | DeckCountText |
| `Round Number Text` | RounNumberText |
| `Status Text` | StatusText |
| `Status Canvas Group` | CanvasGroup sur StatusText (ajoute-le si absent) |
| `Game Over Panel` | *(laisse vide — géré par GameOverCelebration)* |
| `Winner Text` | *(laisse vide)* |
| `Anim Controller` | GameObject AnimationController |

---

## 9. Étapes de nettoyage (à supprimer)

Les éléments suivants sont obsolètes avec le nouveau système. **Supprime-les** de la Hierarchy :

- `Canvas/PassScreenPanel` (auto-pass maintenant géré par script)
- `Canvas/SkipButton`
- `Canvas/LayDownButton`
- `Canvas/SkipMeldsButton`
- `Canvas/ButtonAction`
- `Canvas/HUD/GameOverPanel` (remplacé par GameOverCelebration)
- Tout enfant `Winner` / `PassScreenText` orphelin
- `StatueGroup` (si vide, typo de StatusGroup probablement inutilisé)

---

## 10. Méthode pour ajuster visuellement dans Unity

1. **Window → Layouts → 2 by 3** ou `Wide` pour avoir un gros Scene view.
2. Dans le **Game view**, clique sur la résolution en haut à gauche et choisis **1920×1080 Full HD**.
3. Pour chaque GameObject UI :
   - Clique dessus dans la Hierarchy
   - Dans le Rect Transform, clique sur l'**icône d'ancre** (carré en haut à gauche)
   - Maintiens **Alt** + clic sur le preset désiré → positionne ET ancre en un geste
   - Ajuste les valeurs Pos X, Pos Y, Width, Height selon le tableau
4. Appuie sur **F** dans la Scene view pour zoomer sur l'élément sélectionné.
5. Utilise l'outil **Rect Tool** (touche **T**) pour redimensionner visuellement au besoin.

---

## 11. Test et validation

Après configuration :

1. **Play** (▶).
2. Vérifie que le menu principal s'affiche avec un fade-in propre.
3. Clique **JOUER** → tu dois voir :
   - HUD en haut avec les bonnes infos
   - Ta main de 10 cartes en éventail en bas
   - DiscardPileView en haut avec les défausses des 4 joueurs
   - Deck + Discard + Table alignés au centre
   - LevelProgress à gauche avec les niveaux des joueurs
4. Appuie **ESC** → menu pause avec fond flouté.
5. Fais une partie complète → écran de fin avec confettis.

---

## 12. Troubleshooting

| Problème | Cause probable | Solution |
|---|---|---|
| UI décalée sur un autre écran | Canvas Scaler mal configuré | Étape 0 |
| Cartes coupées en bas | PlayerHand Pos Y trop petit | Augmente à 160 |
| Chevauchement HUD/Discard | Discard Pos Y trop haut | Mets Pos Y = -110 |
| Table invisible | Anchor preset non stretch | Force middle-center + taille fixe |
| LevelProgress déborde à droite | Pos X négatif ou trop grand | Mets Pos X = 160 exact |
| Ma main ne s'affiche pas | `HandContainer` pas référencé dans HandView | Étape 7 |
| Écran de fin basique | `GameOverPanel` de HUDView pas vide | Étape 8, laisse vide |

---

## 13. Ajustements mobiles (optionnel)

Pour un rendu correct sur smartphone (ratios verticaux 9:16) :

- Canvas Scaler `Match` → change à `0` (priorité largeur) pour que tout s'adapte à la largeur.
- Tu peux aussi créer deux layouts (`HUD_Landscape`, `HUD_Portrait`) activés selon l'orientation, mais c'est un second temps.

---

## 14. Couleurs de référence (cohérence visuelle)

Déjà définies dans `Constants.cs`, à réutiliser pour tout nouveau texte :

| Usage | Hex | Couleur C# |
|---|---|---|
| Fond principal | `#0F1923` | `BackgroundDark` |
| Panels | `#131B28` (DD alpha) | `PanelBackground` |
| Bordures | `#2A3A50` | `PanelBorder` |
| Texte principal | `#E8EDF2` | `TextPrimary` |
| Texte secondaire | `#7A8BA0` | `TextSecondary` |
| Texte accent (doré) | `#FFD94D` | `TextAccent` |
| Couleur joueur 1 | `#4D9AFF` (bleu) | `CardBlue` |
| Couleur joueur 2 | `#FF4D6A` (rouge) | `CardRed` |
| Couleur joueur 3 | `#4DFF91` (vert) | `CardGreen` |
| Couleur joueur 4 | `#BB6BFF` (violet) | `CardPurple` |

---

**Une fois ce guide appliqué, ton layout sera propre, aéré, sans chevauchement, et responsive.** Si un élément te semble mal placé visuellement en Play Mode, ajuste le Pos Y de 20-40 px dans la direction voulue plutôt que de tout refaire — les plages du tableau §4 te laissent de la marge.
