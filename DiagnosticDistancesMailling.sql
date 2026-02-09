-- Diagnostic et Configuration des Distances pour les Courses à Venir
-- Ce script vous aide à comprendre pourquoi les distances ne s'affichent pas

-- ============================================
-- ETAPE 1: DIAGNOSTIC - Courses à venir sans distances
-- ============================================
PRINT '=== COURSES A VENIR SANS DISTANCES ==='
PRINT ''

SELECT 
    re.Id AS RaceEventId,
    re.Name AS [Nom de la Course],
    FORMAT(re.EventDate, 'dd/MM/yyyy') AS [Date],
    re.Location AS [Lieu],
    -- Vérifier RaceEventDistances
    CASE 
        WHEN EXISTS (SELECT 1 FROM RaceEventDistances WHERE RaceEventId = re.Id) 
        THEN 'OUI (' + CAST((SELECT COUNT(*) FROM RaceEventDistances WHERE RaceEventId = re.Id) AS VARCHAR) + ' distances)'
        ELSE 'NON - MANQUANT'
    END AS [Distances Configurées],
    -- Vérifier races passées
    CASE 
        WHEN EXISTS (SELECT 1 FROM Races WHERE RaceEventId = re.Id)
        THEN 'OUI (' + CAST((SELECT COUNT(DISTINCT DistanceKm) FROM Races WHERE RaceEventId = re.Id) AS VARCHAR) + ' distances)'
        ELSE 'NON - Aucune course passée'
    END AS [Courses Passées Disponibles],
    -- Afficher les distances si elles existent
    (SELECT STRING_AGG(CAST(DistanceKm AS VARCHAR), ', ') 
     FROM RaceEventDistances 
     WHERE RaceEventId = re.Id) AS [Distances Dans RaceEventDistances],
    (SELECT STRING_AGG(CAST(DISTINCT DistanceKm AS VARCHAR), ', ') 
     FROM Races 
     WHERE RaceEventId = re.Id) AS [Distances Des Courses Passées]
FROM RaceEvents re
WHERE re.EventDate >= GETDATE()  -- Courses à venir uniquement
ORDER BY re.EventDate

PRINT ''
PRINT '=== INTERPRETATION DES RESULTATS ==='
PRINT '- Si "Distances Configurées" = NON ET "Courses Passées Disponibles" = NON'
PRINT '  --> Le mailing affichera "A confirmer" car aucune donnée de distance'
PRINT ''
PRINT '- Si "Distances Configurées" = NON MAIS "Courses Passées Disponibles" = OUI'
PRINT '  --> Le mailing DEVRAIT afficher les distances des courses passées'
PRINT '  --> Si ça n affiche toujours pas, vérifier le code du ViewModel'
PRINT ''
PRINT '- Si "Distances Configurées" = OUI'
PRINT '  --> Le mailing affichera ces distances configurées'
PRINT ''

-- ============================================
-- ETAPE 2: SOLUTION RAPIDE - Copier les distances des courses passées
-- ============================================
PRINT '=== SOLUTION AUTOMATIQUE ==='
PRINT 'Exécutez cette requête pour copier automatiquement les distances'
PRINT 'des courses passées vers RaceEventDistances pour les courses à venir:'
PRINT ''

-- Cette requête copie les distances des races passées vers RaceEventDistances
-- Seulement pour les événements futurs qui n'ont pas encore de distances configurées
INSERT INTO RaceEventDistances (RaceEventId, DistanceKm)
SELECT DISTINCT 
    r.RaceEventId,
    r.DistanceKm
FROM Races r
INNER JOIN RaceEvents re ON r.RaceEventId = re.Id
WHERE r.RaceEventId IS NOT NULL
  AND re.EventDate >= GETDATE()  -- Seulement les courses à venir
  AND NOT EXISTS (
      -- Ne pas dupliquer si la distance existe déjà
      SELECT 1 
      FROM RaceEventDistances red 
      WHERE red.RaceEventId = r.RaceEventId 
        AND red.DistanceKm = r.DistanceKm
  )

-- Afficher les résultats
DECLARE @rowCount INT = @@ROWCOUNT
PRINT CAST(@rowCount AS VARCHAR) + ' distance(s) copiée(s) vers RaceEventDistances'
PRINT ''

-- ============================================
-- ETAPE 3: CONFIGURATION MANUELLE (si nécessaire)
-- ============================================
PRINT '=== CONFIGURATION MANUELLE ==='
PRINT 'Si certaines courses n ont toujours pas de distances, ajoutez-les manuellement:'
PRINT ''

-- Template pour ajouter des distances manuellement
PRINT '-- Exemple: Ajouter 10 km à la course ID 123'
PRINT '-- INSERT INTO RaceEventDistances (RaceEventId, DistanceKm) VALUES (123, 10.0)'
PRINT ''
PRINT '-- Exemple: Ajouter plusieurs distances'
PRINT '-- INSERT INTO RaceEventDistances (RaceEventId, DistanceKm) VALUES '
PRINT '--   (123, 5.0),'
PRINT '--   (123, 10.0),'
PRINT '--   (123, 21.1)'
PRINT ''

-- ============================================
-- ETAPE 4: VERIFICATION FINALE
-- ============================================
PRINT '=== VERIFICATION ==='
PRINT 'Courses à venir avec leurs distances configurées:'
PRINT ''

SELECT 
    re.Id,
    re.Name AS [Course],
    FORMAT(re.EventDate, 'dd/MM/yyyy') AS [Date],
    STRING_AGG(CAST(red.DistanceKm AS VARCHAR), ', ') AS [Distances (km)],
    COUNT(red.Id) AS [Nombre de distances]
FROM RaceEvents re
LEFT JOIN RaceEventDistances red ON re.Id = red.RaceEventId
WHERE re.EventDate >= GETDATE()
GROUP BY re.Id, re.Name, re.EventDate
ORDER BY re.EventDate

PRINT ''
PRINT '=== DISTANCES COURANTES POUR CHALLENGES ==='
PRINT 'Reference pour configuration manuelle:'
PRINT ''
PRINT '- CrossCup / CJPL: Généralement 10.0 ou 10.2 km'
PRINT '- Challenge Lucien Campeggio: 5.0 km, 10.0 km (variable selon la course)'
PRINT '- Jogging de l An Neuf: 5.0 km, 10.0 km'
PRINT '- Semi-marathon: 21.1 km'
PRINT '- Marathon: 42.2 km'
PRINT ''

-- ============================================
-- BONUS: Voir toutes les distances utilisées historiquement
-- ============================================
PRINT '=== DISTANCES HISTORIQUES PAR COURSE ==='
PRINT ''

SELECT 
    re.Name AS [Nom de la Course],
    STRING_AGG(CAST(DISTINCT r.DistanceKm AS VARCHAR), ', ') AS [Distances Historiques],
    COUNT(r.Id) AS [Nombre d éditions],
    MIN(re.EventDate) AS [Première édition],
    MAX(re.EventDate) AS [Dernière édition]
FROM RaceEvents re
INNER JOIN Races r ON re.Id = r.RaceEventId
GROUP BY re.Name
HAVING COUNT(r.Id) > 0
ORDER BY re.Name

PRINT ''
PRINT '=== FIN DU DIAGNOSTIC ==='
PRINT 'Si les distances s affichent toujours pas après avoir exécuté ce script,'
PRINT 'vérifiez que le code du MemberMailingViewModel charge bien les distances.'
