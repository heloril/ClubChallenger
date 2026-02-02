# Database Migration Guide - Classification Columns

## ✅ Migration Status: READY TO APPLY

The database migration has been prepared and the code has been updated to **automatically apply migrations when the application starts**.

## What Will Be Applied

### New Columns (4)
- `Sex` - NVARCHAR(1) NULL - Gender classification (M/F)
- `PositionBySex` - INT NULL - Rank within gender
- `AgeCategory` - NVARCHAR(50) NULL - Age category code
- `PositionByCategory` - INT NULL - Rank within age category

### New Indexes (2)
- `IX_Classifications_Sex_PositionBySex` - Performance index for gender queries
- `IX_Classifications_AgeCategory_PositionByCategory` - Performance index for category queries

---

## 🚀 How to Apply Migration

### ✨ Option 1: Automatic (RECOMMENDED) ⭐

**The migration will be applied automatically when you start your application!**

**Steps:**
1. **Just run your application** - That's it!
   - The `RaceManagementContext` constructor automatically calls `ApplyCustomMigrations()`
   - It checks if columns exist and adds them if missing
   - Safe to run multiple times (idempotent)
   - No manual intervention needed

2. **Verify it worked:**
   - Check the **Debug Output** window for migration messages
   - Or process a race and see the new columns in the UI

**That's all you need to do!** ✅

---

## ✅ Verification

### Quick Check - After Starting Application

**Watch the Debug Output window for:**
```
Sex column added
PositionBySex column added  
AgeCategory column added
PositionByCategory column added
Index IX_Classifications_Sex_PositionBySex created
Index IX_Classifications_AgeCategory_PositionByCategory created
```

### SQL Verification (Optional)

Run this query to verify all columns exist:

```sql
SELECT 
    c.name AS ColumnName,
    t.name AS DataType,
    c.max_length AS MaxLength,
    c.is_nullable AS IsNullable
FROM sys.columns c
INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
WHERE c.object_id = OBJECT_ID(N'[dbo].[Classifications]')
AND c.name IN ('Sex', 'PositionBySex', 'AgeCategory', 'PositionByCategory')
ORDER BY c.column_id;
```

**Expected:** 4 rows returned ✅

### UI Verification

1. Process a race with category columns in the PDF
2. View Race Classification tab
3. New columns should be visible:
   - Sex (M/F)
   - Pos/Sex
   - Category
   - Pos/Cat

---

## 🔧 Troubleshooting

### No migration messages in Debug Output?
- **Normal!** Means columns already exist
- They were added on first run
- Application is working correctly

### Columns not appearing in UI?
1. Check Debug output for errors
2. Rebuild solution
3. Restart application
4. Process a new race

### Migration errors in Debug output?
- Application will continue to work
- Try restarting application
- Check database permissions
- Contact support if persists

---

## 📊 Next Steps

After the automatic migration completes:

1. ✅ **Process a test race** with category data
2. ✅ **Verify new columns** appear in UI  
3. ✅ **Test exports** include new data
4. ✅ **Enjoy the new features!** 🎉

---

## 💡 FAQ

**Q: Do I need to do anything special?**  
A: No! Just run your app. Migration is automatic.

**Q: Will this affect my existing data?**  
A: No. Existing data is safe. New columns will be NULL for old records.

**Q: Can I run my app multiple times?**  
A: Yes! Migration checks before adding columns. Safe to run many times.

**Q: What if I don't have category data in PDFs?**  
A: Columns will be NULL. Application works exactly as before.

---

## ✅ Summary

### What Happens Automatically

When you start your application:
1. ✅ RaceManagementContext initializes
2. ✅ ApplyCustomMigrations() is called
3. ✅ Checks if columns exist
4. ✅ Adds missing columns
5. ✅ Creates indexes
6. ✅ Application starts normally

### Your Checklist

- [x] Code updated ✅
- [x] Build successful ✅  
- [ ] **→ Run application** (Migration happens automatically)
- [ ] Verify in Debug output
- [ ] Process a test race
- [ ] See new columns in UI

---

**Status:** ✅ Ready  
**Method:** Automatic  
**Action Required:** Just run your app!  
**Duration:** < 1 second
