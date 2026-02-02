# Two Tab Classification - Visual Guide

## Application Layout

```
┌─────────────────────────────────────────────────────────────────┐
│                  Race Management System                          │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  ┌──────────────┬────────────────────┬───────────────────────┐ │
│  │ Upload &     │ 🏁 Race            │ 📊 General           │ │
│  │ Process Race │   Classification   │   Classification      │ │
│  └──────────────┴────────────────────┴───────────────────────┘ │
│                                                                  │
│  [Active Tab Content Displayed Here]                            │
│                                                                  │
├─────────────────────────────────────────────────────────────────┤
│  Status: Ready...                                                │
└─────────────────────────────────────────────────────────────────┘
```

## Tab 1: Upload & Process Race

```
┌─────────────────────┬───────────────────────────────────────┐
│  RACE INFORMATION  │         INSTRUCTIONS                  │
│                     │                                       │
│  ┌──────────────┐  │  1. Browse for Excel file            │
│  │ Excel File   │  │  2. Enter race information:          │
│  │              │  │     • Race Name                       │
│  │ [Path...   ] │  │     • Year                            │
│  │ [📁 Browse ] │  │     • Race Number                     │
│  └──────────────┘  │     • Distance                        │
│                     │  3. Process Race                      │
│  ┌──────────────┐  │  4. View results in other tabs       │
│  │ Race Details │  │                                       │
│  │              │  │  ⚠️ Requirements:                     │
│  │ Name: [___]  │  │   • Members.json file exists         │
│  │ Year: [___]  │  │   • Excel file format correct        │
│  │ Race#:[___]  │  │   • File includes reference time     │
│  │ Dist: [___]  │  │                                       │
│  └──────────────┘  │                                       │
│                     │                                       │
│  [⚡ Process Race]  │                                       │
│   [Processing...]   │                                       │
└─────────────────────┴───────────────────────────────────────┘
```

## Tab 2: 🏁 Race Classification

Shows results for **individual races**

```
┌─────────────────────────────────────────────────────────────────┐
│  SELECT RACE                                                     │
│  ┌────────────────────────────────────────────────────────────┐ │
│  │ [🔄 Refresh] [👁️ View] [💾 Download] [🗑️ Delete]        │ │
│  │                                                              │ │
│  │ ID │Year│Race#│Name            │Dist│Status   │Date       │ │
│  │ ══════════════════════════════════════════════════════════ │ │
│  │ 1  │2024│ 1   │Marathon Brussels│ 42 │Processed│2024-01-15│ │
│  │ 2  │2024│ 2   │City Run        │ 10 │Processed│2024-02-20│ │
│  │ ...                                                         │ │
│  └────────────────────────────────────────────────────────────┘ │
│                                                                  │
│  RACE RESULTS                                                    │
│  ┌────────────────────────────────────────────────────────────┐ │
│  │Rank│Pos│FirstName│LastName│Team│Points│Time  │T/km│Speed││ │
│  │══════════════════════════════════════════════════════════│ │
│  │ 1  │ 1 │John     │Doe     │A   │ 100  │30:00│3:00│20.0 ││ │
│  │ 2  │ 2 │Jane     │Smith   │B   │  95  │31:00│3:06│19.4 ││ │
│  │ 3  │ 3 │Bob      │Jones   │A   │  90  │32:00│3:12│18.8 ││ │
│  │ ...                                                         │ │
│  │                                                              │ │
│  │ Each row shows:                                             │ │
│  │  • Points: Calculated for THIS race only                   │ │
│  │  • Bonus KM: Race distance (e.g., 10 km)                   │ │
│  └────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────┘
```

### Race Classification Columns Explained:
- **Rank**: Database ID / ordering
- **Position**: Actual finishing position in race
- **Points**: Race-specific points (based on performance vs. TREF)
- **Bonus KM**: Distance of this race
- **Race Time**: Total time for the race
- **Time/km**: Average pace
- **Speed**: km/h average speed
- **Member**: Whether participant is a registered member

## Tab 3: 📊 General Classification

Shows **aggregated results** across all races in a year

```
┌─────────────────────────────────────────────────────────────────┐
│  Select Year: [2024 ▼] [📊 Load Classification]                │
│  General classification sums points from all races + bonus KM    │
│                                                                  │
│  GENERAL CLASSIFICATION RESULTS                                  │
│  ┌────────────────────────────────────────────────────────────┐ │
│  │Rank│FirstName│LastName│Team│Total│Races│Avg│Best│BestTime││ │
│  │    │         │        │    │Pts  │     │Pts│Pos│        ││ │
│  │══════════════════════════════════════════════════════════│ │
│  │ 1  │John     │Doe     │A   │ 350 │ 5   │ 70│ 1 │ 30:00  ││ │
│  │ 2  │Jane     │Smith   │B   │ 320 │ 4   │ 80│ 2 │ 31:00  ││ │
│  │ 3  │Bob      │Jones   │A   │ 300 │ 5   │ 60│ 3 │ 32:00  ││ │
│  │ ...                                                         │ │
│  │                                                              │ │
│  │ Each row shows:                                             │ │
│  │  • Total Points: Sum of points from ALL races in year      │ │
│  │  • Total Bonus KM: Sum of all race distances participated  │ │
│  │  • Races: Number of races participated in                  │ │
│  │  • Avg Points: Average points per race                     │ │
│  │  • Best Pos: Best finishing position across all races      │ │
│  │  • Best Time: Fastest race time achieved                   │ │
│  └────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────┘
```

### General Classification Columns Explained:
- **Rank**: Overall ranking for the year
- **Total Points**: Sum of points from all races in the year
- **Total Bonus KM**: Sum of distances from all races participated
- **Races**: Count of races participated in
- **Avg Points**: Average points per race
- **Best Pos**: Best finishing position across all races
- **Best Time**: Fastest race time achieved in any race
- **Best T/km**: Best pace achieved in any race

## Points Calculation Examples

### Example 1: Race Classification
```
Race: Marathon Brussels (42 km)
Member: John Doe
Race Time: 3:30:00
Points Earned: 100 points
Bonus KM: 42 km

Result stored in Classifications table:
  RaceId: 1
  Points: 100
  BonusKm: 42
```

### Example 2: General Classification
```
Year: 2024
Member: John Doe

Races participated:
  Race 1 (42km): 100 points + 42 bonus km
  Race 2 (10km):  95 points + 10 bonus km
  Race 3 (21km):  98 points + 21 bonus km

General Classification:
  Total Points: 100 + 95 + 98 = 293 points
  Total Bonus KM: 42 + 10 + 21 = 73 km
  Race Count: 3
  Average Points: 293 / 3 = 97.7 points
```

## Workflow Diagram

```
┌──────────────┐
│ Upload Race  │
│ Process Race │
└──────┬───────┘
       │
       ├─────────────────────┬──────────────────────┐
       │                     │                      │
       ▼                     ▼                      ▼
┌────────────┐     ┌─────────────────┐   ┌──────────────────┐
│Race Results│     │ Race            │   │ General          │
│Stored in DB│────▶│ Classification  │   │ Classification   │
└────────────┘     │ (Per Race)      │   │ (Per Year)       │
                   │                 │   │                  │
                   │ • View specific │   │ • Select year    │
                   │   race results  │   │ • View aggregate │
                   │ • Download      │   │   results        │
                   │ • Delete        │   │ • See totals     │
                   └─────────────────┘   └──────────────────┘
```

## Key Features

### Race Classification Tab
✅ Individual race results
✅ Race-specific points
✅ Detailed performance metrics
✅ Download and delete functionality
✅ Select any processed race to view

### General Classification Tab
✅ Yearly aggregated results
✅ Sum of all race points
✅ Sum of all bonus kilometers
✅ Statistical summaries (avg, best)
✅ Member-only results
✅ Year selection

## Benefits

1. **Clear Separation**: No confusion between race and yearly results
2. **Dedicated Space**: Each view has full screen real estate
3. **No Toggles**: Simple tab navigation instead of toggle buttons
4. **Contextual**: Each tab shows relevant information and actions
5. **Scalable**: Easy to add more features to each tab independently
