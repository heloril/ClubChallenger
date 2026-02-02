# Corrections: Time/km, Speed et Calcul Points

## Résumé des Corrections

Trois corrections importantes ont été apportées au système:

1. ✅ **Format Time per kilometer**: Toujours en `mm:ss`
2. ✅ **Speed parsing**: Supporte `15.15` ET `15,15`
3. ✅ **Calcul points**: `(temps_coureur / temps_premier) × 1000`

---

## 1. Format Time per Kilometer

### Problème
Le TimePerKm n'était pas toujours rempli depuis la colonne Excel spécifique.

### Solution
```csharp
// Extraction des deux colonnes séparément
TimeSpan? raceTime = null;           // De la colonne "Temps"
TimeSpan? timePerKmFromColumn = null; // De la colonne "T/km"

// Extraire TOUJOURS les deux si les colonnes existent
if (raceTimeColumnIndex > 0)
{
    raceTime = ParseTime(ws.Cells[row, raceTimeColumnIndex].Text);
}

if (timePerKmColumnIndex > 0)
{
    timePerKmFromColumn = ParseTime(ws.Cells[row, timePerKmColumnIndex].Text);
}
```

### Affichage UI
Le converter affiche automatiquement:
- **< 1 heure**: `mm:ss` (ex: `04:35`)
- **≥ 1 heure**: `h:mm:ss` (ex: `1:04:35`)

---

## 2. Speed Parsing

### Problème
Excel peut contenir `15.15` (point) ou `15,15` (virgule) selon la locale.

### Solution
```csharp
// Normaliser en remplaçant virgule par point
speedText = speedText.Replace("km/h", "").Replace(",", ".").Trim();

// Parser avec InvariantCulture (utilise le point)
if (double.TryParse(speedText, NumberStyles.Any, CultureInfo.InvariantCulture, out double spd))
{
    speed = spd;
}
```

### Formats Supportés
- `15.15` ✓
- `15,15` ✓
- `15.15 km/h` ✓
- `15,15km/h` ✓

---

## 3. Calcul des Points

### Ancien Calcul ❌
```csharp
// Ancien: (temps_premier / temps_coureur) × 1000
points = (referenceTime.TotalSeconds / memberTime.TotalSeconds) * 1000;

// Résultat:
// 1er: (2535 / 2535) × 1000 = 1000 points
// 2ème: (2535 / 2723) × 1000 = 931 points  ← Plus lent = moins de points
```

### Nouveau Calcul ✅
```csharp
// Nouveau: (temps_coureur / temps_premier) × 1000
points = (memberTime.TotalSeconds / referenceTime.TotalSeconds) * 1000;

// Résultat:
// 1er: (2535 / 2535) × 1000 = 1000 points
// 2ème: (2723 / 2535) × 1000 = 1074 points  ← Plus lent = plus de points
```

### Exemples de Calcul

#### Course 10km

| Position | Temps | Calcul | Points |
|----------|-------|--------|--------|
| 1er | 42:15 (2535s) | (2535 / 2535) × 1000 | **1000** |
| 2ème | 45:23 (2723s) | (2723 / 2535) × 1000 | **1074** |
| 3ème | 47:45 (2865s) | (2865 / 2535) × 1000 | **1130** |
| 10ème | 52:30 (3150s) | (3150 / 2535) × 1000 | **1243** |

**Note**: Le 1er a 1000 points, les autres ont PLUS de points. Plus le temps est long, plus les points augmentent.

---

## Extraction Améliorée

### Avant
- ❌ TimePerKm extrait seulement si "time per km race"
- ❌ RaceTime extrait seulement si "race time race"

### Après
- ✅ **TOUJOURS** extraire les deux colonnes si elles existent
- ✅ Marquer avec `RACETIME;hh:mm:ss;` et `TIMEPERKM;mm:ss;`
- ✅ Utiliser les valeurs extraites en priorité

### Code ExcelRaceResultRepository

```csharp
// Toujours extraire les deux
if (raceTimeColumnIndex > 0)
{
    var timeText = ws.Cells[row, raceTimeColumnIndex].Text;
    raceTime = ParseTime(timeText);
}

if (timePerKmColumnIndex > 0)
{
    var timeText = ws.Cells[row, timePerKmColumnIndex].Text;
    timePerKmFromColumn = ParseTime(timeText);
}

// Ajouter aux données
if (raceTime.HasValue)
{
    rowData.Append($"RACETIME;{raceTime.Value:hh\\:mm\\:ss};");
}

if (timePerKmFromColumn.HasValue)
{
    rowData.Append($"TIMEPERKM;{timePerKmFromColumn.Value:mm\\:ss};");
}
```

### Code RaceProcessingService

```csharp
// Extraire les temps spécifiques
TimeSpan? extractedRaceTime = null;
TimeSpan? extractedTimePerKm = null;

for (int i = 0; i < individualResult.Length - 1; i++)
{
    if (individualResult[i].Equals("RACETIME", StringComparison.OrdinalIgnoreCase))
    {
        if (TryParseTime(individualResult[i + 1], out TimeSpan rt))
        {
            extractedRaceTime = rt;
        }
    }

    if (individualResult[i].Equals("TIMEPERKM", StringComparison.OrdinalIgnoreCase))
    {
        if (TryParseTime(individualResult[i + 1], out TimeSpan tpk))
        {
            extractedTimePerKm = tpk;
        }
    }
}

// Utiliser les valeurs extraites
TimeSpan? finalRaceTime = extractedRaceTime ?? (isTimePerKmRace ? null : memberTime);
TimeSpan? finalTimePerKm = extractedTimePerKm ?? timePerKm;

classification.AddOrUpdateResult(member, race, points, finalRaceTime, finalTimePerKm, ...);
```

---

## Format de Données

### Données Transmises (Result String)

```
TMEM;1;Doe;John;Club Athlé;45:23;4:32;RACETYPE;RACE_TIME;RACETIME;00:45:23;TIMEPERKM;04:32;POS;2;TEAM;Club Athlé;SPEED;15.67;ISMEMBER;1;
     ↑                                                               ↑               ↑            ↑
   Marker                                                      Temps course    Temps/km     Position
```

### Parsing dans RaceProcessingService

```
RACETIME;00:45:23;  → extractedRaceTime = 45:23
TIMEPERKM;04:32;    → extractedTimePerKm = 4:32
SPEED;15.67;        → speed = 15.67
```

---

## Résultats Attendus

### Excel Input:
```
Pl. | Nom      | Temps | T/km | Vitesse
 1  | Kipchoge | 42:15 | 4:14 | 16,85
 2  | Doe      | 45:23 | 4:32 | 15,67
 3  | Smith    | 47:45 | 4:46 | 14.92
```

### Database Output:
```sql
Pos | Name          | RaceTime | TimePerKm | Speed | Points
----|---------------|----------|-----------|-------|--------
 1  | Eliud Kipchoge| 42:15    | 4:14      | 16.85 | 1000
 2  | John Doe      | 45:23    | 4:32      | 15.67 | 1074
 3  | Jane Smith    | 47:45    | 4:46      | 14.92 | 1130
```

### UI Display:
```
┌──────┬────────────┬───────────┬───────────┬──────────┬─────────────┬────────┐
│ Pos  │ First Name │ Last Name │ Race Time │ Time/km  │ Speed (km/h)│ Points │
├──────┼────────────┼───────────┼───────────┼──────────┼─────────────┼────────┤
│  1   │ Eliud      │ Kipchoge  │ 42:15     │   4:14   │    16.85    │  1000  │
│  2   │ John       │ Doe       │ 45:23     │   4:32   │    15.67    │  1074  │
│  3   │ Jane       │ Smith     │ 47:45     │   4:46   │    14.92    │  1130  │
└──────┴────────────┴───────────┴───────────┴──────────┴─────────────┴────────┘
```

---

## Points Clés

### ✅ TimePerKm
- Toujours extrait de la colonne Excel si elle existe
- Format d'affichage: `mm:ss` (ex: `04:32`)
- Stocké comme `TimeSpan` en base de données

### ✅ Speed
- Parse `15.15` et `15,15` (virgule → point)
- Retire "km/h" automatiquement
- Stocké comme `double` en base de données

### ✅ Points
- Formule: `(temps_coureur / temps_premier) × 1000`
- 1er = 1000 points
- Plus lent = plus de points
- Permet de voir l'écart en pour-mille

---

## Fichiers Modifiés

1. ✅ **ExcelRaceResultRepository.cs**
   - Extraction RaceTime et TimePerKm séparément
   - Ajout des marqueurs RACETIME et TIMEPERKM

2. ✅ **RaceProcessingService.cs**
   - Extraction des marqueurs RACETIME et TIMEPERKM
   - Inversion du calcul de points
   - Utilisation des temps extraits en priorité

3. ✅ **TimeSpanToStringConverter.cs**
   - Déjà correct: affiche `mm:ss` pour < 1h

---

## Testing

### Vérifier:

1. **TimePerKm rempli**
   ```sql
   SELECT MemberFirstName, TimePerKm 
   FROM Classifications 
   WHERE TimePerKm IS NOT NULL;
   ```

2. **Speed avec virgule**
   - Excel: `15,67`
   - DB: `15.67` ✓

3. **Points corrects**
   ```sql
   SELECT Position, MemberFirstName, Points
   FROM Classifications
   ORDER BY Position;
   
   -- 1er doit avoir 1000 points
   -- 2ème doit avoir > 1000 points
   ```

---

## Build Status

✅ **Tous les builds réussis - Aucune erreur**

---

*Corrections appliquées avec succès. Le système extrait maintenant correctement tous les temps et calcule les points selon la formule demandée.*
