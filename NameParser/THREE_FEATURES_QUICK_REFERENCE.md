# Quick Reference: Three New Features

## 🎯 Feature Summary

| Feature | What It Does | UI Location |
|---------|--------------|-------------|
| **Member Filter** | Filter results by member name/email | Race & General Classification tabs |
| **Hors Challenge** | Create races without a year | Upload & Process tab |
| **Multiple Distances** | Multiple distances per race number | Automatic (backend) |

---

## 1️⃣ Member Filtering

### Where to Find It
- **Race Classification Tab:** Above the results grid
- **General Classification Tab:** Below year selection

### How to Use
```
1. Type member name (first, last, or email) in filter box
2. Click "Apply Filter" or press Enter
3. Results update to show only matching members
4. Click "Clear" to show all results again
```

### Examples
- Filter: `"John"` → Shows: John Doe, John Smith, Mary Johnson
- Filter: `"@gmail"` → Shows: All members with Gmail addresses
- Filter: `"Smith"` → Shows: All Smiths (first or last name)

---

## 2️⃣ Hors Challenge Races

### Where to Find It
- **Upload & Process Tab:** In Race Details section

### How to Use
```
1. Check ☑ "Hors Challenge (no year)"
2. Year field becomes disabled (grayed out)
3. Fill in other race details (Name, Number, Distance)
4. Process race normally
```

### When to Use
- Training runs outside regular challenge
- Special events not part of yearly series
- Test races or unofficial events

### Database Impact
- Year saved as `NULL`
- `IsHorsChallenge` flag set to `true`
- Race appears in list with blank year

---

## 3️⃣ Multiple Distances Per Race

### How It Works
- **Same race number** + **same year** = OK if different names
- **Example:** Race #1, 2024 can have 5km and 10km variants

### What's Unique
- **Old Rule:** Race Number + Year must be unique
- **New Rule:** Race Name + Year must be unique

### Examples

✅ **ALLOWED:**
```
Race #1, 2024, 5km,  "Marathon 5K"
Race #1, 2024, 10km, "Marathon 10K"
Race #1, 2024, 21km, "Marathon Half"
```

❌ **NOT ALLOWED:**
```
Race #1, 2024, 5km, "Marathon 5K"
Race #2, 2024, 5km, "Marathon 5K"  ← Same name (duplicate)
```

---

## 📋 Quick Start Guide

### Setting Up (First Time)

1. **Run Database Migration:**
   ```sql
   -- File: Infrastructure\Data\Migrations\UpdateRaceConstraints.sql
   -- Run this on your database (SQL Server)
   ```

2. **Restart Application:**
   ```
   - Close and reopen Race Management System
   - New features will be available
   ```

### Creating a Hors Challenge Race

```
Step 1: Upload & Process tab
Step 2: Browse and select race file
Step 3: Enter race name
Step 4: ☑ Check "Hors Challenge (no year)"
Step 5: Enter race number and distance
Step 6: Click "Process Race"
```

### Creating Multiple Distance Variants

```
Race 1 (5km):
- Name: "City Run 5K"
- Year: 2024
- Race #: 1
- Distance: 5

Race 2 (10km):
- Name: "City Run 10K"  ← Different name!
- Year: 2024
- Race #: 1  ← Same race number OK!
- Distance: 10
```

### Filtering Members

```
Race Classification Tab:
1. Select a processed race from list
2. Enter name in "Filter by Member" box
3. Click "Apply Filter"
4. View filtered results

General Classification Tab:
1. Select year
2. Load classification
3. Enter name in filter box
4. Click "Apply Filter"
5. View filtered results
```

---

## 🔧 Troubleshooting

### Issue: "Hors Challenge" checkbox not visible
**Solution:** Make sure you've rebuilt the application after pulling latest code

### Issue: Can't create race with same race number
**Solution:** Check that the race NAMES are different (names must be unique)

### Issue: Year field won't disable
**Solution:** Make sure "Hors Challenge" checkbox is actually checked

### Issue: Filter not working
**Solution:** Click "Apply Filter" button after typing (or press Enter)

### Issue: Database error when saving race
**Solution:** Run the migration script first (UpdateRaceConstraints.sql)

---

## 💡 Tips & Tricks

### Member Filtering
- **Partial matching works:** Type "Joh" to find "John" or "Johnson"
- **Case insensitive:** "john" finds "John" and "JOHN"
- **Search email too:** "@gmail" finds all Gmail users
- **Clear quickly:** Use "Clear" button instead of deleting text

### Hors Challenge
- **Use for training:** Perfect for tracking practice runs
- **No year restrictions:** Create events any time
- **Still tracked:** All statistics still calculated
- **Easy to spot:** Blank year in race list

### Multiple Distances
- **Keep names clear:** Use distance in name (e.g., "Marathon 5K", "Marathon 10K")
- **Same event:** Use same race number for variants of same event
- **Different events:** Use different race numbers for unrelated races

---

## 📊 UI Quick Reference

### Race Details Form
```
┌─ Race Details ────────────────────────┐
│ Race Name: [__Spring Marathon_____]   │
│ ☑ Hors Challenge (no year)            │ ← NEW
│ Year: [2024 ▼] (disabled)             │ ← Auto-disabled
│ Race Number: [_1_]                     │
│ Distance (km): [10]                    │
└────────────────────────────────────────┘
```

### Race Classification (with filter)
```
┌─ Race Results ─────────────────────────────┐
│ Filter: [_John__] [Apply] [Clear]          │ ← NEW
│ ───────────────────────────────────────── │
│ Rank │ Name      │ Points │ Time   │ ...  │
│  1   │ John Doe  │  950   │ 35:20  │      │
│  5   │ John Smith│  880   │ 38:45  │      │
└────────────────────────────────────────────┘
```

### Races List (with hors challenge)
```
Year    │ Race # │ Name           │ Distance
────────┼────────┼────────────────┼─────────
2024    │   1    │ Marathon 5K    │   5
2024    │   1    │ Marathon 10K   │  10    ← Same race #
2024    │   2    │ Spring Run     │   5
(blank) │   99   │ Training Run   │  10    ← Hors challenge
```

---

## 🎯 Key Takeaways

### Remember
1. **Filter** = Search by name or email in results
2. **Hors Challenge** = Race without a year (NULL year)
3. **Multiple Distances** = Same race # OK if different names

### Database Changes
- `Year` column now **nullable**
- `IsHorsChallenge` flag **added**
- Unique constraint changed from **(RaceNumber, Year)** to **(Name, Year)**

### Backwards Compatibility
- ✅ Existing races still work
- ✅ Existing year selection still works
- ✅ All existing features unchanged
- ⚠️ Must run migration script first!

---

## 📞 Need Help?

### Common Questions

**Q: Do I need to migrate my database?**
A: Yes, run `UpdateRaceConstraints.sql` before using new features.

**Q: Can I convert an existing race to hors challenge?**
A: Not from UI, but you can update directly in database (set Year = NULL, IsHorsChallenge = true).

**Q: What happens if I create races with same name?**
A: Database will reject it (name must be unique per year).

**Q: Can hors challenge races have a race number?**
A: Yes, race number is still required for organization.

**Q: Does filtering work on hors challenge races?**
A: Yes, member filtering works on all races.

---

## ✅ Checklist: First Use

- [ ] Database migration script executed
- [ ] Application rebuilt
- [ ] Application restarted
- [ ] Test: Create regular race (with year)
- [ ] Test: Create hors challenge race (no year)
- [ ] Test: Create two races with same race # but different names
- [ ] Test: Filter members in race classification
- [ ] Test: Filter members in general classification
- [ ] Verify: Existing races still load correctly

---

## 🎉 You're Ready!

All three features are now available:
- 🔍 **Filter members** in classifications
- 🏃 **Create hors challenge races** without years
- 📏 **Multiple distances** per race number

Enjoy your enhanced Race Management System!
