# ✅ Classement par Course et Classement Général

## Résumé

Le système propose maintenant **deux vues de classement**:
1. 🏁 **Classement par Course** - Une course spécifique
2. 📊 **Classement Général** - Toutes les courses de l'année

---

## Nouvelle Interface

### Boutons de Bascule
```
┌───────────────────────────────────────────────┐
│ 📊 General Classification  🏁 Race Class...   │
│ Year: [2024 ▼]                                │
└───────────────────────────────────────────────┘
```

---

## 🏁 Classement par Course

### Colonnes
- Position, Name, Team, Points
- Race Time, Time/km, Speed
- Member flag (☑/☐)
- Bonus KM

### Usage
1. Sélectionner une course
2. Cliquer "👁️ View Classification"
3. Voir les résultats de cette course

### Affichage
```
Pos | Name          | Team       | Points | Race Time | Member
 1  | Eliud Kipchoge| Nike Team  |  1000  | 42:15     |  ☐
 2  | John Doe      | Club Athlé |  1074  | 45:23     |  ☑
 3  | Jane Smith    | Club Athlé |  1130  | 47:45     |  ☑
```

---

## 📊 Classement Général

### Colonnes
- Rank, Name, Team
- **Total Points** - Somme de tous les points
- **Races** - Nombre de courses
- **Avg Points** - Moyenne par course
- **Best Pos** - Meilleure position
- **Best Time** - Meilleur temps
- **Total Bonus KM** - Somme bonus km

### Usage
1. Cliquer "📊 General Classification"
2. Sélectionner année
3. Voir le classement complet

### Affichage
```
Rank | Name       | Team       | Total Points | Races | Avg | Best Pos
  1  | John Doe   | Club Athlé |    10750     |  10   | 1075|    2
  2  | Jane Smith | Club Athlé |    10200     |   9   | 1133|    3
  3  | Bob Johnson| Running Pro|     9500     |   8   | 1188|    4
```

---

## Calcul Classement Général

### Agrégation
```
Pour chaque membre:
  - Total Points = SOMME(points de toutes ses courses)
  - Total Bonus KM = SOMME(bonus km de toutes ses courses)
  - Races = NOMBRE de courses participées
  - Avg Points = MOYENNE(points par course)
  - Best Position = MIN(position obtenue)
  - Best Time = MIN(temps de course)
```

### Tri
1. **Total Points** (décroissant)
2. **Total Bonus KM** (décroissant) - en cas d'égalité

### Filtrage
**Seulement les membres** (`IsMember = true`)
- Les participants externes ne sont PAS dans le classement général

---

## Fonctionnalités

### ✅ Bascule Facile
Cliquer les boutons pour switcher entre les vues

### ✅ Sélection Année
ComboBox pour choisir l'année (mode général uniquement)

### ✅ Deux DataGrids
- Un pour classement course (caché en mode général)
- Un pour classement général (caché en mode course)

### ✅ Visibilité Automatique
Les grilles apparaissent/disparaissent selon le mode

---

## Fichiers Créés/Modifiés

### 1. GeneralClassificationDto.cs ⭐
DTO pour le classement général avec agrégations

### 2. ClassificationRepository.cs ⭐
Méthode `GetGeneralClassification(year)` avec GroupBy

### 3. MainViewModel.cs ⭐
- Propriété `ShowGeneralClassification`
- Propriété `SelectedYear`
- Collection `GeneralClassifications`
- Commandes `ViewGeneralClassificationCommand`, `ShowRaceClassificationCommand`
- Méthode `LoadGeneralClassification()`

### 4. MainWindow.xaml ⭐
- Boutons de bascule
- DataGrid classement course
- DataGrid classement général
- Visibilité conditionnelle

### 5. InverseBoolToVisibilityConverter.cs ⭐
Converter pour inverser bool → visibility

### 6. App.xaml ⭐
Enregistrement des converters en ressources globales

---

## Workflow

### Consulter une Course
```
Onglet "View Results"
  ↓
Sélectionner course
  ↓
"👁️ View Classification"
  ↓
Voir résultats de la course
```

### Consulter Classement Général
```
Onglet "View Results"
  ↓
"📊 General Classification"
  ↓
Sélectionner année
  ↓
Voir classement complet
```

---

## Exemples

### Membre avec 10 Courses
```
Total Points: 10750 (somme)
Races: 10
Avg Points: 1075
Best Position: 2 (meilleure place)
Best Time: 42:15 (meilleur chrono)
Total Bonus KM: 100 (10 courses × 10 km)
```

### Comparaison
- **John**: 10750 pts / 10 courses = 1075 moy
- **Jane**: 10200 pts / 9 courses = 1133 moy ← Meilleure moyenne!

---

## Points Clés

✅ **Classement Course**: Tous participants (membres + externes)
✅ **Classement Général**: Seulement membres du club
✅ **Agrégation**: Somme points, moyenne, meilleurs résultats
✅ **Bascule**: Facile entre les deux vues
✅ **Multi-Années**: Sélectionner année en mode général

---

## Build Status

✅ **Tous les builds réussis - Aucune erreur**

---

## Documentation

📄 **GENERAL_CLASSIFICATION_IMPLEMENTATION.md** - Guide détaillé

---

## Testing

- [ ] Traiter plusieurs courses
- [ ] Vérifier classement par course
- [ ] Basculer vers classement général
- [ ] Vérifier agrégation correcte
- [ ] Vérifier tri par total points
- [ ] Changer d'année
- [ ] Basculer retour vers classement course

---

*Implémentation complète! Le système offre maintenant une vue par course ET une vue générale pour suivre les performances sur toute l'année.*
