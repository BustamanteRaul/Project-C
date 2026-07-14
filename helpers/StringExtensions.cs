using System.Text.RegularExpressions;

namespace MySharpVenture;

public static class StringExtensions
{
    // Strips Spectre markup tags like [lime]...[/] from a string,
    // e.g. after a SelectionPrompt<string> returns a markup-styled choice.
    public static string RemoveMarkup(this string value)
    {
        return Regex.Replace(value, @"\[/?[^\]]*\]", string.Empty);
    }
}
