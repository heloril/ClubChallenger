# Belgian Race Categories - Complete Reference Guide

## Overview
This document contains all observed category codes from Belgian race results (PDF and Excel files).

## Category Patterns by Type

### 1. **Gender Categories**
| Code | Full Name | Description |
|------|-----------|-------------|
| M | Homme / Masculin | Male |
| H | Homme | Male (alternative) |
| F | Femme / Féminin | Female |
| D | Dame | Female (alternative) |

### 2. **Senior Categories**
| Code | Full Name | Age Range |
|------|-----------|-----------|
| SH | Senior Homme | Men 20-39 |
| SM | Senior Masculin | Men 20-39 |
| SD | Senior Dame | Women 20-39 |
| SF | Senior Femme | Women 20-39 |
| SEN H | Senior Homme | Men 20-39 |
| SEN F | Senior Femme | Women 20-39 |
| SENH | Senior Homme | Men 20-39 |
| SENF | Senior Femme | Women 20-39 |

### 3. **Veteran Categories (Men)**
| Code | Full Name | Age Range |
|------|-----------|-----------|
| V1 | Veteran 1 | Men 40-49 |
| V2 | Veteran 2 | Men 50-59 |
| V3 | Veteran 3 | Men 60-69 |
| V4 | Veteran 4 | Men 70+ |
| VET1 | Veteran 1 | Men 40-49 |
| VET2 | Veteran 2 | Men 50-59 |
| VET3 | Veteran 3 | Men 60-69 |
| VETH | Veteran Homme | Men 40+ |

### 4. **Veteran Categories (Women)**
| Code | Full Name | Age Range |
|------|-----------|-----------|
| D1 | Dame 1 | Women 40-49 |
| D2 | Dame 2 | Women 50-59 |
| D3 | Dame 3 | Women 60+ |
| A1 | Ainée 1 | Women 40-49 |
| A2 | Ainée 2 | Women 50-59 |
| A3 | Ainée 3 | Women 60+ |
| AINÉE1 | Ainée 1 | Women 40-49 |
| AINÉE2 | Ainée 2 | Women 50-59 |
| AINÉE3 | Ainée 3 | Women 60+ |
| VETF | Veteran Femme | Women 40+ |

### 5. **Junior/Youth Categories**
| Code | Full Name | Age Range |
|------|-----------|-----------|
| ESP | Espoir | 18-19 |
| ESPH | Espoir Homme | Men 18-19 |
| ESPF | Espoir Femme | Women 18-19 |
| ESH | Espoir Senior Homme | Men 18-19 |
| ESF | Espoir Senior Femme | Women 18-19 |
| ESG | Espoir | Mixed 18-19 |
| JUN | Junior | 16-17 |
| JUNH | Junior Homme | Men 16-17 |
| JUNF | Junior Femme | Women 16-17 |
| CAD | Cadet | 14-15 |
| CADH | Cadet Homme | Boys 14-15 |
| CADF | Cadet Femme | Girls 14-15 |
| SCO | Scolaire | 12-13 |
| BEN | Benjamin | 10-11 |
| PUP | Poussin | 8-9 |
| MIN | Minime | 12-13 |

### 6. **Master Categories (Alternative System)**
| Code | Full Name | Age Range |
|------|-----------|-----------|
| M35 | Master 35+ | 35-39 |
| M40 | Master 40+ | 40-44 |
| M45 | Master 45+ | 45-49 |
| M50 | Master 50+ | 50-54 |
| M55 | Master 55+ | 55-59 |
| M60 | Master 60+ | 60-64 |
| M65 | Master 65+ | 65-69 |
| M70 | Master 70+ | 70+ |
| W35 | Women 35+ | 35-39 |
| W40 | Women 40+ | 40-44 |
| W45 | Women 45+ | 45-49 |
| W50 | Women 50+ | 50-54 |
| W55 | Women 55+ | 55-59 |
| W60 | Women 60+ | 60+ |

### 7. **Handisport Categories**
| Code | Full Name |
|------|-----------|
| HAN | Handisport |
| HAND | Handisport |

### 8. **Fun/Non-Competitive**
| Code | Full Name |
|------|-----------|
| REC | Recreatif |
| FUN | Fun Run |
| WAL | Marche |

## Regex Patterns for Detection

### Short Codes (2-4 characters)
```regex
^(SH|SD|SF|SM|V[1-4]|D[1-3]|A[1-3]|M\d{2}|W\d{2}|ESP|JUN|CAD|SCO|BEN|PUP|MIN|HAN|REC|FUN|WAL|ESH|ESF|ESG)$
```

### Long Codes (5+ characters)
```regex
^(SENH|SENF|SENIOR\s*[HF]|VET[1-3H]|AINÉE[1-3]|ESPH|ESPF|JUNH|JUNF|CADH|CADF|HAND|VETH|VETF)$
```

### Full Names (French)
```regex
(Senior\s+(Homme|Femme|Dame)|Veteran\s+[1-4]|Ainée\s+[1-3]|Espoir(\s+(Homme|Femme))?|Junior(\s+(Homme|Femme))?|Cadet(\s+(Homme|Femme))?|Master\s+\d{2}\+?|Women\s+\d{2}\+?)
```

## Common Header Variations

### Category Column Headers
- `Cat.`
- `Cat`
- `Catég.`
- `Catégorie`
- `Category`
- `Categ`
- `Cat°`

### Position by Category Headers
- `Pl./C.`
- `Pl./Cat.`
- `Clas. Cat`
- `Pl. Cat`
- `Pos.Cat`
- `Classement Cat`
- `Cl.Cat`
- `Pos/Cat`

### Sex Column Headers
- `Sexe`
- `Sex`
- `S.`
- `S`
- `Genre`

### Position by Sex Headers
- `Pl./S.`
- `Pl. Sexe`
- `Clas.Sexe`
- `Pos.Sexe`
- `Classement Sexe`
- `Cl.S`
- `Pos/Sexe`

## Examples from Real Files

### CJPL Format (Cross Country)
```
Pl. | Dos | Nom           | Cat. | Pl./C. | Sexe | Pl./S. | Club      | Temps
1   | 123 | DUPONT Jean   | SH   | 1      | M    | 1      | AC Hannut | 35:25
2   | 456 | MARTIN Anne   | SD   | 1      | F    | 1      | RC Liège  | 37:12
15  | 789 | BERNARD Paul  | V1   | 5      | M    | 12     | No Team   | 42:30
```

### Grand Challenge Format
```
Pl. | Nom           | Cat.    | Temps    | Club      | Vitesse
1   | DUPONT Jean   | SENH    | 00:35:25 | AC Hannut | 16.95
2   | MARTIN Anne   | SENF    | 00:37:12 | RC Liège  | 16.14
20  | LEGRAND Marc  | VET1    | 00:42:30 | No Team   | 14.12
```

### Standard Format (Simple)
```
Pl. | Dos | Nom           | Sexe | Cat | Temps    | Vitesse
1   | 123 | DUPONT Jean   | M    | SH  | 00:35:25 | 16.95
2   | 456 | MARTIN Anne   | F    | SD  | 00:37:12 | 16.14
```

## Category Normalization Rules

### Priority Order for Detection
1. **Exact Match** - Direct match from list above
2. **Case Insensitive** - Match ignoring case
3. **Remove Spaces** - "SEN H" → "SENH"
4. **Remove Accents** - "AINÉE" → "AINEE"
5. **Abbreviation Expansion** - Check full names

### Ambiguous Cases
- `V` alone → Could be Veteran (need context)
- `D` alone → Could be Dame or D1 (need position/age)
- `A` alone → Could be Ainée (need context)
- Numbers alone → Not a category (likely position)

### Invalid Patterns
- Single digit: `1`, `2`, `3` → Position numbers
- Large numbers: `123`, `456` → Bib numbers
- Time patterns: `35:25`, `1:23:45` → Race times
- Decimal numbers: `16.95`, `14.12` → Speeds

## Implementation Guidelines

### Detection Logic
```csharp
1. Extract potential category from text
2. Clean: trim, uppercase, remove accents
3. Check against known patterns (shortest to longest):
   - Single char (M, F, H, D)
   - 2-char codes (SH, SD, V1, V2, D1, A1)
   - 3-char codes (ESP, JUN, CAD, VET, ESH)
   - 4-char codes (SENH, SENF, ESPH, ESPF)
   - 5+ char codes (AINÉE1, SENIOR H)
4. Validate: Not a number, not a time, not a speed
5. Store as-is (preserve original formatting)
```

### Storage Format
- Store **original text** as found in PDF/Excel
- No normalization in database
- Allows for exact reproduction
- Future queries can normalize for comparison

### Query/Analysis Format
- Normalize for grouping/comparison:
  - `SH`, `SENH`, `Senior H` → Group as "Senior Homme"
  - `V1`, `VET1`, `Veteran 1` → Group as "Veteran 1"
  - Case-insensitive comparison

## Testing Data

### Test Cases
```
Valid Categories:
  SH, SD, V1, V2, V3, D1, A1, ESP, JUN
  SENH, SENF, VET1, VET2, AINÉE1
  Senior H, Veteran 1, Master 40+

Invalid (Should Reject):
  1, 12, 123 (positions)
  35:25, 1:23:45 (times)
  16.95, 14.12 (speeds)
  M 123, F 456 (with numbers)
```

### Edge Cases
```
Ambiguous:
  "V" → Check context (likely Veteran if after name)
  "D" → Check context (likely Dame if position 1-2)
  "M 1" → M is sex, 1 is position

Complex:
  "V1 H" → V1 is category, H is sex
  "SEN H 35" → SEN H is category, 35 is position
  "ESPOIR G" → ESPOIR is category, G is sex
```

## Regional Variations

### Wallonia (Most Common)
- SH/SD (Senior)
- V1/V2/V3 (Veteran)
- A1/A2/A3 or D1/D2/D3 (Ainée/Dame)

### Brussels
- Similar to Wallonia
- Sometimes uses full names

### Flanders
- More likely to use Master system (M35, M40, etc.)
- Less common in this dataset

## Future Enhancements

1. **Age Validation**
   - Cross-reference category with actual age
   - Flag inconsistencies

2. **Automatic Normalization**
   - Map all variations to standard codes
   - UI option to show normalized or original

3. **Category Statistics**
   - Count participants per category
   - Age distribution analysis

4. **Missing Category Detection**
   - Flag entries without category
   - Suggest category based on age/performance

---

**Version:** 1.0  
**Date:** 2026-02-01  
**Sources:** 22 Courses Excel files + 11 PDF files analyzed  
**Total Categories:** 50+ unique codes identified
