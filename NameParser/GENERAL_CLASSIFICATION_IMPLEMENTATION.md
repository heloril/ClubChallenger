# Classement par Course et Classement Général

## Vue d'ensemble

Le système permet maintenant de basculer entre deux types de classement:
1. **Classement par Course** - Résultats d'une course individuelle
2. **Classement Général** - Classement complet sur toute l'année

---

## Fonctionnalités

### 1. Classement par Course 🏁

**Affiche**: Les résultats d'une course spécifique

**Colonnes**:
- Rank - ID base de données
- Position - Position dans la course
- First Name - Prénom
- Last Name - Nom
- Team - Équipe
- Points - Points de la course
- Race Time - Temps de course
- Time/km - Temps par km
- Speed (km/h) - Vitesse
- Member - Flag membre (☑/☐)
- Bonus KM - Kilomètres bonus

**Usage**:
1. Sélectionner une course dans la liste
2. Cliquer sur "👁️ View Classification"
3. Les résultats s'affichent en bas

---

### 2. Classement Général 📊

**Affiche**: Agrégation de toutes les courses de l'année

**Colonnes**:
- Rank - Classement général
- First Name - Prénom
- Last Name - Nom
- Team - Équipe
- Total Points - Somme de tous les points
- Races - Nombre de courses participées
- Avg Points - Moyenne des points par course
- Best Pos - Meilleure position obtenue
- Best Time - Meilleur temps de course
- Best T/km - Meilleur temps/km
- Total Bonus KM - Somme des bonus km

**Usage**:
1. Cliquer sur "📊 General Classification"
2. Sélectionner l'année dans le ComboBox
3. Le classement général s'affiche

---

## Interface Utilisateur

### Boutons de Bascule

```
┌──────────────────────────────────────────────────┐
│ 📊 General Classification  🏁 Race Classification│
│ Year: [2024 ▼]                                   │
└──────────────────────────────────────────────────┘
```

- **📊 General Classification** - Affiche le classement général
- **🏁 Race Classification** - Revient au classement par course
- **Year** - Sélecteur d'année (visible uniquement en mode général)

---

## Calcul du Classement Général

### Logique d'Agrégation

```csharp
var generalClassification = context.Classifications
    .Where(c => c.Race.Year == year && c.IsMember) // Seulement les membres
    .GroupBy(c => new { 
        c.MemberFirstName, 
        c.MemberLastName, 
        c.MemberEmail, 
        c.Team 
    })
    .Select(g => new GeneralClassificationDto
    {
        MemberFirstName = g.Key.MemberFirstName,
        MemberLastName = g.Key.MemberLastName,
        TotalPoints = g.Sum(c => c.Points),           // Somme
        TotalBonusKm = g.Sum(c => c.BonusKm),         // Somme
        RaceCount = g.Count(),                         // Nombre
        AveragePoints = (int)g.Average(c => c.Points), // Moyenne
        BestPosition = g.Min(c => c.Position),         // Min
        BestRaceTime = g.Min(c => c.RaceTime),         // Min
        BestTimePerKm = g.Min(c => c.TimePerKm)        // Min
    })
    .OrderByDescending(c => c.TotalPoints)
    .ThenByDescending(c => c.TotalBonusKm)
    .ToList();
```

### Critères de Tri

1. **Total Points** (décroissant) - Plus de points = meilleur
2. **Total Bonus KM** (décroissant) - En cas d'égalité

---

## Exemples

### Classement par Course

**Course**: 10km Geer (10 octobre 2024)

```
┌──────┬──────────┬────────┬─────────┬────────────┬────────┬───────────┬──────────┐
│ Rank │ Position │ First  │ Last    │ Team       │ Points │ Race Time │ Member   │
├──────┼──────────┼────────┼─────────┼────────────┼────────┼───────────┼──────────┤
│  100 │    1     │ Eliud  │Kipchoge │ Nike Team  │  1000  │ 42:15     │    ☐     │
│   15 │    2     │ John   │ Doe     │ Club Athlé │  1074  │ 45:23     │    ☑     │
│   16 │    3     │ Jane   │ Smith   │ Club Athlé │  1130  │ 47:45     │    ☑     │
└──────┴──────────┴────────┴─────────┴────────────┴────────┴───────────┴──────────┘
```

### Classement Général (Année 2024)

```
┌──────┬────────┬─────────┬────────────┬──────────────┬───────┬───────────┬──────────┬───────────────┐
│ Rank │ First  │ Last    │ Team       │ Total Points │ Races │ Avg Points│ Best Pos │ Total Bonus KM│
├──────┼────────┼─────────┼────────────┼──────────────┼───────┼───────────┼──────────┼───────────────┤
│   1  │ John   │ Doe     │ Club Athlé │    10750     │  10   │   1075    │    2     │     100       │
│   2  │ Jane   │ Smith   │ Club Athlé │    10200     │   9   │   1133    │    3     │      90       │
│   3  │ Bob    │ Johnson │Running Pro │     9500     │   8   │   1188    │    4     │      80       │
└──────┴────────┴─────────┴────────────┴──────────────┴───────┼───────────┼──────────┼───────────────┘
```

**Interprétation**:
- **John Doe** est 1er avec 10750 points sur 10 courses
- **Jane Smith** est 2ème avec 10200 points sur 9 courses (moyenne supérieure!)
- **Best Pos** montre la meilleure position obtenue dans l'année

---

## Workflow Utilisateur

### Consulter une Course Spécifique

```
1. Onglet "View Results"
   ↓
2. Sélectionner une course dans la liste
   ↓
3. Cliquer "👁️ View Classification"
   ↓
4. Voir les résultats de cette course
   ↓
5. Éventuellement télécharger avec "💾 Download Results"
```

### Consulter le Classement Général

```
1. Onglet "View Results"
   ↓
2. Cliquer "📊 General Classification"
   ↓
3. Sélectionner l'année (ex: 2024)
   ↓
4. Voir le classement général de l'année
   ↓
5. Comparer les performances des membres
```

### Basculer entre les Vues

```
Mode Course → Cliquer "📊 General Classification" → Mode Général
Mode Général → Cliquer "🏁 Race Classification" → Mode Course
```

---

## Architecture

### Fichiers Créés/Modifiés

#### 1. **GeneralClassificationDto.cs** ⭐
```csharp
public class GeneralClassificationDto
{
    public int Rank { get; set; }
    public string MemberFirstName { get; set; }
    public string MemberLastName { get; set; }
    public string Team { get; set; }
    public int TotalPoints { get; set; }
    public int TotalBonusKm { get; set; }
    public int RaceCount { get; set; }
    public int AveragePoints { get; set; }
    public int? BestPosition { get; set; }
    public TimeSpan? BestRaceTime { get; set; }
    public TimeSpan? BestTimePerKm { get; set; }
}
```

#### 2. **ClassificationRepository.cs** ⭐
```csharp
public List<GeneralClassificationDto> GetGeneralClassification(int year)
{
    // Agrège les résultats par membre
    // Calcule sommes, moyennes, minimums
    // Trie par total points
    // Retourne le classement général
}
```

#### 3. **MainViewModel.cs** ⭐
```csharp
public bool ShowGeneralClassification { get; set; }
public int SelectedYear { get; set; }
public ObservableCollection<GeneralClassificationDto> GeneralClassifications { get; }

public ICommand ViewGeneralClassificationCommand { get; }
public ICommand ShowRaceClassificationCommand { get; }

private void LoadGeneralClassification() { ... }
```

#### 4. **MainWindow.xaml** ⭐
- Boutons de bascule
- DataGrid pour classement par course
- DataGrid pour classement général
- Visibilité basée sur `ShowGeneralClassification`

#### 5. **InverseBoolToVisibilityConverter.cs** ⭐
```csharp
// true → Collapsed
// false → Visible
```

---

## Détails Techniques

### Filtrage des Membres

**Classement Général**: Seulement les **membres** (`IsMember = true`)
```csharp
.Where(c => c.Race.Year == year && c.IsMember)
```

**Raison**: Les participants externes (gagnants élite) ne font pas partie du club et ne doivent pas apparaître dans le classement général.

### Gestion de la Visibilité

```xaml
<!-- Affiche si ShowGeneralClassification = false -->
<DataGrid Visibility="{Binding ShowGeneralClassification, 
                               Converter={StaticResource InverseBoolToVisibilityConverter}}">

<!-- Affiche si ShowGeneralClassification = true -->
<DataGrid Visibility="{Binding ShowGeneralClassification, 
                               Converter={StaticResource BoolToVisibilityConverter}}">
```

### Sélection Année

Le ComboBox année est visible uniquement en mode général:
```xaml
<ComboBox Visibility="{Binding ShowGeneralClassification, 
                               Converter={StaticResource BoolToVisibilityConverter}}"/>
```

---

## Points Clés

### ✅ Classement par Course
- Affiche **une course** spécifique
- Inclut **tous les participants** (membres + externes)
- Montre la **position**, **temps**, **vitesse**

### ✅ Classement Général
- Agrège **toutes les courses** de l'année
- Inclut **seulement les membres** du club
- Montre **total points**, **nombre courses**, **meilleures performances**

### ✅ Bascule Facile
- Deux boutons pour switcher
- Sélection année pour classement général
- Vues s'excluent mutuellement

---

## Avantages

✅ **Vue Complète**: Voir performance globale sur l'année
✅ **Comparaison**: Comparer facilement les membres
✅ **Motivation**: Suivre sa progression au fil des courses
✅ **Statistiques**: Moyenne, meilleur temps, meilleure position
✅ **Flexibilité**: Basculer rapidement entre les vues
✅ **Filtrage Année**: Consulter les années précédentes

---

## Cas d'Usage

### 1. Membre Régulier
*"Je veux voir mon classement sur l'année"*
→ Cliquer "📊 General Classification", chercher son nom

### 2. Organisateur
*"Je veux publier le classement général de l'année"*
→ Classement général → Export (à développer)

### 3. Analyse Performance
*"Je veux comparer mes résultats entre courses"*
→ Voir "Avg Points", "Best Pos", "Best Time"

### 4. Résultats Course Spécifique
*"Je veux voir qui a gagné la course de Geer"*
→ Sélectionner course → View Classification

---

## Build Status

✅ **Tous les builds réussis - Aucune erreur**

---

## Testing

### Test 1: Classement par Course
1. Traiter une course
2. Cliquer "View Classification"
3. ✓ Vérifier que tous les participants apparaissent
4. ✓ Vérifier position, temps, flag membre

### Test 2: Classement Général
1. Traiter plusieurs courses
2. Cliquer "📊 General Classification"
3. ✓ Vérifier agrégation correcte (Total Points = somme)
4. ✓ Vérifier tri par Total Points
5. ✓ Vérifier que seulement les membres apparaissent

### Test 3: Bascule
1. En mode course → Cliquer "📊 General Classification"
2. ✓ Vérifier switch vers classement général
3. Cliquer "🏁 Race Classification"
4. ✓ Vérifier retour au mode course

---

*Implémentation complète du classement par course et du classement général avec bascule facile entre les deux vues.*
