# Guide de Résolution - Distances Manquantes dans le Mailing

## Problème
Les distances ne s'affichent pas dans le calendrier des courses à venir ("À confirmer" s'affiche toujours).

## Causes Possibles

### 1. Courses à Venir Sans Données (CAUSE PRINCIPALE)
Pour les **courses à venir**, il n'existe pas encore de données dans:
- ❌ Table `RaceEventDistances` (pas configuré manuellement)
- ❌ Table `Races` (pas de courses passées avec ce RaceEventId)

**Résultat**: Le système n'a aucune information de distance → Affiche "À confirmer"

### 2. RaceEventId Non Associé
Les courses passées existent mais ne sont **pas associées** au bon `RaceEventId`.

### 3. Nouvelle Course Jamais Courue
C'est une nouvelle course qui n'a jamais eu lieu auparavant.

## Solution Étape par Étape

### ÉTAPE 1: Diagnostic
Exécutez le script SQL de diagnostic:

```sql
-- Dans Visual Studio, ouvrir SQL Server Object Explorer
-- Ou utiliser SQL Server Management Studio
-- Exécuter: DiagnosticDistancesMailling.sql
```

Ce script va vous montrer:
- ✅ Quelles courses ont des distances configurées
- ✅ Quelles courses ont des éditions passées disponibles
- ❌ Quelles courses n'ont AUCUNE donnée de distance

### ÉTAPE 2: Solution Automatique (Recommandée)
Si des courses passées existent, copiez automatiquement leurs distances:

```sql
-- Cette requête copie les distances des courses passées vers RaceEventDistances
INSERT INTO RaceEventDistances (RaceEventId, DistanceKm)
SELECT DISTINCT 
    r.RaceEventId,
    r.DistanceKm
FROM Races r
INNER JOIN RaceEvents re ON r.RaceEventId = re.Id
WHERE r.RaceEventId IS NOT NULL
  AND re.EventDate >= GETDATE()
  AND NOT EXISTS (
      SELECT 1 
      FROM RaceEventDistances red 
      WHERE red.RaceEventId = r.RaceEventId 
        AND red.DistanceKm = r.DistanceKm
  )
```

### ÉTAPE 3: Configuration Manuelle (Si Nécessaire)
Pour les nouvelles courses sans historique:

#### Option A: Via SQL
```sql
-- Remplacer 123 par le RaceEventId réel
-- Trouver l'ID avec: SELECT Id, Name, EventDate FROM RaceEvents WHERE EventDate >= GETDATE()

-- Exemple: CrossCup / CJPL (généralement 10.2 km)
INSERT INTO RaceEventDistances (RaceEventId, DistanceKm) VALUES (123, 10.2)

-- Exemple: Course avec plusieurs distances
INSERT INTO RaceEventDistances (RaceEventId, DistanceKm) VALUES 
  (123, 5.0),
  (123, 10.0)
```

#### Option B: Via l'Application
1. Ouvrir l'application
2. Aller dans la gestion des Race Events
3. Sélectionner la course
4. Ajouter les distances dans l'interface

### ÉTAPE 4: Test avec Logs de Diagnostic
L'application affiche maintenant des logs détaillés dans la fenêtre de debug:

```
[MAILING] Race Event: 'CrossCup Hannut' (ID: 45)
[MAILING]   Pre-configured distances: 0
[MAILING]   No pre-configured distances, checking past races...
[MAILING]   Found 3 past races
[MAILING]   Using 1 distance(s) from past races: 10.2
[MAILING]   Final distance string: '10.2 km'
```

**Pour voir ces logs**:
1. Lancer l'application en mode Debug (F5)
2. Aller dans Member Mailing
3. Cliquer sur "Generate Template"
4. Regarder la fenêtre "Output" dans Visual Studio (View → Output)
5. Sélectionner "Debug" dans le dropdown

### ÉTAPE 5: Vérification
1. Générer un template de mailing
2. Vérifier la section "Calendrier de la Semaine"
3. Les distances doivent maintenant s'afficher

## Distances Courantes pour Référence

| Type de Course | Distances Typiques |
|----------------|-------------------|
| CrossCup / CJPL | 10.0 km, 10.2 km |
| Challenge Lucien Campeggio | 5.0 km, 10.0 km (variable) |
| Jogging de l'An Neuf | 5.0 km, 10.0 km |
| Semi-Marathon | 21.1 km |
| Marathon | 42.2 km |

## Exemple Complet: Configuration d'une Nouvelle Course

### Scénario
Vous avez une nouvelle course "CrossCup Waremme" le 15/03/2025.

### Solution
```sql
-- 1. Trouver le RaceEventId
SELECT Id, Name, EventDate 
FROM RaceEvents 
WHERE Name LIKE '%Waremme%' 
  AND EventDate >= GETDATE()
-- Résultat: Id = 156

-- 2. Ajouter la distance standard CrossCup
INSERT INTO RaceEventDistances (RaceEventId, DistanceKm) 
VALUES (156, 10.2)

-- 3. Vérifier
SELECT re.Name, red.DistanceKm
FROM RaceEvents re
LEFT JOIN RaceEventDistances red ON re.Id = red.RaceEventId
WHERE re.Id = 156
-- Doit afficher: Waremme | 10.2
```

### Test
1. Sélectionner la date 15/03/2025 dans le mailing
2. Generate Template
3. Vérifier le calendrier → Doit afficher "10.2 km" ✅

## Ordre de Priorité du Système

Le système cherche les distances dans cet ordre:

1. **RaceEventDistances** (configuration manuelle) ← PRIORITE 1
2. **Races historiques** (éditions passées) ← PRIORITE 2
3. **"À confirmer"** (aucune donnée) ← DERNIER RECOURS

## Cas Particuliers

### Cas 1: Course Récurrente avec Nom Différent
Si une course change de nom mais c'est le même événement:
- Associer les anciennes races avec le nouveau RaceEventId
```sql
UPDATE Races 
SET RaceEventId = 156  -- Nouveau RaceEventId
WHERE Id IN (123, 124, 125)  -- IDs des anciennes courses
```

### Cas 2: Course Hors Challenge
Les courses "Hors Challenge" (`IsHorsChallenge = true` ou `Year = NULL`) peuvent ne pas avoir de `RaceEventId`.
- Solution: Créer un RaceEvent et associer les courses

### Cas 3: Plusieurs Distances Variables
Certaines courses ont des distances qui changent chaque année:
- Configurer toutes les distances possibles dans `RaceEventDistances`
- Le système affichera toutes les distances disponibles

## Script de Vérification Rapide

```sql
-- Combien de courses à venir ont des distances?
SELECT 
    COUNT(*) AS [Total Courses A Venir],
    SUM(CASE WHEN EXISTS (
        SELECT 1 FROM RaceEventDistances WHERE RaceEventId = re.Id
    ) THEN 1 ELSE 0 END) AS [Avec Distances Configurées],
    SUM(CASE WHEN EXISTS (
        SELECT 1 FROM Races WHERE RaceEventId = re.Id
    ) THEN 1 ELSE 0 END) AS [Avec Courses Passées],
    SUM(CASE 
        WHEN NOT EXISTS (SELECT 1 FROM RaceEventDistances WHERE RaceEventId = re.Id)
         AND NOT EXISTS (SELECT 1 FROM Races WHERE RaceEventId = re.Id)
        THEN 1 ELSE 0 
    END) AS [Sans Aucune Donnée - PROBLEME]
FROM RaceEvents re
WHERE re.EventDate >= GETDATE()
```

## Support

Si après avoir suivi ce guide les distances ne s'affichent toujours pas:

1. ✅ Vérifier les logs de debug dans Visual Studio
2. ✅ Exécuter le script de diagnostic complet
3. ✅ Vérifier que la base de données est correctement connectée
4. ✅ Vérifier que les dates des courses sont bien dans le futur

## Fichiers Importants

- **DiagnosticDistancesMailling.sql** - Script de diagnostic complet
- **MemberMailingViewModel.cs** - Code avec logs de diagnostic
- **MAILING_DISTANCES_FIX.md** - Documentation technique détaillée

---
**Status**: Solution avec logs de diagnostic implémentée ✅  
**Prochaine étape**: Exécuter DiagnosticDistancesMailling.sql pour identifier les courses problématiques
