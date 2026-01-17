namespace OmniNet.SourceGenerators.Core.Tests;

internal static class HelperExtensions
{
    public static string UnifyCodeLineEndings(this string input)
    {
        var lines = input.Split(["\r\n", "\r", "\n"], StringSplitOptions.None | StringSplitOptions.TrimEntries);
        return string.Join("\n", lines);
    }
}