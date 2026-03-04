using System;
using System.Linq;
using System.Text.RegularExpressions;

public static class Utils
{
    public static string MakeSnippet(string text, string q)
    {
        var keys = InMemorySearchEngine.Tokenize(q);
        if (!keys.Any()) return System.Net.WebUtility.HtmlEncode(text.Length > 200 ? text[..200] + "..." : text);

        
        var lower = text.ToLowerInvariant();
        var first = keys.FirstOrDefault(k => lower.Contains(k)) ?? keys[0];
        var idx = lower.IndexOf(first, StringComparison.Ordinal);
        int start = idx >= 0 ? Math.Max(0, idx - 80) : 0;
        int len = Math.Min(200, text.Length - start);
        var snip = text.Substring(start, len).Trim();
        var raw = (start > 0 ? "..." : "") + snip + (start + len < text.Length ? "..." : "");

        
        var encoded = System.Net.WebUtility.HtmlEncode(raw);
        var pattern = string.Join("|", keys.Select(k => Regex.Escape(System.Net.WebUtility.HtmlEncode(k))));
        encoded = Regex.Replace(encoded, $"({pattern})", "<mark>$1</mark>", RegexOptions.IgnoreCase);
        return encoded;
    }
}
