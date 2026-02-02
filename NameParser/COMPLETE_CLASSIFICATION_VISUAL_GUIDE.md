# Guide Visuel: Classement Complet

## Vue d'Ensemble

Le système extrait maintenant **tous les champs** disponibles dans Excel avec identification automatique des membres.

---

## Avant vs Après

### AVANT ❌
```
┌──────┬──────────┬────────────┬───────────┬────────┐
│ Rank │ Position │ First Name │ Last Name │ Points │
├──────┼──────────┼────────────┼───────────┼────────┤
│  15  │    1     │ John       │ Doe       │  100   │
│  16  │    2     │ Jane       │ Smith     │   95   │
└──────┴──────────┴────────────┴───────────┴────────┘

Manque:
❌ Pas d'équipe visible
❌ Pas de vitesse
❌ Impossible de voir qui est membre
❌ Gagnant externe non inclus
```

### APRÈS ✅
```
┌──────┬──────────┬────────────┬───────────┬─────────────┬────────┬───────────┬─────────────┬────────┐
│ Rank │ Position │ First Name │ Last Name │    Team     │ Points │ Race Time │ Speed (km/h)│ Member │
├──────┼──────────┼────────────┼───────────┼─────────────┼────────┼───────────┼─────────────┼────────┤
│  100 │    1     │ Eliud      │ Kipchoge  │ Nike Team   │  1000  │ 42:15     │    16.85    │   ☐    │ ← Externe
│  15  │    2     │ John       │ Doe       │ Club Athlé  │   952  │ 45:23     │    15.67    │   ☑    │ ← Membre
│  16  │    3     │ Jane       │ Smith     │ Club Athlé  │   920  │ 47:45     │    14.92    │   ☑    │ ← Membre
│  17  │    4     │ Bob        │ Johnson   │ Running Pro │   885  │ 50:12     │    14.18    │   ☐    │ ← Externe
└──────┴──────────┴────────────┴───────────┴─────────────┴────────┴───────────┴─────────────┴────────┘

Avantages:
✅ Équipe visible
✅ Vitesse en km/h
✅ Flag membre (case à cocher)
✅ Gagnant externe inclus
✅ Classement complet
```

---

## Format Excel Supportés

### Format 1: Course Standard
```
Excel:
┌───────┬─────┬──────────┬─────────────┬───────────┬────────┬────────┬────────┬─────────┐
│ Place │ Dos │   NOM    │   Équipe    │ Catégorie │ Pl cat │ Temps  │  T/km  │ Vitesse │
├───────┼─────┼──────────┼─────────────┼───────────┼────────┼────────┼────────┼─────────┤
│   1   │ 125 │ Kipchoge │  Nike Team  │   Elite   │   1    │ 42:15  │  4:14  │  16.85  │
│   2   │  42 │   Doe    │ Club Athlé  │  Senior   │   1    │ 45:23  │  4:32  │  15.67  │
│   3   │  67 │  Smith   │ Club Athlé  │  Senior   │   2    │ 47:45  │  4:46  │  14.92  │
└───────┴─────┴──────────┴─────────────┴───────────┴────────┴────────┴────────┴─────────┘
         ↓      ↓           ↓                                  ↓                 ↓
     Position  Nom       Équipe                             Temps             Vitesse
```

### Format 2: Course Alternative
```
Excel:
┌──────┬─────┬──────────┬─────────────┬───────────┬────────┬────────┬──────────┬───────┐
│ Pl.  │ Dos │   NOM    │   Équipe    │ Catégorie │ Pl cat │ Temps  │ Temps km │ Vit.  │
├──────┼─────┼──────────┼─────────────┼───────────┼────────┼────────┼──────────┼───────┤
│  1   │ 125 │ Kipchoge │  Nike Team  │   Elite   │   1    │ 42:15  │   4:14   │ 16.85 │
│  2   │  42 │   Doe    │ Club Athlé  │  Senior   │   1    │ 45:23  │   4:32   │ 15.67 │
└──────┴─────┴──────────┴─────────────┴───────────┴────────┴────────┴──────────┴───────┘
   ↓                                                                              ↓
Position                                                                      Vitesse
```

---

## Extraction des Données

### Étape 1: Scan de l'En-tête

```
Le système scanne la ligne 1 pour trouver:

┌───────────────┬─────────────────────────────────────┐
│     Champ     │   Colonnes Excel Reconnues          │
├───────────────┼─────────────────────────────────────┤
│ Position      │ place, pl, pl., position, pos, rang │
│ Team          │ équipe, equipe, team, club          │
│ Speed         │ vitesse, vit, vit., speed, km/h     │
│ RaceTime      │ temps, time, chrono                 │
│ TimePerKm     │ t/km, temps/km, temps km, pace      │
└───────────────┴─────────────────────────────────────┘

Résultat:
positionColumnIndex = 1  (colonne "Place")
teamColumnIndex = 4      (colonne "Équipe")
speedColumnIndex = 9     (colonne "Vitesse")
```

### Étape 2: Extraction des Valeurs

```
Pour chaque ligne (participant):

Row 2: │ 1 │ 125 │ Kipchoge │ Nike Team │ Elite │ 1 │ 42:15 │ 4:14 │ 16.85 │
         ↓                      ↓                              ↓        ↓
    Position: 1              Team: Nike Team                Temps   Speed: 16.85
    
    ↓ Vérifier dans Members.json
    
    "Kipchoge" NOT FOUND → IsMember = false
    
    ↓ Marquer comme
    
    TWINNER (gagnant externe)
```

### Étape 3: Calcul des Points

```
Temps du 1er (référence): 42:15 = 2535 secondes

Pour chaque participant:
┌──────────────┬────────┬─────────────────────────────────────────┬────────┐
│ Participant  │ Temps  │          Calcul                         │ Points │
├──────────────┼────────┼─────────────────────────────────────────┼────────┤
│ Kipchoge (1) │ 42:15  │ (2535 / 2535) × 1000 = 1.000 × 1000    │  1000  │ ← 100%
│ Doe (2)      │ 45:23  │ (2535 / 2723) × 1000 = 0.931 × 1000    │   931  │ ← 93.1%
│ Smith (3)    │ 47:45  │ (2535 / 2865) × 1000 = 0.885 × 1000    │   885  │ ← 88.5%
│ Johnson (4)  │ 50:12  │ (2535 / 3012) × 1000 = 0.842 × 1000    │   842  │ ← 84.2%
└──────────────┴────────┴─────────────────────────────────────────┴────────┘

Le 1er obtient TOUJOURS 1000 points!
Plus on est lent, moins on a de points.
```

---

## Marquage des Membres

### Processus de Vérification

```
Pour chaque participant:

1. Nom extrait de Excel: "Doe"
   ↓
2. Chercher dans Members.json:
   {
     "firstName": "John",
     "lastName": "Doe",
     "email": "john.doe@club.com"
   }
   ↓
3. TROUVÉ ✓
   ↓
4. Marquer: TMEM (membre)
   IsMember = true
   ↓
5. UI: Case cochée ☑
```

```
Pour le gagnant externe:

1. Nom extrait de Excel: "Kipchoge"
   ↓
2. Chercher dans Members.json:
   NOT FOUND ✗
   ↓
3. Marquer: TWINNER (externe)
   IsMember = false
   ↓
4. UI: Case décochée ☐
```

---

## Interface Utilisateur

### Légende des Colonnes

```
┌────────────────┬──────────────────────────────────────────────┐
│    Colonne     │              Description                     │
├────────────────┼──────────────────────────────────────────────┤
│ Rank           │ ID base de données (technique)              │
│ Position       │ Position dans la course (1, 2, 3...)       │
│ First Name     │ Prénom du participant                       │
│ Last Name      │ Nom du participant                          │
│ Team           │ Équipe/Club (ex: "Club Athlé")            │
│ Points         │ Points calculés (1er = 1000)               │
│ Race Time      │ Temps de course (hh:mm:ss)                 │
│ Time/km        │ Temps par km (mm:ss)                       │
│ Speed (km/h)   │ Vitesse en kilomètres/heure (16.85)       │
│ Member         │ ☑ = Membre, ☐ = Externe                   │
│ Bonus KM       │ Kilomètres bonus accumulés                 │
└────────────────┴──────────────────────────────────────────────┘
```

### Exemples Visuels

#### Membre du Club ✓
```
┌──────┬──────────┬────────┬───────┬────────────┬────────┬───────────┬──────────┬─────────┬────────┐
│  15  │    2     │ John   │ Doe   │ Club Athlé │  931   │ 45:23     │    -     │  15.67  │   ☑    │
└──────┴──────────┴────────┴───────┴────────────┴────────┴───────────┴──────────┴─────────┴────────┘
                                                                                               ↑
                                                                                    Case cochée = Membre
```

#### Participant Externe ✗
```
┌──────┬──────────┬───────┬──────────┬───────────┬────────┬───────────┬──────────┬─────────┬────────┐
│ 100  │    1     │ Eliud │ Kipchoge │ Nike Team │  1000  │ 42:15     │    -     │  16.85  │   ☐    │
└──────┴──────────┴───────┴──────────┴───────────┴────────┴───────────┴──────────┴─────────┴────────┘
                                                                                               ↑
                                                                                    Case vide = Externe
```

---

## Flux de Traitement Complet

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│                              FICHIER EXCEL                                      │
│  Place│Dos│NOM     │Équipe     │Cat│Pl cat│Temps │T/km│Vitesse                │
│   1   │125│Kipchoge│Nike Team  │E  │  1   │42:15 │4:14│ 16.85                 │
│   2   │ 42│Doe     │Club Athlé │S  │  1   │45:23 │4:32│ 15.67                 │
└─────────────────────────────────────────────────────────────────────────────────┘
                                    ↓
┌─────────────────────────────────────────────────────────────────────────────────┐
│                        SCAN EN-TÊTE ET EXTRACTION                               │
│  Position col: 1 | Team col: 4 | Speed col: 9 | Temps col: 7                  │
└─────────────────────────────────────────────────────────────────────────────────┘
                                    ↓
┌─────────────────────────────────────────────────────────────────────────────────┐
│                         POUR CHAQUE PARTICIPANT                                 │
│                                                                                 │
│  Kipchoge: Position=1, Team="Nike Team", Speed=16.85, Temps=42:15            │
│    ↓                                                                            │
│  Chercher "Kipchoge" dans Members.json → NOT FOUND                            │
│    ↓                                                                            │
│  Marquer: TWINNER, IsMember=false                                             │
│    ↓                                                                            │
│  Calculer: Points = (42:15 / 42:15) × 1000 = 1000                            │
│                                                                                 │
│  ─────────────────────────────────────────────────────────────────────────    │
│                                                                                 │
│  Doe: Position=2, Team="Club Athlé", Speed=15.67, Temps=45:23                │
│    ↓                                                                            │
│  Chercher "Doe" dans Members.json → FOUND ✓                                   │
│    ↓                                                                            │
│  Marquer: TMEM, IsMember=true                                                 │
│    ↓                                                                            │
│  Calculer: Points = (42:15 / 45:23) × 1000 = 931                             │
└─────────────────────────────────────────────────────────────────────────────────┘
                                    ↓
┌─────────────────────────────────────────────────────────────────────────────────┐
│                        SAUVEGARDER EN BASE DE DONNÉES                           │
│                                                                                 │
│  Classification {                                                               │
│    Position: 1,                                                                 │
│    Name: "Eliud Kipchoge",                                                     │
│    Team: "Nike Team",                                                          │
│    Points: 1000,                                                                │
│    RaceTime: 42:15,                                                            │
│    Speed: 16.85,                                                               │
│    IsMember: false                                                             │
│  }                                                                              │
└─────────────────────────────────────────────────────────────────────────────────┘
                                    ↓
┌─────────────────────────────────────────────────────────────────────────────────┐
│                             AFFICHER DANS L'UI                                  │
│                                                                                 │
│  Rank│Pos│First │Last    │Team      │Points│RaceTime│Speed │Member│            │
│  100 │ 1 │Eliud │Kipchoge│Nike Team │ 1000 │ 42:15  │16.85 │  ☐   │ ← Externe  │
│   15 │ 2 │John  │Doe     │Club Athlé│  931 │ 45:23  │15.67 │  ☑   │ ← Membre   │
└─────────────────────────────────────────────────────────────────────────────────┘
```

---

## Exemples de Courses

### Course 1: 10km avec Gagnant Élite

**Excel:**
```
Pl.│Nom      │Équipe       │Temps │Vit.  
 1 │Kipchoge │Nike Team    │42:15 │16.85
 2 │Doe      │Club Athlé   │45:23 │15.67
 3 │Smith    │Club Athlé   │47:45 │14.92
 4 │Johnson  │Running Pro  │50:12 │14.18
```

**UI:**
```
Pos│Name          │Team        │Points│Speed │Member
 1 │Eliud Kipchoge│Nike Team   │ 1000 │16.85 │  ☐    ← Externe (élite)
 2 │John Doe      │Club Athlé  │  931 │15.67 │  ☑    ← Membre
 3 │Jane Smith    │Club Athlé  │  885 │14.92 │  ☑    ← Membre
 4 │Bob Johnson   │Running Pro │  842 │14.18 │  ☐    ← Externe
```

### Course 2: Time Trial (Time/km)

**Excel:**
```
Place│Nom   │Équipe      │T/km │Vit.  
  1  │Brown │Running Pro │4:15 │14.12
  2  │Doe   │Club Athlé  │4:28 │13.45
  3  │Smith │Club Athlé  │4:42 │12.77
```

**UI:**
```
Pos│Name       │Team        │Points│Time/km│Speed │Member
 1 │Brown      │Running Pro │ 1000 │ 4:15  │14.12 │  ☐
 2 │John Doe   │Club Athlé  │  952 │ 4:28  │13.45 │  ☑
 3 │Jane Smith │Club Athlé  │  904 │ 4:42  │12.77 │  ☑
```

---

## Points Clés à Retenir

### ✅ Ce qui a changé:

1. **Team (Équipe)** maintenant visible
2. **Speed (Vitesse)** affichée en km/h
3. **IsMember** flag avec case à cocher
4. **Points sur 1000** (au lieu de 100)
5. **Gagnant externe** toujours inclus

### ✅ Avantages:

- **Complet**: Tous les champs Excel extraits
- **Clair**: Flag membre visible immédiatement
- **Précis**: Calcul points basé sur temps du 1er
- **Flexible**: Supporte différents formats Excel
- **Automatique**: Détection colonnes automatique

---

*Guide visuel complet pour le système de classement avec tous les champs Excel*
