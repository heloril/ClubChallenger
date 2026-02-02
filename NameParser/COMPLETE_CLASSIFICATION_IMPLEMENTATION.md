# Classement Complet avec Team, Speed et Flag Membre

## Vue d'ensemble
Le système extrait maintenant un **classement complet** depuis les fichiers Excel avec tous les champs disponibles et marque les membres du club.

---

## Nouveaux Champs Ajoutés

### 1. **Team (Équipe)** 
- **Type**: String (200 caractères)
- **Source Excel**: Colonnes "équipe", "equipe", "team", "club"
- **Affichage UI**: Colonne "Team"

### 2. **Speed (Vitesse)**
- **Type**: Double (nullable)
- **Source Excel**: Colonnes "vitesse", "vit", "vit.", "speed", "km/h"
- **Format**: km/h
- **Affichage UI**: "Speed (km/h)" avec 2 décimales

### 3. **IsMember (Flag Membre)**
- **Type**: Boolean
- **Valeur**: `true` = Membre du club, `false` = Participant externe
- **Affichage UI**: Case à cocher "Member"

---

## Mapping des Colonnes Excel

### Format Excel 1:
```
Place | Dos | NOM | Équipe | Catégorie | Pl cat | Temps | T/km | Vitesse
```

### Format Excel 2:
```
Pl. | Dos | NOM | Équipe | Catégorie | Pl cat | Temps | Temps km | Vit.
```

### Mapping Automatique:

| Propriété | Colonnes Excel Supportées |
|-----------|--------------------------|
| **Position** | Place, Pl, Pl., Position, Pos, Rang, Classement |
| **RaceTime** | Temps, Time, Chrono |
| **TimePerKm** | T/km, Temps/km, Temps km, Time/km, Pace |
| **Speed** | Vitesse, Vit, Vit., Speed, Km/h |
| **Team** | Équipe, Equipe, Team, Club |

---

## Calcul des Points (Nouveau)

### Formule: Pourcentage du Temps du 1er

```
Points = (Temps_du_1er / Temps_du_membre) × 1000
```

### Exemples:

| Position | Temps | Calcul | Points |
|----------|-------|--------|--------|
| 1er | 45:00 | (45:00 / 45:00) × 1000 | **1000** |
| 2ème | 47:15 | (45:00 / 47:15) × 1000 | **952** |
| 10ème | 52:30 | (45:00 / 52:30) × 1000 | **857** |

**Avantages:**
- Le 1er obtient toujours 1000 points
- Plus on est lent, moins on a de points
- Permet de comparer entre courses différentes
- Échelle plus lisible (1000 au lieu de 100)

---

## Structure des Données

### ClassificationEntity (Base de données)

```csharp
public class ClassificationEntity
{
    public int Id { get; set; }                    // Rank
    public int? Position { get; set; }             // Position dans la course
    public string MemberFirstName { get; set; }    // Prénom
    public string MemberLastName { get; set; }     // Nom
    public string Team { get; set; }               // ⭐ Équipe
    public int Points { get; set; }                // Points calculés
    public TimeSpan? RaceTime { get; set; }        // Temps de course
    public TimeSpan? TimePerKm { get; set; }       // Temps par km
    public double? Speed { get; set; }             // ⭐ Vitesse (km/h)
    public bool IsMember { get; set; }             // ⭐ Flag membre
    public int BonusKm { get; set; }               // Kilomètres bonus
}
```

---

## Affichage UI

### DataGrid Classifications (Colonnes):

```
┌──────┬──────────┬────────────┬───────────┬─────────────┬────────┬───────────┬──────────┬─────────────┬────────┬──────────┐
│ Rank │ Position │ First Name │ Last Name │    Team     │ Points │ Race Time │ Time/km  │ Speed (km/h)│ Member │ Bonus KM │
├──────┼──────────┼────────────┼───────────┼─────────────┼────────┼───────────┼──────────┼─────────────┼────────┼──────────┤
│  100 │    1     │ Eliud      │ Kipchoge  │ Nike Team   │  1000  │ 42:15     │    -     │    16.85    │   ☐    │   10     │
│  15  │    2     │ John       │ Doe       │ Club Athlé  │   952  │ 45:23     │    -     │    15.67    │   ☑    │   10     │
│  16  │    3     │ Jane       │ Smith     │ Club Athlé  │   920  │ 47:45     │    -     │    14.92    │   ☑    │   10     │
└──────┴──────────┴────────────┴───────────┴─────────────┴────────┴───────────┴──────────┴─────────────┴────────┴──────────┘
                                                                                                              ↑
                                                                                            Case à cocher pour identifier les membres
```

---

## Fonctionnalités

### 1. Extraction Automatique des Champs

Le système scanne automatiquement l'en-tête Excel pour trouver:
- Position (Place, Pl., etc.)
- Team (Équipe, Team, etc.)
- Speed (Vitesse, Vit., etc.)
- Race Time (Temps, Time, etc.)
- Time/km (T/km, Temps km, etc.)

### 2. Marquage des Membres

**Membres du club** (`TMEM`):
- Trouvés dans Members.json
- Flag `IsMember = true`
- Case cochée ✓ dans l'UI

**Participants externes** (`TWINNER`):
- PAS dans Members.json (ex: gagnant élite)
- Flag `IsMember = false`
- Case décochée ☐ dans l'UI

### 3. Calcul Points Pourcentage

```csharp
// Points = (temps du 1er / temps du membre) × 1000
double points = (referenceTime.TotalSeconds / memberTime.TotalSeconds) * 1000;

// Exemple:
// 1er: 45:00 → (2700s / 2700s) × 1000 = 1000 points
// 2ème: 47:15 → (2700s / 2835s) × 1000 = 952 points
```

---

## Flux de Données

```
Excel File
    ↓
1. Scan header pour trouver colonnes:
   - Position → col 1
   - Team → col 4
   - Speed → col 9
   - Temps → col 7
    ↓
2. Pour chaque ligne:
   - Lire Position, Team, Speed, Temps
   - Vérifier si membre (dans Members.json)
   - Marquer: TMEM (membre) ou TWINNER (externe)
    ↓
3. Extraire données:
   - Position: "1" → 1
   - Team: "Club Athlé" → "Club Athlé"
   - Speed: "15.67 km/h" → 15.67
   - Temps: "45:23" → TimeSpan
    ↓
4. Calculer points:
   - Temps 1er: 42:15 (référence)
   - Temps membre: 45:23
   - Points = (42:15 / 45:23) × 1000 = 930
    ↓
5. Sauvegarder dans DB avec tous les champs
    ↓
6. Afficher dans UI avec flag membre visible
```

---

## Exemples de Résultats

### Exemple 1: Course avec Gagnant Externe

**Excel:**
```
Pl. | Nom      | Prénom | Équipe       | Temps | Vit.  
 1  | Kipchoge | Eliud  | Nike Team    | 42:15 | 16.85
 2  | Doe      | John   | Club Athlé   | 45:23 | 15.67
 3  | Smith    | Jane   | Club Athlé   | 47:45 | 14.92
```

**Base de Données:**
```sql
Position | Name          | Team        | Points | Speed | IsMember
---------|---------------|-------------|--------|-------|----------
   1     | Eliud Kipchoge| Nike Team   |  1000  | 16.85 | false   ← Externe
   2     | John Doe      | Club Athlé  |   930  | 15.67 | true    ← Membre
   3     | Jane Smith    | Club Athlé  |   884  | 14.92 | true    ← Membre
```

### Exemple 2: Course Time/km

**Excel:**
```
Place | Nom   | Équipe      | T/km | Vit.  
  1   | Brown | Running Pro | 4:15 | 14.12
  2   | Doe   | Club Athlé  | 4:28 | 13.45
```

**Base de Données:**
```sql
Position | Name       | Team        | TimePerKm | Speed | IsMember
---------|------------|-------------|-----------|-------|----------
   1     | Brown      | Running Pro |   4:15    | 14.12 | false
   2     | John Doe   | Club Athlé  |   4:28    | 13.45 | true
```

---

## Migration Base de Données

### Fichier: `AddTeamSpeedMemberColumns.sql`

Ajoute 3 colonnes à la table Classifications:

```sql
ALTER TABLE Classifications
ADD Team NVARCHAR(200) NULL;

ALTER TABLE Classifications
ADD Speed FLOAT NULL;

ALTER TABLE Classifications
ADD IsMember BIT NOT NULL DEFAULT 1;
```

### Appliquer la Migration:

**Option 1: SSMS**
```sql
-- Exécuter dans SQL Server Management Studio
USE YourDatabase;
GO
-- Copier/coller le contenu de AddTeamSpeedMemberColumns.sql
```

**Option 2: Command Line**
```bash
sqlcmd -S your_server -d your_database -i Infrastructure\Data\Migrations\AddTeamSpeedMemberColumns.sql
```

---

## Configuration

### Ajouter de Nouveaux Noms de Colonnes

Si vos fichiers Excel utilisent d'autres noms:

```csharp
// Dans ExcelRaceResultRepository.FindColumnIndex()

// Pour Team:
int teamColumnIndex = FindColumnIndex(ws, new[] { 
    "equipe", "équipe", "team", "club",
    "YOUR_NEW_NAME_HERE"  ← Ajouter ici
});

// Pour Speed:
int speedColumnIndex = FindColumnIndex(ws, new[] { 
    "vitesse", "vit", "vit.", "speed", "km/h",
    "YOUR_NEW_NAME_HERE"  ← Ajouter ici
});
```

---

## Avantages

✅ **Classement Complet**: Tous les champs Excel disponibles
✅ **Identification Membres**: Flag visible pour distinguer membres/externes
✅ **Équipe Visible**: Affiche l'équipe de chaque participant
✅ **Vitesse Affichée**: Montre la vitesse en km/h
✅ **Points Pourcentage**: Calcul basé sur le temps du 1er (plus intuitif)
✅ **Flexible**: Supporte différents formats Excel
✅ **Automatique**: Détection automatique des colonnes

---

## Points Clés

### Calcul Points (1000 points pour le 1er)
- **Avant**: `(temps_ref / temps_membre) × 100` → 100 points max
- **Maintenant**: `(temps_ref / temps_membre) × 1000` → 1000 points max
- **Avantage**: Échelle plus précise et lisible

### Flag Membre
- **True**: Dans Members.json → Membre du club
- **False**: Pas dans Members.json → Externe (gagnant élite, etc.)
- **UI**: Case à cocher pour visualisation rapide

### Extraction Team/Speed
- **Automatique**: Scan de l'en-tête Excel
- **Flexible**: Supporte EN/FR
- **Robuste**: Continue si colonnes absentes

---

## Testing

### Checklist:

- [ ] Migration SQL appliquée
- [ ] Excel avec colonne "Équipe" → Team extrait
- [ ] Excel avec colonne "Vitesse" → Speed extrait
- [ ] Membre du club → IsMember = true, case cochée
- [ ] Gagnant externe → IsMember = false, case décochée
- [ ] Points calculés: 1er = 1000, autres < 1000
- [ ] UI affiche toutes les colonnes correctement

---

## Troubleshooting

### Team toujours vide
**Cause**: Colonne pas trouvée dans l'en-tête
**Solution**: Vérifier le nom exact dans Excel, ajouter à `FindColumnIndex`

### Speed toujours null
**Cause**: Format non reconnu (ex: "16.85km/h" sans espace)
**Solution**: Le système enlève "km/h", vérifier le format dans Excel

### IsMember toujours false
**Cause**: Membre pas trouvé dans Members.json
**Solution**: Vérifier orthographe exacte du nom dans Members.json

### Points incorrects
**Cause**: Temps de référence incorrect
**Solution**: Vérifier que le 1er a bien 1000 points

---

## Résumé

Le système offre maintenant un **classement complet** avec:

1. ✅ **Position** de course
2. ✅ **Team** (équipe) du participant
3. ✅ **Speed** (vitesse en km/h)
4. ✅ **Points** calculés en pourcentage du 1er (×1000)
5. ✅ **Flag IsMember** pour identifier les membres
6. ✅ **Race Time** et **Time/km**
7. ✅ **Support gagnant externe** même si pas membre

Tout est extrait automatiquement depuis Excel avec une interface claire pour distinguer membres et participants externes.
