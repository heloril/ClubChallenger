# Points Calculation Detailed Guide

## Overview
This document explains how points are calculated for race classification and how they are aggregated for general classification.

## Points Calculation Formula

### For Each Race (Per Member)

**Formula:**
```
Points = (Reference Time / Member Time) × 1000
```

**Implementation:**
```csharp
public int CalculatePoints(TimeSpan referenceTime, TimeSpan memberTime)
{
    if (memberTime.TotalSeconds == 0)
        throw new ArgumentException("Member time cannot be zero", nameof(memberTime));

    var points = Math.Round(referenceTime.TotalSeconds / memberTime.TotalSeconds * 1000);
    return (int)points;
}
```

### Example 1: Member Faster Than Reference
```
Reference Time (TREF): 30:00 (1800 seconds)
Member Time:          27:00 (1620 seconds)

Points = (1800 / 1620) × 1000
       = 1.111 × 1000
       = 1111 points

Result: Member gets 1111 points (above 1000 = faster than reference)
```

### Example 2: Member Slower Than Reference
```
Reference Time (TREF): 30:00 (1800 seconds)
Member Time:          33:00 (1980 seconds)

Points = (1800 / 1980) × 1000
       = 0.909 × 1000
       = 909 points

Result: Member gets 909 points (below 1000 = slower than reference)
```

### Example 3: Member Same as Reference
```
Reference Time (TREF): 30:00 (1800 seconds)
Member Time:          30:00 (1800 seconds)

Points = (1800 / 1800) × 1000
       = 1.000 × 1000
       = 1000 points

Result: Member gets exactly 1000 points (equal to reference)
```

## Bonus Kilometers

### Per Race
Each race awards **Bonus KM equal to the race distance**:

```
Race Distance: 10 km  → Bonus KM: 10
Race Distance: 21 km  → Bonus KM: 21
Race Distance: 42 km  → Bonus KM: 42
```

**Implementation:**
```csharp
public void AddOrUpdateResult(Member member, Race race, int points, ...)
{
    // ...
    _classifications[key] = new MemberClassification(
        member, 
        race, 
        points, 
        race.DistanceKm,  // <- Bonus KM = Race Distance
        raceTime, 
        timePerKm, 
        position, 
        team, 
        speed, 
        isMember
    );
}
```

## Race Classification Storage

### Database Schema (Classifications Table)

Each race result is stored with:

```sql
ClassificationEntity:
  - Id (Primary Key)
  - RaceId (Foreign Key)
  - MemberFirstName
  - MemberLastName
  - MemberEmail
  - Points             -- Points for THIS race only
  - BonusKm           -- Distance of THIS race
  - RaceTime          -- Member's time in this race
  - TimePerKm         -- Average pace
  - Position          -- Finishing position
  - Team
  - Speed
  - IsMember
  - CreatedDate
```

### Example Race Results Stored

**Race 1: Marathon Brussels (42 km)**
```
TREF: 3:00:00 (10800 seconds)

+------+----------+---------+--------+---------+----------+
| Name | Time     | Calc    | Points | BonusKm | Position |
+------+----------+---------+--------+---------+----------+
| John | 2:45:00  | 10800/  | 1091   | 42      | 1        |
|      | (9900s)  | 9900×   |        |         |          |
|      |          | 1000    |        |         |          |
+------+----------+---------+--------+---------+----------+
| Jane | 3:00:00  | 10800/  | 1000   | 42      | 2        |
|      | (10800s) | 10800×  |        |         |          |
|      |          | 1000    |        |         |          |
+------+----------+---------+--------+---------+----------+
| Bob  | 3:30:00  | 10800/  | 857    | 42      | 3        |
|      | (12600s) | 12600×  |        |         |          |
|      |          | 1000    |        |         |          |
+------+----------+---------+--------+---------+----------+
```

**Race 2: City Run (10 km)**
```
TREF: 40:00 (2400 seconds)

+------+----------+---------+--------+---------+----------+
| Name | Time     | Calc    | Points | BonusKm | Position |
+------+----------+---------+--------+---------+----------+
| John | 38:00    | 2400/   | 1053   | 10      | 1        |
|      | (2280s)  | 2280×   |        |         |          |
|      |          | 1000    |        |         |          |
+------+----------+---------+--------+---------+----------+
| Jane | 42:00    | 2400/   | 952    | 10      | 3        |
|      | (2520s)  | 2520×   |        |         |          |
|      |          | 1000    |        |         |          |
+------+----------+---------+--------+---------+----------+
| Bob  | 39:00    | 2400/   | 1026   | 10      | 2        |
|      | (2340s)  | 2340×   |        |         |          |
|      |          | 1000    |        |         |          |
+------+----------+---------+--------+---------+----------+
```

## General Classification Calculation

### Aggregation Query

**SQL Logic (from ClassificationRepository):**
```csharp
var generalClassification = context.Classifications
    .Include(c => c.Race)
    .Where(c => c.Race.Year == year && c.IsMember) // Only members
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
        MemberEmail = g.Key.MemberEmail,
        Team = g.Key.Team,
        TotalPoints = g.Sum(c => c.Points),          // Sum all race points
        TotalBonusKm = g.Sum(c => c.BonusKm),        // Sum all bonus km
        RaceCount = g.Count(),                        // Count races
        AveragePoints = (int)g.Average(c => c.Points),// Average points
        BestPosition = g.Min(c => c.Position),        // Best position
        BestRaceTime = g.Where(c => c.RaceTime.HasValue)
                        .Min(c => c.RaceTime),        // Fastest time
        BestTimePerKm = g.Where(c => c.TimePerKm.HasValue)
                         .Min(c => c.TimePerKm)       // Best pace
    })
    .OrderByDescending(c => c.TotalPoints)            // Sort by points
    .ThenByDescending(c => c.TotalBonusKm)            // Then by bonus km
    .ToList();
```

### Example General Classification Calculation

**Year: 2024**
**Member: John Doe**

```
Race 1 (Marathon Brussels, 42km):
  Points: 1091
  Bonus KM: 42

Race 2 (City Run, 10km):
  Points: 1053
  Bonus KM: 10

Race 3 (Half Marathon, 21km):
  Points: 1020
  Bonus KM: 21

────────────────────────────────────────────
GENERAL CLASSIFICATION:

Total Points    = 1091 + 1053 + 1020 = 3164 points
Total Bonus KM  = 42 + 10 + 21 = 73 km
Race Count      = 3 races
Average Points  = 3164 / 3 = 1055 points
Best Position   = MIN(1, 1, 1) = 1
Best Race Time  = MIN(2:45:00, 0:38:00, 1:25:00) = 0:38:00
Best Time/km    = Best pace achieved
────────────────────────────────────────────
```

### Full Example with 3 Members

**2024 Season Results:**

```
┌──────┬────────┬────────┬────────┬─────────┬────────────┐
│Member│ Race 1 │ Race 2 │ Race 3 │ Total   │ Total      │
│      │ (42km) │ (10km) │ (21km) │ Points  │ Bonus KM   │
├──────┼────────┼────────┼────────┼─────────┼────────────┤
│ John │ 1091   │ 1053   │ 1020   │ 3164    │ 73         │
│      │ +42    │ +10    │ +21    │         │            │
├──────┼────────┼────────┼────────┼─────────┼────────────┤
│ Jane │ 1000   │  952   │  990   │ 2942    │ 73         │
│      │ +42    │ +10    │ +21    │         │            │
├──────┼────────┼────────┼────────┼─────────┼────────────┤
│ Bob  │  857   │ 1026   │  900   │ 2783    │ 52         │
│      │ +42    │ +10    │  -     │         │ (no Race3) │
└──────┴────────┴────────┴────────┴─────────┴────────────┘

RANKING (OrderBy TotalPoints DESC, TotalBonusKm DESC):
1. John - 3164 points (73 km)
2. Jane - 2942 points (73 km)
3. Bob  - 2783 points (52 km)
```

## Important Notes

### 1. Points Are Independent Per Race
- Each race calculates points independently
- Points depend only on TREF and member's time for that race
- Previous race results do NOT affect current race points

### 2. Bonus KM Is Always Race Distance
- Not dependent on performance
- Every participant gets the same bonus KM for the same race
- Accumulated in general classification

### 3. General Classification Filters
- Only includes members (IsMember = true)
- Non-members appear in race classification but not general
- Grouped by member (FirstName, LastName, Email, Team)

### 4. Tie Breaking
```csharp
.OrderByDescending(c => c.TotalPoints)      // Primary: Total points
.ThenByDescending(c => c.TotalBonusKm)      // Secondary: Bonus KM
```
If two members have same total points, more bonus KM ranks higher

### 5. Ranking Assignment
```csharp
int rank = 1;
foreach (var classification in generalClassification)
{
    classification.Rank = rank++;
}
```
Rank is assigned sequentially after sorting (1, 2, 3, ...)

## UI Display

### Race Classification Tab
Shows **individual race results**:
- Points column = points for that specific race
- Bonus KM column = distance of that race
- One row per member per race

### General Classification Tab
Shows **aggregated yearly results**:
- Total Points column = sum of all race points
- Total Bonus KM column = sum of all race distances
- Races column = number of races participated
- One row per member (all races aggregated)

## Data Flow

```
1. Upload Excel File
   ↓
2. Extract race results (times)
   ↓
3. Calculate points for each member
   Points = (TREF / MemberTime) × 1000
   ↓
4. Store in Classifications table
   - RaceId
   - MemberName
   - Points (for this race)
   - BonusKm (race distance)
   ↓
5a. Race Classification Tab
    - Query: WHERE RaceId = selected
    - Display: Show all results for that race
    ↓
5b. General Classification Tab
    - Query: WHERE Year = selected AND IsMember = true
    - GroupBy: Member
    - Aggregate: SUM(Points), SUM(BonusKm), COUNT(*), etc.
    - Display: Show aggregated results per member
```

## Validation Rules

### Points Calculation
```csharp
public bool IsValidRaceTime(TimeSpan time)
{
    return time.TotalMinutes > 10 && time.TotalHours < 5;
}
```
- Minimum: 10 minutes
- Maximum: 5 hours
- Zero time throws exception

### Database Constraints
```csharp
[Required]
public int Points { get; set; }

[Required]
public int BonusKm { get; set; }
```
- Points and BonusKm are required
- Cannot be null

## Summary

| Aspect | Race Classification | General Classification |
|--------|---------------------|------------------------|
| Scope | Single race | All races in a year |
| Points | Calculated per race | Sum of all race points |
| Bonus KM | Race distance | Sum of all race distances |
| Participants | Members + Non-members | Members only |
| Rows | One per member per race | One per member (aggregated) |
| Calculation | TREF / Time × 1000 | SUM(Points) |
| Display | Race-specific results | Yearly rankings |
