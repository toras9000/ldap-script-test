#r "nuget: Lestaly, 0.58.0"
#nullable enable
using System.Text.RegularExpressions;
using Lestaly;

public static IEnumerable<string> EnumerateTextBlocks(this FileInfo self)
{
    var buffer = new StringBuilder();
    foreach (var line in self.ReadLines())
    {
        if (line.IsWhite())
        {
            if (0 < buffer.Length)
            {
                yield return buffer.ToString();
                buffer.Clear();
            }
            continue;
        }

        buffer.AppendLine(line);
    }

    if (0 < buffer.Length)
    {
        yield return buffer.ToString();
    }
}

public static class RegexResources
{
    public static Regex Space { get; } = new(@"\s+");
    public static Regex AttrIndex { get; } = new(@"^\{\d+\}");
}

public static string NormalizeSpace(this string self)
    => RegexResources.Space.Replace(self, m =>
    {
        if (m.Index == 0) return "";
        if (self.Length <= m.Index + m.Length) return "";
        return " ";
    });

public static string RemoveIndex(this string self)
    => RegexResources.AttrIndex.Replace(self, "");

