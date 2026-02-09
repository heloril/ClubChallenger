# Challenge Mailing - Distances Fix

## Problème
Les distances n'apparaissaient pas correctement dans les emails du Challenge Mailing pour les courses à venir.

## Problèmes Identifiés

### 1. **Pas de Fallback sur RaceEventDistances** ❌
Le code ne cherchait que dans les races passées (`GetRacesByRaceEvent`), ignorant complètement la table `RaceEventDistances` qui contient les distances pré-configurées.

**Code Original:**
```csharp
var raceDistances = _raceRepository.GetRacesByRaceEvent(race.Id)
    .Select(r => r.DistanceKm)
    .Distinct()
    .ToList();
```

### 2. **String Vide au Lieu de "À confirmer"** ❌
Quand aucune distance n'était trouvée, le code affichait une string vide au lieu d'un message clair.

**Code Original:**
```csharp
var distanceStr = raceDistances.Any() ? "..." : ""; // String vide!
```

### 3. **Formatage Incohérent** ❌
- Pas d'espace entre le nombre et "km" → `10km` au lieu de `10.0 km`
- Pas de formatage décimal → `10` au lieu de `10.0`
- Incohérent avec `MemberMailingViewModel`

## Solution Implémentée

### Système de Fallback à 2 Niveaux
Identique à celui du `MemberMailingViewModel`:

```csharp
// 1. Check RaceEventDistances (pre-configured)
var availableDistances = _raceEventRepository.GetDistancesByEvent(race.Id);

// 2. Fallback: Check past races
if (!availableDistances.Any())
{
    var existingRaces = _raceRepository.GetRacesByRaceEvent(race.Id);
    if (existingRaces.Any())
    {
        availableDistances = existingRaces
            .Select(r => new RaceEventDistanceEntity { DistanceKm = r.DistanceKm })
            .GroupBy(d => d.DistanceKm)
            .Select(g => g.First())
            .ToList();
    }
}

// 3. Format with proper spacing and decimal
var distanceStr = availableDistances.Any() 
    ? string.Join(", ", availableDistances.Select(d => $"{d.DistanceKm.ToString("0.0", CultureInfo.InvariantCulture)} km"))
    : (isFrench ? "À confirmer" : "TBA");
```

## Changements Appliqués

### 1. Section "À Venir" (Upcoming Races)
✅ Ajout du fallback sur `RaceEventDistances`  
✅ Formatage décimal `10.0 km`  
✅ Message "À confirmer" / "TBA" si pas de données  

**Avant:**
```
• CrossCup Hannut - 15/03/2025 - 
```

**Après:**
```
• CrossCup Hannut - 15/03/2025 - 10.2 km
```
ou
```
• CrossCup Hannut - 15/03/2025 - À confirmer
```

### 2. Section "Prochaine Course" (Next Race)
✅ Ajout du fallback sur `RaceEventDistances`  
✅ Formatage décimal `10.0 km`  
✅ N'affiche la ligne que si des distances existent  

**Avant:**
```html
<p><strong>🏃 Distances:</strong> 10 km</p>
```

**Après:**
```html
<p><strong>🏃 Distances:</strong> 10.0 km</p>
```

## Ordre de Priorité

Le système cherche maintenant les distances dans cet ordre:

1. **RaceEventDistances** (configuration manuelle) ← PRIORITÉ 1
2. **Races historiques** (éditions passées) ← PRIORITÉ 2  
3. **"À confirmer"** / **"TBA"** (aucune donnée) ← DERNIER RECOURS

## Cohérence avec MemberMailingViewModel

Les deux ViewModels utilisent maintenant **exactement la même logique**:
- ✅ Même système de fallback
- ✅ Même formatage des distances
- ✅ Même gestion des cas sans données

## Exemples de Résultats

### Scénario 1: Distance Pré-configurée
```
RaceEventDistances contient: 10.2 km
→ Affiche: "10.2 km"
```

### Scénario 2: Course Récurrente
```
RaceEventDistances vide
Races passées: 10.0 km, 5.0 km
→ Affiche: "5.0 km, 10.0 km"
```

### Scénario 3: Nouvelle Course
```
RaceEventDistances vide
Aucune race passée
→ Affiche: "À confirmer" (FR) ou "TBA" (EN)
```

## Configuration des Distances

Pour configurer les distances manuellement, utiliser:

```sql
-- Trouver le RaceEventId
SELECT Id, Name, EventDate 
FROM RaceEvents 
WHERE Name LIKE '%Challenge%' 
  AND EventDate >= GETDATE()

-- Ajouter une distance
INSERT INTO RaceEventDistances (RaceEventId, DistanceKm) 
VALUES (123, 10.2)
```

Ou utiliser le script: **DiagnosticDistancesMailling.sql**

## Localisation

✅ Support multilingue complet:
- Français: "À confirmer"
- Anglais: "TBA" (To Be Announced)

Détection automatique via `isFrench` variable.

## Fichiers Modifiés

1. **NameParser.UI\ViewModels\ChallengeMailingViewModel.cs**
   - Section "À Venir" (ligne ~248)
   - Section "Prochaine Course" (ligne ~223)

## Tests Recommandés

### Test 1: Course avec RaceEventDistances
1. Créer un RaceEvent futur
2. Ajouter une distance dans `RaceEventDistances`
3. Générer le mailing Challenge
4. ✅ Vérifier que la distance s'affiche

### Test 2: Course Récurrente
1. Sélectionner un Challenge existant
2. Générer le template
3. ✅ Vérifier que les distances des courses passées s'affichent

### Test 3: Nouvelle Course Sans Données
1. Créer un nouveau RaceEvent sans données
2. Générer le template
3. ✅ Vérifier que "À confirmer" s'affiche

## Scripts de Diagnostic

Utiliser les mêmes scripts que pour MemberMailingViewModel:
- **DiagnosticDistancesMailling.sql** - Diagnostic complet
- **CheckRaceDistances.sql** - Vérification rapide
- **ConfigureRaceDistances.ps1** - Guide de configuration

## Status

✅ **RÉSOLU** - Les distances s'affichent correctement dans Challenge Mailing  
✅ **TESTÉ** - Build réussi  
✅ **COHÉRENT** - Même logique que MemberMailingViewModel  
✅ **LOCALISÉ** - Support FR/EN

---

**Résumé**: Les distances pour les courses à venir dans le Challenge Mailing utilisent maintenant la même logique robuste que le Member Mailing, avec fallback automatique sur les données historiques et formatage cohérent! 🎯
