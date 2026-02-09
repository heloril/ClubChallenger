-- Query to check Race Events and their distance configuration
-- Use this to diagnose why distances might not appear in mailings

-- ============================================
-- 1. Upcoming Race Events (Current Week +)
-- ============================================
SELECT 
    re.Id AS RaceEventId,
    re.Name AS RaceName,
    FORMAT(re.EventDate, 'dd/MM/yyyy') AS EventDate,
    re.Location,
    -- Check if distances are pre-configured
    (SELECT COUNT(*) FROM RaceEventDistances WHERE RaceEventId = re.Id) AS PreConfiguredDistances,
    -- Check if there are past races with this event ID
    (SELECT COUNT(*) FROM Races WHERE RaceEventId = re.Id) AS PastRacesCount,
    -- Show actual configured distances
    (SELECT STRING_AGG(CAST(DistanceKm AS VARCHAR), ', ') 
     FROM RaceEventDistances 
     WHERE RaceEventId = re.Id) AS ConfiguredDistances,
    -- Show distances from past races
    (SELECT STRING_AGG(CAST(DISTINCT DistanceKm AS VARCHAR), ', ') 
     FROM Races 
     WHERE RaceEventId = re.Id) AS PastRaceDistances
FROM RaceEvents re
WHERE re.EventDate >= GETDATE()
ORDER BY re.EventDate

-- ============================================
-- 2. Race Events Without ANY Distance Info
-- ============================================
SELECT 
    re.Id AS RaceEventId,
    re.Name AS RaceName,
    FORMAT(re.EventDate, 'dd/MM/yyyy') AS EventDate,
    re.Location,
    'No distances configured - will show "A confirmer"' AS Status
FROM RaceEvents re
WHERE re.EventDate >= GETDATE()
  AND NOT EXISTS (SELECT 1 FROM RaceEventDistances WHERE RaceEventId = re.Id)
  AND NOT EXISTS (SELECT 1 FROM Races WHERE RaceEventId = re.Id)
ORDER BY re.EventDate

-- ============================================
-- 3. Challenge Race Events (All)
-- ============================================
SELECT 
    re.Id AS RaceEventId,
    re.Name AS RaceName,
    FORMAT(re.EventDate, 'dd/MM/yyyy') AS EventDate,
    c.Name AS ChallengeName,
    (SELECT STRING_AGG(CAST(DistanceKm AS VARCHAR), ', ') 
     FROM RaceEventDistances 
     WHERE RaceEventId = re.Id) AS ConfiguredDistances,
    (SELECT STRING_AGG(CAST(DISTINCT DistanceKm AS VARCHAR), ', ') 
     FROM Races 
     WHERE RaceEventId = re.Id) AS PastRaceDistances
FROM RaceEvents re
INNER JOIN ChallengeRaceEvents cre ON re.Id = cre.RaceEventId
INNER JOIN Challenges c ON cre.ChallengeId = c.Id
WHERE re.EventDate >= GETDATE()
ORDER BY re.EventDate

-- ============================================
-- 4. Add Missing Distances for Known Races
-- ============================================
-- Template: Uncomment and replace values as needed

-- Example: Add 10km distance to a specific race event
-- INSERT INTO RaceEventDistances (RaceEventId, DistanceKm) VALUES (123, 10.0)

-- Example: Add multiple distances to a race event
-- INSERT INTO RaceEventDistances (RaceEventId, DistanceKm) VALUES 
--   (123, 5.0),
--   (123, 10.0),
--   (123, 21.1)

-- ============================================
-- 5. Copy Distances from Past Races (Automatic)
-- ============================================
-- This will automatically configure RaceEventDistances based on past races
-- Only for races that don't already have configured distances

INSERT INTO RaceEventDistances (RaceEventId, DistanceKm)
SELECT DISTINCT 
    r.RaceEventId,
    r.DistanceKm
FROM Races r
INNER JOIN RaceEvents re ON r.RaceEventId = re.Id
WHERE r.RaceEventId IS NOT NULL
  AND re.EventDate >= GETDATE()  -- Only for upcoming races
  AND NOT EXISTS (
      SELECT 1 
      FROM RaceEventDistances red 
      WHERE red.RaceEventId = r.RaceEventId 
        AND red.DistanceKm = r.DistanceKm
  )
ORDER BY r.RaceEventId, r.DistanceKm

-- Check results
SELECT 
    re.Name,
    red.DistanceKm,
    'Auto-configured' AS Source
FROM RaceEventDistances red
INNER JOIN RaceEvents re ON red.RaceEventId = re.Id
WHERE re.EventDate >= GETDATE()
ORDER BY re.EventDate, red.DistanceKm
