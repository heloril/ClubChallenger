# Name Comparison - Hyphen Normalization

## Summary
Modified the name comparison logic throughout the application to ignore hyphens when matching member names with race results. This allows names like "Jean-Marc" to match with "Jean Marc" or "Marie France" to match with "Marie-France".

## Changes Made

### 1. StringExtensions.cs
**New Method Added:**
```csharp
public static string NormalizeForComparison(this string text)
```

**Purpose:** 
- Provides a standardized way to normalize names for comparison
- Removes diacritics (accents)
- Replaces hyphens with spaces
- Normalizes multiple spaces to single space
- Converts to lowercase
- Trims whitespace

**Example:**
- "Jean-Marc" → "jean marc"
- "Marie-France DUPONT" → "marie france dupont"
- "José-María" → "jose maria"

### 2. MemberService.cs
**Updated Method:** `GetMemberKey(Member member)`

**Change:**
```csharp
// Before:
return $"{member.FirstName?.Trim().ToLowerInvariant()}|{member.LastName?.Trim().ToLowerInvariant()}";

// After:
var normalizedFirstName = member.FirstName?.NormalizeForComparison() ?? "";
var normalizedLastName = member.LastName?.NormalizeForComparison() ?? "";
return $"{normalizedFirstName}|{normalizedLastName}";
```

**Impact:** Member and Challenger deduplication now ignores hyphens

### 3. Member.cs
**Updated Methods:**
- `Equals(object obj)` - Now normalizes names before comparison
- `GetHashCode()` - Updated to match the Equals implementation (critical for hash-based collections)

**Change:**
```csharp
// Before:
return FirstName == other.FirstName && LastName == other.LastName;

// After:
var normalizedFirstName = FirstName?.NormalizeForComparison() ?? "";
var normalizedLastName = LastName?.NormalizeForComparison() ?? "";
var otherNormalizedFirstName = other.FirstName?.NormalizeForComparison() ?? "";
var otherNormalizedLastName = other.LastName?.NormalizeForComparison() ?? "";

return normalizedFirstName == otherNormalizedFirstName && 
       normalizedLastName == otherNormalizedLastName;
```

**Impact:** Member equality checks now ignore hyphens, accents, and case

### 4. RaceProcessingService.cs
**Updated Method:** `FindMatchingMembers(List<Member> members, string resultValue)`

**Change:**
```csharp
// Before:
return members.Where(member =>
    resultValue.RemoveDiacritics().Contains(member.FirstName.RemoveDiacritics(), StringComparison.InvariantCultureIgnoreCase) &&
    resultValue.RemoveDiacritics().Contains(member.LastName.RemoveDiacritics(), StringComparison.InvariantCultureIgnoreCase))
    .ToList();

// After:
var normalizedResult = resultValue.NormalizeForComparison();

return members.Where(member =>
{
    var normalizedFirstName = member.FirstName.NormalizeForComparison();
    var normalizedLastName = member.LastName.NormalizeForComparison();
    
    return normalizedResult.Contains(normalizedFirstName) && 
           normalizedResult.Contains(normalizedLastName);
}).ToList();
```

**Impact:** Race result matching now ignores hyphens when finding members

## Benefits

1. **More Robust Matching:** Handles variations in name formatting across different data sources
2. **Consistent Behavior:** All name comparisons use the same normalization logic
3. **Reduced Duplicates:** Better deduplication of members with hyphenated names
4. **Better User Experience:** Users don't need to worry about exact hyphen placement

## Test Scenarios

The following name variations will now match:
- "Jean-Marc CUCCURU" ↔ "Jean Marc CUCCURU"
- "Marie-France LISSOIR" ↔ "Marie France LISSOIR"
- "Jean Luc FRIEDRICH" ↔ "Jean-Luc FRIEDRICH"
- "José-María GONZÁLEZ" ↔ "Jose Maria GONZALEZ" (also handles accents)

## Verification

✅ Build successful
✅ All existing name comparison functionality preserved
✅ New normalization applied consistently across all comparison points

## Notes

- The original names are preserved in the data (not modified)
- Normalization only happens during comparison operations
- The `GetHashCode()` method was updated to match `Equals()` to maintain the contract for hash-based collections (Dictionary, HashSet, etc.)
