# Guide de configuration Unity — Level Up

Ce document te guide pas-a-pas pour configurer le projet Unity.
Suis chaque etape dans l'ordre. Coche au fur et a mesure.

---

## 1. Premier lancement et compilation

### 1.1 Activer le New Input System
1. Ouvre le projet dans Unity Hub (Unity 2022.3 LTS)
2. Va dans **Edit > Project Settings > Player**
3. Descends jusqu'a **Other Settings > Configuration**
4. Change **Active Input Handling** en **Both** (pour garder l'ancien en fallback)
5. Unity va te demander de redemarrer — accepte

### 1.2 Verifier les packages
1. Va dans **Window > Package Manager**
2. Verifie que ces packages sont installes (sinon installe-les) :
   - **Input System** (com.unity.inputsystem)
   - **Universal RP** (com.unity.render-pipelines.universal)
   - **TextMeshPro** (com.unity.textmeshpro)
3. Si Unity te demande d'importer les "TMP Essential Resources", clique **Import**

### 1.3 Verifier la compilation
1. Regarde la console Unity (**Window > General > Console**)
2. Il ne doit y avoir **aucune erreur rouge**
3. Si des erreurs persistent, ferme Unity, supprime le dossier `Library/`, et reouvre le projet

---

## 2. Creer le ScriptableObject GameConfig

1. Dans la fenetre **Project**, fais clic droit dans le dossier `Assets/`
2. Clique **Create > LevelUp > GameConfig**
3. Renomme le fichier en `GameConfig`
4. Selectionne-le et verifie dans l'**Inspector** :

| Champ | Valeur |
|---|---|
| Deck Size | 108 |
| Card Min Value | 1 |
| Card Max Value | 18 |
| Cards Per Player | 10 |
| Min Players | 2 |
| Max Players | 6 |
| Level Definitions | Laisser vide (les 8 niveaux par defaut sont codes en dur) |

---

## 3. Creer le prefab Card

C'est le visuel d'UNE carte. On en fera des copies a l'execution.

### 3.1 Creer le Canvas principal (temporaire pour construire le prefab)

1. Clic droit dans la **Hierarchy > UI > Canvas**
2. Selectionne le Canvas et dans l'Inspector :
   - **Canvas** :
     - Render Mode : **Screen Space - Overlay**
   - **Canvas Scaler** :
     - UI Scale Mode : **Scale With Screen Size**
     - Reference Resolution : **1080 x 1920**
     - Screen Match Mode : **Match Width Or Height**
     - Match : **0.5**

### 3.2 Construire la carte

Toutes les manipulations se font dans la Hierarchy, sous le Canvas.

#### 3.2.1 Le GameObject racine "Card"
1. Clic droit sur le Canvas > **UI > Image**
2. Renomme en `Card`
3. Dans l'Inspector :
   - **RectTransform** :
     - Width : **120**
     - Height : **180**
   - **Image** :
     - Color : **blanc** (#FFFFFF)
   - Ajoute un composant : **Canvas Group**
     - (Laisse Alpha = 1, Interactable = true, Blocks Raycasts = true)
   - Ajoute le script : **CardView** (il est dans LevelUp.UI)

#### 3.2.2 Enfant "CardFront" (conteneur du recto)
1. Clic droit sur `Card` > **Create Empty**
2. Renomme en `CardFront`
3. Dans l'Inspector :
   - **RectTransform** : clique sur le carre d'ancrage, maintiens **Alt** et clique sur le dernier carre en bas a droite (**Stretch Stretch**) pour remplir le parent
   - Verifie : Left = 0, Right = 0, Top = 0, Bottom = 0

#### 3.2.3 Enfant "CardBack" (verso de la carte)
1. Clic droit sur `Card` > **UI > Image**
2. Renomme en `CardBack`
3. Dans l'Inspector :
   - **RectTransform** : Stretch Stretch (comme CardFront)
   - **Image** : Color = **#2C3150** (bleu sombre)
4. Desactive le GameObject CardBack (decoche la case en haut de l'Inspector)

#### 3.2.4 Enfant "Border" (bordure coloree)
1. Clic droit sur `CardFront` > **UI > Image**
2. Renomme en `Border`
3. Dans l'Inspector :
   - **RectTransform** : Stretch Stretch
   - **Image** : Color = **#E84545** (rouge par defaut, sera change par le script)

#### 3.2.5 Enfant "ColorBand" (bande de couleur sur le cote gauche)
1. Clic droit sur `CardFront` > **UI > Image**
2. Renomme en `ColorBand`
3. Dans l'Inspector :
   - **RectTransform** :
     - Ancrage : **Left Stretch** (etire verticalement a gauche)
     - Left : 0, Top : 5, Bottom : 5
     - Width : **8**
   - **Image** : Color = **#E84545** (sera change par le script)

#### 3.2.5b Enfant "Shadow" (ombre portee)
1. Clic droit sur `Card` (PAS CardFront) > **UI > Image**
2. Renomme en `Shadow`
3. Dans l'Inspector :
   - **RectTransform** :
     - Ancrage : **Stretch Stretch**
     - Left : 3, Right : -3, Top : -3, Bottom : 3 (decale en bas-droite)
   - **Image** : Color = **#00000040** (noir 25% opacity)
4. **IMPORTANT** : dans la Hierarchy, drag `Shadow` pour qu'il soit **AU-DESSUS** de `CardFront` (= rendu derriere)
   - L'ordre doit etre : Shadow, CardFront, CardBack

#### 3.2.6 Enfant "ValueText" (valeur centrale)
1. Clic droit sur `CardFront` > **UI > Text - TextMeshPro**
2. Si Unity demande d'importer TMP Essentials, clique **Import**
3. Renomme en `ValueText`
4. Dans l'Inspector :
   - **RectTransform** : Stretch Stretch (remplit le parent)
   - **TextMeshPro** :
     - Text : `8` (texte de preview)
     - Font Size : **48**
     - Alignment : **Center** + **Middle** (les deux boutons centraux)
     - Font Style : **Bold**
     - Color : **#E84545**

#### 3.2.7 Enfant "CornerValueTL" (coin haut-gauche)
1. Clic droit sur `CardFront` > **UI > Text - TextMeshPro**
2. Renomme en `CornerValueTL`
3. Dans l'Inspector :
   - **RectTransform** :
     - Ancrage : **Top Left**
     - Pos X : **15**, Pos Y : **-8**
     - Width : **30**, Height : **25**
   - **TextMeshPro** :
     - Text : `8`
     - Font Size : **18**
     - Alignment : **Left** + **Top**
     - Font Style : **Bold**
     - Color : **#E84545**

#### 3.2.8 Enfant "CornerValueBR" (coin bas-droite)
1. Clic droit sur `CardFront` > **UI > Text - TextMeshPro**
2. Renomme en `CornerValueBR`
3. Dans l'Inspector :
   - **RectTransform** :
     - Ancrage : **Bottom Right**
     - Pos X : **-15**, Pos Y : **8**
     - Width : **30**, Height : **25**
     - Rotation Z : **180** (retourne le texte)
   - **TextMeshPro** :
     - Text : `8`
     - Font Size : **18**
     - Alignment : **Left** + **Top**
     - Font Style : **Bold**
     - Color : **#E84545**

### 3.3 Connecter le script CardView

1. Selectionne le GameObject `Card`
2. Dans l'Inspector, dans le composant **CardView**, drag & drop :

| Champ CardView | GameObject a assigner |
|---|---|
| Card Front | `CardFront` (le GameObject) |
| Card Back | `CardBack` (le GameObject) |
| Background | `Card` (son propre composant Image) |
| Border | `CardFront/Border` (composant Image) |
| Color Band | `CardFront/ColorBand` (composant Image) |
| Shadow Image | `Shadow` (composant Image) |
| Value Text | `CardFront/ValueText` (composant TextMeshProUGUI) |
| Corner Value Top Left | `CardFront/CornerValueTL` (composant TextMeshProUGUI) |
| Corner Value Bottom Right | `CardFront/CornerValueBR` (composant TextMeshProUGUI) |
| Canvas Group | `Card` (son propre composant CanvasGroup) |

### 3.4 Sauvegarder comme Prefab

1. Dans la fenetre **Project**, cree un dossier `Assets/Prefabs/`
2. **Drag & drop** le GameObject `Card` depuis la Hierarchy vers le dossier `Assets/Prefabs/`
3. Ca cree un fichier bleu `Card.prefab`
4. **Supprime** le GameObject `Card` de la Hierarchy (le prefab est sauvegarde)
5. Tu peux aussi supprimer le Canvas temporaire

---

## 4. Construire la scene de jeu "Game"

### 4.1 Creer la scene

1. **File > New Scene** > choisir **Basic (Built-in)**
2. **File > Save As** > sauvegarde dans `Assets/Scenes/Game.unity`

### 4.2 Configurer la Camera

1. Selectionne **Main Camera** dans la Hierarchy
2. Dans l'Inspector :
   - **Camera** :
     - Clear Flags : **Solid Color**
     - Background : **#1A1F3C** (navy sombre)
   - Si tu as URP, verifie que la camera utilise le bon renderer

### 4.3 Creer le Canvas principal

1. Clic droit dans la Hierarchy > **UI > Canvas**
2. Selectionne le Canvas, dans l'Inspector :
   - **Canvas** :
     - Render Mode : **Screen Space - Overlay**
   - **Canvas Scaler** :
     - UI Scale Mode : **Scale With Screen Size**
     - Reference Resolution : **1080 x 1920**
     - Screen Match Mode : **Match Width Or Height**
     - Match : **0.5**
3. Ajoute un composant **Canvas Group** sur le Canvas (utile pour bloquer les inputs globalement)

### 4.4 Creer la zone HUD (haut de l'ecran)

1. Clic droit sur Canvas > **Create Empty**
2. Renomme en `HUD`
3. **RectTransform** :
   - Ancrage : **Top Stretch** (top bar)
   - Left : 0, Right : 0, Top : 0
   - Height : **200**
4. Ajoute le script **HUDView**

#### Enfants du HUD :

**4.4.1 — RoundNumberText**
1. Clic droit sur HUD > **UI > Text - TextMeshPro**
2. Renomme en `RoundNumberText`
3. RectTransform : Ancrage **Top Left**, Pos X = 20, Pos Y = -10, W = 200, H = 40
4. TextMeshPro : Text = `Round 1`, Font Size = 20, Color = blanc, Alignment = Left + Middle

**4.4.2 — DeckCountText**
1. Clic droit sur HUD > **UI > Text - TextMeshPro**
2. Renomme en `DeckCountText`
3. RectTransform : Ancrage **Top Right**, Pos X = -20, Pos Y = -10, W = 200, H = 40
4. TextMeshPro : Text = `Deck: 108`, Font Size = 20, Color = blanc, Alignment = Right + Middle

**4.4.3 — CurrentPlayerText**
1. Clic droit sur HUD > **UI > Text - TextMeshPro**
2. Renomme en `CurrentPlayerText`
3. RectTransform : Ancrage **Top Center**, Pos X = 0, Pos Y = -10, W = 300, H = 40
4. TextMeshPro : Text = `Player 1`, Font Size = 24, Font Style = Bold, Color = blanc, Alignment = Center + Middle

**4.4.4 — CurrentLevelText**
1. Clic droit sur HUD > **UI > Text - TextMeshPro**
2. Renomme en `CurrentLevelText`
3. RectTransform : Ancrage **Top Center**, Pos X = 0, Pos Y = -50, W = 300, H = 40
4. TextMeshPro : Text = `Niveau 1`, Font Size = 20, Color = #F5C842 (jaune), Alignment = Center + Middle

**4.4.5 — CurrentPhaseText**
1. Clic droit sur HUD > **UI > Text - TextMeshPro**
2. Renomme en `CurrentPhaseText`
3. RectTransform : Ancrage **Top Center**, Pos X = 0, Pos Y = -90, W = 400, H = 40
4. TextMeshPro : Text = `Piochez une carte`, Font Size = 18, Color = #AAAAAA, Alignment = Center + Middle

**4.4.6 — StatusText (avec fade)**
1. Clic droit sur HUD > **Create Empty**, renomme en `StatusGroup`
2. RectTransform : Ancrage **Top Center**, Pos X = 0, Pos Y = -140, W = 500, H = 50
3. Ajoute un composant **CanvasGroup** (Alpha = 0)
4. Clic droit sur StatusGroup > **UI > Text - TextMeshPro**, renomme en `StatusText`
5. RectTransform : Stretch Stretch
6. TextMeshPro : Text = ``, Font Size = 22, Font Style = Bold, Color = #F5C842, Alignment = Center + Middle

#### Connecter HUDView (sur le GameObject HUD) :

| Champ HUDView | Assigner |
|---|---|
| Current Player Text | `CurrentPlayerText` |
| Current Phase Text | `CurrentPhaseText` |
| Current Level Text | `CurrentLevelText` |
| Deck Count Text | `DeckCountText` |
| Round Number Text | `RoundNumberText` |
| Status Text | `StatusGroup/StatusText` |
| Status Canvas Group | `StatusGroup` (composant CanvasGroup) |
| Status Display Duration | 2 |
| Game Over Panel | `GameOverPanel` (on le cree plus tard, etape 4.10) |
| Winner Text | `GameOverPanel/WinnerText` (on le cree plus tard) |

---

### 4.5 Creer la zone LevelProgress (sous le HUD)

1. Clic droit sur Canvas > **Create Empty**, renomme en `LevelProgress`
2. RectTransform :
   - Ancrage : **Top Stretch**
   - Left : 20, Right : 20, Top : 200
   - Height : **180**
3. Ajoute un composant **Vertical Layout Group** :
   - Spacing : 5
   - Child Alignment : Upper Left
   - Child Force Expand Width : true
   - Child Force Expand Height : false
4. Ajoute le script **LevelProgressView**

#### Connecter LevelProgressView :

| Champ | Assigner |
|---|---|
| Container | `LevelProgress` (son propre RectTransform) |
| Step Width | 60 |
| Player Row Height | 40 |
| Completed Color | #45C878 (vert) |
| Current Color | #F5C842 (jaune) |
| Pending Color | #3A3F5C (gris-bleu) |

---

### 4.6 Creer la zone Table (centre de l'ecran)

1. Clic droit sur Canvas > **Create Empty**, renomme en `Table`
2. RectTransform :
   - Ancrage : **Middle Stretch**
   - Left : 20, Right : 20
   - Top : 400, Bottom : 450
3. Ajoute le script **TableView**

4. Clic droit sur Table > **Create Empty**, renomme en `TableContainer`
5. RectTransform : Stretch Stretch (remplit Table)

#### Connecter TableView :

| Champ | Assigner |
|---|---|
| Table Container | `TableContainer` |
| Card Prefab | `Assets/Prefabs/Card` (le prefab) |
| Meld Group Prefab | Laisser vide (sera cree dynamiquement) |
| Meld Spacing | 20 |
| Card In Meld Spacing | 35 |
| Player Zone Spacing | 40 |

---

### 4.7 Creer les zones Deck et Discard

#### 4.7.1 — DeckArea
1. Clic droit sur Canvas > **UI > Image**, renomme en `DeckArea`
2. RectTransform :
   - Ancrage : **Middle Left**
   - Pos X : **100**, Pos Y : **0**
   - Width : **120**, Height : **180**
3. Image : Color = **#2C3150** (bleu sombre, simule le dos d'une carte)
4. Ajoute un enfant **UI > Text - TextMeshPro**, renomme `DeckLabel`
   - Text = `PIOCHE`, Font Size = 16, Color = blanc, Alignment = Center + Middle
   - RectTransform : Stretch Stretch

#### 4.7.2 — DiscardArea
1. Clic droit sur Canvas > **UI > Image**, renomme en `DiscardArea`
2. RectTransform :
   - Ancrage : **Middle Right**
   - Pos X : **-100**, Pos Y : **0**
   - Width : **120**, Height : **180**
3. Image : Color = **#1A1F3C** avec Alpha = **100** (semi-transparent)
4. Ajoute un contour : composant **Outline**, Effect Color = #FFFFFF33, Effect Distance = (2, 2)
5. Ajoute un enfant **UI > Text - TextMeshPro**, renomme `DiscardLabel`
   - Text = `DEFAUSSE`, Font Size = 14, Color = #AAAAAA, Alignment = Center + Middle
   - RectTransform : Stretch Stretch

---

### 4.8 Creer la zone PlayerHand (bas de l'ecran)

1. Clic droit sur Canvas > **Create Empty**, renomme en `PlayerHand`
2. RectTransform :
   - Ancrage : **Bottom Stretch**
   - Left : 0, Right : 0, Bottom : 20
   - Height : **220**
3. Ajoute le script **HandView**

4. Clic droit sur PlayerHand > **Create Empty**, renomme en `HandContainer`
5. RectTransform : Stretch Stretch

#### Connecter HandView :

| Champ | Assigner |
|---|---|
| Hand Container | `HandContainer` |
| Card Prefab | `Assets/Prefabs/Card` (le prefab) |
| Card Spacing | 60 |
| Max Hand Width | 800 |
| Fan Angle | 5 |
| Anim Controller | Le composant AnimationController (cree a l'etape 4.11) |

---

### 4.9 Creer les boutons d'action

1. Clic droit sur Canvas > **Create Empty**, renomme en `ActionButtons`
2. RectTransform :
   - Ancrage : **Middle Center**
   - Pos X : 0, Pos Y : -100
   - Width : 600, Height : 60
3. Ajoute un **Horizontal Layout Group** :
   - Spacing : 20
   - Child Alignment : Middle Center
   - Child Force Expand Width : false

#### 4.9.1 — LayDownButton
1. Clic droit sur ActionButtons > **UI > Button - TextMeshPro**
2. Renomme en `LayDownButton`
3. RectTransform : Width = 200, Height = 50
4. Image : Color = **#45C878** (vert)
5. Enfant TMP : Text = `Poser le niveau`, Font Size = 16, Color = blanc, Font Style = Bold

#### 4.9.2 — SkipButton
1. Clic droit sur ActionButtons > **UI > Button - TextMeshPro**
2. Renomme en `SkipButton`
3. RectTransform : Width = 150, Height = 50
4. Image : Color = **#3A3F5C** (gris)
5. Enfant TMP : Text = `Passer`, Font Size = 16, Color = blanc

#### 4.9.3 — SkipMeldsButton
1. Clic droit sur ActionButtons > **UI > Button - TextMeshPro**
2. Renomme en `SkipMeldsButton`
3. RectTransform : Width = 150, Height = 50
4. Image : Color = **#3A3F5C** (gris)
5. Enfant TMP : Text = `Terminer`, Font Size = 16, Color = blanc

---

### 4.10 Creer les panels plein ecran

#### 4.10.1 — GameOverPanel
1. Clic droit sur Canvas > **UI > Image**, renomme en `GameOverPanel`
2. RectTransform : **Stretch Stretch** (remplit tout le Canvas)
3. Image : Color = **#000000CC** (noir semi-transparent)
4. **Desactive le GameObject** (decoche la case)

5. Clic droit sur GameOverPanel > **UI > Text - TextMeshPro**, renomme en `WinnerText`
6. RectTransform : Ancrage Middle Center, W = 600, H = 100
7. TextMeshPro : Text = `Player 1 gagne !`, Font Size = 36, Font Style = Bold, Color = #F5C842, Alignment = Center + Middle

> Retourne maintenant au composant HUDView (sur le GameObject HUD) et assigne :
> - Game Over Panel = `GameOverPanel`
> - Winner Text = `GameOverPanel/WinnerText`

#### 4.10.2 — PassScreenPanel
1. Clic droit sur Canvas > **UI > Image**, renomme en `PassScreenPanel`
2. RectTransform : **Stretch Stretch**
3. Image : Color = **#1A1F3CFF** (opaque, cache tout)
4. Ajoute un composant **Button** (sur l'image elle-meme, pour detecter le tap)
5. **Desactive le GameObject**

6. Clic droit sur PassScreenPanel > **UI > Text - TextMeshPro**, renomme en `PassScreenText`
7. RectTransform : Ancrage Middle Center, W = 600, H = 200
8. TextMeshPro : Text = `Passez l'appareil`, Font Size = 28, Color = blanc, Alignment = Center + Middle

---

### 4.11 Creer le GameObject GameManager

1. Clic droit dans la Hierarchy > **Create Empty**, renomme en `GameManager`
2. Dans l'Inspector, ajoute ces **5 scripts** :
   - **GameManager** (LevelUp.Core)
   - **GameBootstrapper** (LevelUp.Core)
   - **AnimationController** (LevelUp.UI)
   - **GameInputController** (LevelUp.UI)
   - **NetworkManager** (LevelUp.Network)

#### Connecter GameManager (script) :

| Champ | Assigner |
|---|---|
| Config | `Assets/GameConfig` (le ScriptableObject cree a l'etape 2) |

#### Connecter AnimationController :

| Champ | Assigner |
|---|---|
| Move Curve | Laisser le defaut (EaseInOut) |
| Deck Position | `DeckArea` (RectTransform) |
| Discard Position | `DiscardArea` (RectTransform) |
| Table Center | `Table` (RectTransform) |

#### Connecter GameInputController :

| Champ | Assigner |
|---|---|
| Hand View | `PlayerHand` (composant HandView) |
| Table View | `Table` (composant TableView) |
| HUD View | `HUD` (composant HUDView) |
| Anim Controller | `GameManager` (composant AnimationController, sur ce meme objet) |
| Deck Hit Area | `DeckArea` (RectTransform) |
| Discard Hit Areas | Taille = 1, Element 0 = `DiscardArea` (RectTransform) |
| Table Hit Area | `Table` (RectTransform) |

#### Connecter NetworkManager :

| Champ | Assigner |
|---|---|
| Pass Screen Panel | `PassScreenPanel` (le GameObject) |
| Pass Screen Text | `PassScreenPanel/PassScreenText` (composant TMP) |
| Pass Screen Duration | 1.5 |

#### Connecter GameBootstrapper :

| Champ | Assigner |
|---|---|
| Game Manager | `GameManager` (composant GameManager, sur ce meme objet) |
| Hand View | `PlayerHand` (composant HandView) |
| Table View | `Table` (composant TableView) |
| HUD View | `HUD` (composant HUDView) |
| Level Progress View | `LevelProgress` (composant LevelProgressView) |
| Anim Controller | `GameManager` (composant AnimationController, sur ce meme objet) |
| Input Controller | `GameManager` (composant GameInputController, sur ce meme objet) |
| Network Manager | `GameManager` (composant NetworkManager, sur ce meme objet) |
| Human Player Count | 1 |
| AI Player Count | 3 |

---

## 5. Connecter les boutons

### 5.1 LayDownButton
1. Selectionne `LayDownButton`
2. Dans le composant **Button > On Click ()** :
   - Clique **+**
   - Drag le GameObject `GameManager` dans le champ objet
   - Choisis la fonction : **GameInputController > OnLayDownButton()**

### 5.2 SkipButton
1. Selectionne `SkipButton`
2. On Click :
   - **+** > `GameManager` > **GameInputController > OnSkipLayDownButton()**

### 5.3 SkipMeldsButton
1. Selectionne `SkipMeldsButton`
2. On Click :
   - **+** > `GameManager` > **GameInputController > OnSkipAddToMeldsButton()**

### 5.4 PassScreenPanel (le bouton plein ecran)
1. Selectionne `PassScreenPanel`
2. Dans le composant **Button > On Click ()** :
   - **+** > `GameManager` > **NetworkManager > DismissPassScreen()**

---

## 6. Configurer le Build

### 6.1 Scenes dans le Build
1. **File > Build Settings**
2. Clique **Add Open Scenes** pour ajouter la scene `Game`
3. Assure-toi que `Game` est en index 0

### 6.2 Player Settings
1. **Edit > Project Settings > Player**
   - Company Name : `Indie`
   - Product Name : `LevelUp`
2. Onglet **iOS** et **Android** :
   - Default Orientation : **Auto Rotation**
   - Coche Portrait, Portrait Upside Down, Landscape Right, Landscape Left
3. Onglet **PC** :
   - Fullscreen Mode : **Windowed**
   - Default Screen Width : **1080**
   - Default Screen Height : **1920**

---

## 7. Tester

1. Sauvegarde la scene (**Ctrl+S**)
2. Clique **Play** dans l'editeur
3. Tu devrais voir :
   - Le fond navy (#1A1F3C)
   - Les textes du HUD (Round 1, Player 1, Niveau 1)
   - Les cartes apparaitre dans la main en bas
   - Le deck a gauche
   - Les boutons d'action au centre
4. Clique sur le deck pour piocher
5. L'IA joue automatiquement quand c'est son tour

---

## Resume de la Hierarchy finale

```
Game (scene)
|
+-- Main Camera
|
+-- Canvas (Screen Space Overlay, Scale With Screen Size 1080x1920)
|   |
|   +-- HUD [HUDView]
|   |   +-- RoundNumberText (TMP)
|   |   +-- DeckCountText (TMP)
|   |   +-- CurrentPlayerText (TMP)
|   |   +-- CurrentLevelText (TMP)
|   |   +-- CurrentPhaseText (TMP)
|   |   +-- StatusGroup [CanvasGroup]
|   |       +-- StatusText (TMP)
|   |
|   +-- LevelProgress [LevelProgressView, VerticalLayoutGroup]
|   |
|   +-- Table [TableView]
|   |   +-- TableContainer
|   |
|   +-- DeckArea (Image #2C3150)
|   |   +-- DeckLabel (TMP "PIOCHE")
|   |
|   +-- DiscardArea (Image transparent)
|   |   +-- DiscardLabel (TMP "DEFAUSSE")
|   |
|   +-- PlayerHand [HandView]
|   |   +-- HandContainer
|   |
|   +-- ActionButtons [HorizontalLayoutGroup]
|   |   +-- LayDownButton (Button)
|   |   +-- SkipButton (Button)
|   |   +-- SkipMeldsButton (Button)
|   |
|   +-- GameOverPanel (Image noir, DESACTIVE) [X]
|   |   +-- WinnerText (TMP)
|   |
|   +-- PassScreenPanel (Image navy, DESACTIVE) [X]
|       +-- PassScreenText (TMP)
|
+-- GameManager [GameManager, GameBootstrapper, AnimationController,
                  GameInputController, NetworkManager]
```

---

## Champs a connecter — Checklist rapide

- [ ] GameManager.Config -> GameConfig asset
- [ ] GameBootstrapper -> tous les 10 champs
- [ ] AnimationController -> MoveCurve, DeckPosition, DiscardPosition, TableCenter
- [ ] GameInputController -> HandView, TableView, HUDView, AnimController, DeckHitArea, DiscardHitAreas, TableHitArea
- [ ] NetworkManager -> PassScreenPanel, PassScreenText
- [ ] HUDView -> 10 champs (textes + panels)
- [ ] LevelProgressView -> Container
- [ ] TableView -> TableContainer, CardPrefab
- [ ] HandView -> HandContainer, CardPrefab, CardSpacing=65, MaxHandWidth=900, FanAngle=4, AnimController
- [ ] CardView (dans le prefab) -> CardFront, CardBack, Background, Border, ColorBand, ShadowImage, ValueText, CornerTL, CornerBR, CanvasGroup
- [ ] LayDownButton.OnClick -> GameInputController.OnLayDownButton
- [ ] SkipButton.OnClick -> GameInputController.OnSkipLayDownButton
- [ ] SkipMeldsButton.OnClick -> GameInputController.OnSkipAddToMeldsButton
- [ ] PassScreenPanel.OnClick -> NetworkManager.DismissPassScreen
