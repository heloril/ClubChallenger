# Parser Restructure Specification

## Overview

The PDF parsing system has been restructured to support 4 specific Belgian race timing systems, plus a standard fallback parser.

## Parser Priority Order

Parsers are evaluated in this order (most specific first):

1. **OtopFormatParser**
2. **GlobalPacingFormatParser**
3. **ChallengeLaMeuseFormatParser**
4. **GoalTimingFormatParser**
5. **StandardFormatParser** (fallback)

---

## 1. Otop Format Parser

### Detection
- Presence of "pl./s." AND "pl./c." headers
- OR "otop" keyword
- OR "sexe" AND "catég." headers

### Column Mapping

| Column Header | Maps To | Notes |
|--------------|---------|-------|
| Place | Position | |
| Dos. | N/A | Bib number (not used) |
| Nom | LastName | |
| Prénom | FirstName | |
| Sexe | Sex | Values: m, f, M, F |
| Pl./S. | PositionBySex | |
| Catég. | AgeCategory | |
| Pl./C. | PositionByCategory | |
| Temps | RaceTime | |
| Vitesse | Speed | |
| Moy. | TimePerKm | Average pace |
| Points | N/A | (not used) |
| Jetons | N/A | (not used) |

### Sex Values
- `m`, `M` → Male
- `f`, `F` → Female

### Valid Categories
- Senior H
- Moins16 H
- Espoir H
- Veteran 1
- Moins16 D
- Vétéran 2
- Veteran 3
- Ainée 1
- Espoir D
- Senior D
- Ainée 3
- Vétéran 4
- Ainée 2
- Ainée 4

---

## 2. Global Pacing Format Parser

### Detection
- Presence of "clas.sexe" AND "clas.cat" headers
- OR "global pacing" / "globalpacing" keyword

### Column Mapping

| Column Header | Maps To | Notes |
|--------------|---------|-------|
| Pl. | Position | |
| Dos | N/A | Bib number (not used) |
| Nom | LastName, FirstName | Format: "LastName, FirstName" |
| Sexe | Sex | Values: m, f, M, F |
| Clas.Sexe | PositionBySex | |
| Cat | AgeCategory | |
| Clas.Cat | PositionByCategory | |
| Club | Team | |
| Vitesse | Speed | |
| min/km | TimePerKm | |
| Temps | RaceTime | |
| Points | N/A | (not used) |

### Sex Values
- `m`, `M` → Male
- `f`, `F` → Female

### Valid Categories
- Sen (Seniors)
- V1 (Veterans 1)
- V2 (Veterans 2)
- Dam (Dames)
- Esp G (Espoirs Garçons)
- V3 (Veterans 3)
- A2 (Ainées 2)
- A1 (Ainées 1)
- A3 (Ainées 3)
- V4 (Veterans 4)
- Esp F (Espoirs Filles)

### Name Format
Names are in the format: "LASTNAME, Firstname"
- Must be split on comma
- LastName comes first (typically uppercase)
- FirstName comes after comma

---

## 3. Challenge La Meuse Format Parser

### Detection
- Presence of "zatopek" keyword OR filename contains "zatopek"
- OR "p.ca" header (position par catégorie)

### Column Mapping

| Column Header | Maps To | Notes |
|--------------|---------|-------|
| Pos. | Position | |
| Nom | FirstName, LastName | Format: "Firstname LASTNAME" |
| Dos. | N/A | Bib number (not used) |
| Temps | RaceTime | |
| Vitesse | Speed | |
| Allure | TimePerKm | Pace |
| Club | Team | |
| Catégorie | AgeCategory | |
| P.Ca | PositionByCategory | |
| D.Cha | N/A | (not used) |

### Valid Categories (Canonical)
- Séniors
- Vétérans 1
- Espoirs Garçons
- Vétérans 2
- Espoirs Filles
- Ainées 2
- Vétérans 3
- Dames
- Ainées 1
- Ainées 3
- Vétérans 4
- Ainées 4

### Special Features
- Uses canonical category mapping (exact accents and capitalization)
- Name format: "Firstname LASTNAME" (firstname first, lastname often uppercase)
- P.Ca can appear as "P.Ca", "P Ca", "P/Ca", etc.

---

## 4. Goal Timing Format Parser

### Detection
- Presence of "rank" AND "pl/cat" headers
- OR "goal timing" / "goaltiming" keyword
- OR "rank" AND "t/km" headers

### Column Mapping

| Column Header | Maps To | Notes |
|--------------|---------|-------|
| Rank | Position | |
| Dos | N/A | Bib number (not used) |
| [empty] | N/A | Empty column |
| Nom Prenom | LastName, FirstName | Format: "LASTNAME Firstname" |
| Sexe | Sex | Values: H, F |
| Club | Team | |
| Cat | AgeCategory | |
| Pl/Cat | PositionByCategory | |
| Temps | RaceTime | |
| T/Km | TimePerKm | |
| Vitesse | Speed | |
| Points | N/A | (not used) |

### Sex Values
- `H` → Male (Homme)
- `F` → Female (Femme)

### Valid Categories
- SH (Senior Homme)
- V1 (Veterans 1)
- V2 (Veterans 2)
- SD (Senior Dame)
- V3 (Veterans 3)
- ESH (Espoir Homme)
- A2 (Ainées 2)
- A1 (Ainées 1)
- V4 (Veterans 4)
- ESF (Espoir Femme)
- V5 (Veterans 5)
- A3 (Ainées 3)
- A4 (Ainées 4)
- A5 (Ainées 5)

### Name Format
Names are in the format: "LASTNAME Firstname"
- LastName comes first (typically uppercase)
- FirstName follows (typically proper case)
- No comma separator

---

## 5. Standard Format Parser

### Purpose
Fallback parser for PDFs that don't match any specific timing system.

### Detection
Always returns `true` (catches all remaining PDFs).

### Features
- Generic pattern-based parsing
- Attempts to extract:
  - Position (leading number)
  - Name (text before times/speeds)
  - Times (HH:MM:SS or MM:SS patterns)
  - Speed (decimal number with km/h)
  - Team (text in parentheses/brackets)
  - Basic category codes (SH, V1, V2, etc.)

### Limitations
- Less accurate than specialized parsers
- May not extract all fields
- Category detection limited to simple codes

---

## Implementation Status

### ✅ Completed
- Parser priority order defined
- Detection methods for each parser
- Column mappings documented
- Category lists documented
- ChallengeLaMeuseFormatParser implemented

### 🚧 In Progress
- OtopFormatParser implementation
- GlobalPacingFormatParser implementation
- GoalTimingFormatParser implementation

### 📋 TODO
- Implement column-based parsing for Otop
- Implement column-based parsing for Global Pacing
- Implement column-based parsing for Goal Timing
- Test each parser with real PDF samples
- Update documentation with examples

---

## Testing Requirements

Each parser should be tested with:
1. **Real PDF samples** from the timing system
2. **Column header detection** accuracy
3. **Data extraction** completeness
4. **Category mapping** correctness
5. **Name format parsing** (especially for different formats)
6. **Edge cases** (missing columns, malformed data)

---

## Migration Notes

### Removed Parsers
- ❌ ChallengeCondrusienFormatParser
- ❌ FrenchColumnFormatParser
- ❌ CJPLFormatParser

### Reason
These were generic parsers that didn't match specific timing systems. They have been replaced with the 4 specialized timing system parsers.

### Impact
- Existing PDFs should now be parsed more accurately
- Each timing system has dedicated logic
- Category mappings are system-specific
- Sex value mappings are system-specific

---

## Date
2025-01-XX (Restructure specification)
