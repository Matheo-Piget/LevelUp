# Fiche Play Store — LevelUp

Tout ce qu'il faut copier dans la Play Console. Les images sont dans `store/fr` et `store/en`.

## Réglages de la fiche

| Champ | Valeur |
|---|---|
| Nom de l'application | LevelUp – Jeu de cartes (FR) / LevelUp – Card Game (EN) |
| Langue par défaut | Français (France), ajouter Anglais (États-Unis) |
| Type | Jeu, gratuit |
| Catégorie | Cartes |
| Adresse e-mail de contact | matheo.piget5@gmail.com |
| Politique de confidentialité | https://matheo-piget.github.io/LevelUp/privacy.html |
| Contient des annonces | Non |
| Achats intégrés | Non |

## Images

| Élément | Fichier |
|---|---|
| Icône 512 × 512 | `store/icone-512.png` |
| Image de présentation 1024 × 500 | `store/fr/presentation-1024x500.png`, `store/en/presentation-1024x500.png` |
| Captures téléphone | `store/fr/capture-*.png`, `store/en/capture-*.png` (4 paysage, 2 portrait) |

Pour les refaire après un changement visuel : captures du build PC avec
`-autoshot <dossier> -supersize 2`, puis `tools/store/captures.ps1` et `tools/store/presentation.ps1`.

## Français

**Description courte (80 caractères max)**

Jeu de cartes à contrats : 8 niveaux, bots malins, en ligne entre amis.

**Description complète**

LevelUp est un jeu de cartes à contrats : à chaque niveau, une combinaison à poser. Le premier joueur qui franchit les 8 niveaux gagne.

À ton tour : pioche, pose ton contrat si ta main le permet, complète les combinaisons de la table, puis défausse. Suites, séries, couleurs : les contrats se corsent de niveau en niveau.

POURQUOI Y JOUER
• Des parties rapides, pensées pour le téléphone : glisse une carte, elle se place toute seule.
• Des bots qui jouent juste, sans tricher : trois niveaux de difficulté, chacun son style.
• En ligne entre amis : crée un salon, partage le code, jouez à quatre.
• Passe-et-joue : plusieurs joueurs sur le même appareil.
• Donne du jour : la même donne pour tout le monde, une fois par jour.
• Mode Défi : une main, quelques pioches connues d'avance, un contrat à poser.
• Tutoriel jouable pour apprendre en trois minutes.

À TA FAÇON
• Règles au choix : jokers et cartes action, contrats variés, 4, 6 ou 8 niveaux, minuteur.
• Aides désactivables : c'est toi qui décides de ce que le jeu te souffle.
• Portrait ou paysage, français ou anglais.
• Symboles sur chaque couleur pour les joueurs daltoniens.
• Tapis et dos de cartes à débloquer en gagnant.

SANS PUBLICITÉ, SANS ACHAT, SANS COMPTE
Le jeu fonctionne hors ligne. Tes parties et tes statistiques restent sur ton appareil.

## English

**Short description (80 characters max)**

Contract card game: 8 levels, smart bots, online play with friends.

**Full description**

LevelUp is a contract card game: each level asks for a specific combination. The first player to clear all 8 levels wins.

On your turn: draw, lay down your contract if your hand allows it, add cards to the melds on the table, then discard. Runs, sets, flushes: contracts get harder level after level.

WHY PLAY
• Quick games built for phones: drag a card and it snaps into place.
• Bots that play fair, with no cheating: three difficulty levels, each with its own style.
• Online with friends: create a room, share the code, play with up to four.
• Pass-and-play: several players on one device.
• Daily deal: the same deal for everyone, once a day.
• Challenge mode: one hand, a few draws known in advance, one contract to lay down.
• Playable tutorial to learn in three minutes.

YOUR WAY
• Pick your rules: jokers and action cards, varied contracts, 4, 6 or 8 levels, turn timer.
• Optional hints: you decide how much the game tells you.
• Portrait or landscape, English or French.
• Symbols on every color for colorblind players.
• Felts and card backs to unlock by winning.

NO ADS, NO PURCHASES, NO ACCOUNT
The game works offline. Your games and statistics stay on your device.

## Formulaire « Sécurité des données »

À confirmer avec le guide d'Unity sur ce formulaire (Unity publie, pour chacun de ses services, ce qu'il
faut déclarer) : le jeu utilise Unity Authentication et Unity Relay, uniquement pour le jeu en ligne.

| Question | Réponse proposée |
|---|---|
| L'application collecte-t-elle ou partage-t-elle des données ? | Oui (à cause du jeu en ligne) |
| Données chiffrées en transit | Oui |
| Suppression des données sur demande | Par e-mail (aucun compte, identifiant anonyme) |
| Type de données | Identifiants de l'appareil ou autres identifiants |
| Collectées ou partagées | Collectées, non partagées |
| Traitement éphémère | Non (identifiant conservé par Unity) |
| Obligatoire ou facultatif | Facultatif (seulement en jouant en ligne) |
| Finalité | Fonctionnement de l'application |

## Classification du contenu

Questionnaire IARC : jeu de cartes, pas de violence, pas de contenu sexuel, pas de jeux d'argent (aucune
mise, aucun achat), pas de langage grossier. Les joueurs en ligne n'échangent que des réactions
prédéfinies et un pseudo : répondre « oui » à l'interaction entre utilisateurs, « non » au contenu libre
partagé (le pseudo est le seul texte libre).

## Public cible

13 ans et plus (évite les obligations de la politique « Familles »).

## Notes de version (1.1)

FR : Première version publique : solo contre des bots, en ligne entre amis, donne du jour, mode Défi,
tutoriel jouable, portrait et paysage, français et anglais.

EN : First public release: solo against bots, online with friends, daily deal, Challenge mode, playable
tutorial, portrait and landscape, English and French.
