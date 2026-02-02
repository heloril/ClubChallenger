# Quick Reference: Export Features

## Export Options

### 1. Single Race Export (📧 Export for Email)
**When to use**: Export one race at a time  
**How to use**:
1. Select a race (single click)
2. Apply filter (optional)
3. Click "📧 Export for Email"
4. Choose HTML or Text format
5. Save and copy to email

**Output**: Single race with all filtered results

---

### 2. Multiple Races Export (📧 Export Multiple Races)
**When to use**: Export several races together  
**How to use**:
1. Select multiple races:
   - `Ctrl+Click` = Add individual races
   - `Shift+Click` = Select range
   - `Ctrl+A` = Select all
2. Apply filter (applies to ALL races)
3. Click "📧 Export Multiple Races"
4. Choose HTML or Text format
5. Save and copy to email

**Output**: All races in one document with summaries

---

## Filter Options

### 👥 All Participants
- Shows everyone who participated
- Useful for: Complete race reports, official results

### ✓ Members Only
- Shows only club members
- Winner always included (even if not a member)
- Useful for: Club newsletters, member rankings

### ○ Non-Members Only  
- Shows only non-members
- Winner always included (even if a member)
- Useful for: External participant lists, recruitment

---

## Format Comparison

| Feature | HTML | Text |
|---------|------|------|
| Styling | ✅ Colors, highlights | ❌ Plain |
| Email Clients | Most support | All support |
| File Size | Larger | Smaller |
| Print | Beautiful | Simple |
| Copy/Paste | Works well | Works everywhere |
| Best For | Newsletters | Plain text emails |

---

## Selection Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| `Click` | Select single race |
| `Ctrl+Click` | Add/remove race |
| `Shift+Click` | Select range |
| `Ctrl+A` | Select all |

---

## Quick Tips

### ✅ DO
- Sort races before selecting (click column headers)
- Apply filter before exporting
- Use HTML for newsletters
- Use Text for plain emails
- Test with small selection first

### ❌ DON'T
- Mix processed and unprocessed races
- Forget to apply filter
- Export too many races at once (limit ~50)
- Paste HTML into plain text email clients

---

## Common Workflows

### Weekly Newsletter
```
1. Select last week's races (Ctrl+Click)
2. Apply "Members Only" filter
3. Export Multiple Races (HTML)
4. Copy content to email
```

### Monthly Report
```
1. Select all month's races (Shift+Click range)
2. Apply "All Participants" filter
3. Export Multiple Races (HTML)
4. Include in report
```

### Quick Share
```
1. Select race (Click)
2. Apply "All Participants"
3. Export for Email (Text)
4. Share in chat
```

---

## Button Colors

| Button | Color | Meaning |
|--------|-------|---------|
| 📧 Export for Email | Green | Safe, primary action |
| 📧 Export Multiple | Blue | Secondary action |
| 🗑️ Delete Race | Red | Danger, destructive |

---

## Status Indicators

### Race Status
- ✅ **Processed** - Ready to export
- ❌ **Not Processed** - Cannot export

### Export Button
- ✅ **Enabled** - Ready to export
- ❌ **Disabled** - Selection invalid

---

## File Naming

### Single Race
```
Email_Race_2026_1_Marathon_Brussels.html
```
- Format: `Email_Race_{Year}_{Number}_{Name}.{ext}`

### Multiple Races
```
Email_Multiple_Races_5_races_20260201.html
```
- Format: `Email_Multiple_Races_{Count}_races_{Date}.{ext}`

---

## Email Client Compatibility

### HTML Format
| Client | Support | Notes |
|--------|---------|-------|
| Outlook | ✅ Excellent | Full styling |
| Gmail | ✅ Good | Some styles limited |
| Apple Mail | ✅ Excellent | Full styling |
| Thunderbird | ✅ Good | Most styles work |
| Yahoo | ⚠️ Limited | Basic styles only |

### Text Format
| Client | Support |
|--------|---------|
| All | ✅ Perfect |

---

## Troubleshooting

### Button Disabled?
**Check**: All selected races are "Processed"

### Selection Lost?
**Reason**: Changing filter clears selection

### Styling Not Working?
**Solution**: Use Text format or check email client

### Export Takes Long?
**Limit**: Keep selections under 50 races

---

## Support

- Documentation: `EXPORT_FOR_EMAIL_FEATURE.md`
- Multiple Export: `EXPORT_MULTIPLE_RACES_FEATURE.md`
- Issues: Contact development team

---

**Version**: 1.0  
**Last Updated**: 2026-02-01
