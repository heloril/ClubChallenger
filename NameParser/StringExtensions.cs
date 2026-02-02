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
}