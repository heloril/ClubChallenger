using System;
using System.Globalization;
using System.Text;

public static class StringExtensions
{
    public static bool Contains(this string source, string toCheck, StringComparison comp)
    {
        return source != null && RemoveDiacritics(source).IndexOf(RemoveDiacritics(toCheck), comp) >= 0;
    }

    public static string RemoveDiacritics(this string text)
    {
        if (text == null) return null;
        string str = text.Normalize(NormalizationForm.FormD);
        StringBuilder stringBuilder = new StringBuilder();
        foreach (char ch in str)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                stringBuilder.Append(ch);
        }
        return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
    }

    /// <summary>
    /// Normalizes a name for comparison by removing diacritics and hyphens, and converting to lowercase.
    /// Used for matching names like "Jean-Marc" with "Jean Marc".
    /// </summary>
    public static string NormalizeForComparison(this string text)
    {
        if (text == null) return null;

        // Remove diacritics first
        string normalized = RemoveDiacritics(text);

        // Remove hyphens (replace with space to keep word separation)
        normalized = normalized.Replace("-", " ");

        // Normalize whitespace (replace multiple spaces with single space)
        normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"\s+", " ");

        return normalized.Trim().ToLowerInvariant();
    }
}