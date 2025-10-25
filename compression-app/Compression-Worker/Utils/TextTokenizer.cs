using System.Text.RegularExpressions;

namespace Compression_Worker.Utils;

/// <summary>
/// Utility class for intelligent text tokenization.
/// Preserves punctuation and spacing information for reversibility.
/// </summary>
public static class TextTokenizer
{
    private static readonly Regex TokenRegex = new(
        @"(\w+|[^\w\s]|\s+)",
        RegexOptions.Compiled
    );

    /// <summary>
    /// Tokenizes text into words, punctuation marks, and whitespace.
    /// Preserves all information needed for exact reconstruction.
    /// </summary>
    public static List<string> Tokenize(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new List<string>();

        var matches = TokenRegex.Matches(text);
        var tokens = new List<string>(matches.Count);

        foreach (Match match in matches)
        {
            tokens.Add(match.Value);
        }

        return tokens;
    }

    /// <summary>
    /// Reconstructs text from tokens.
    /// </summary>
    public static string Detokenize(List<string> tokens)
    {
        if (tokens == null || tokens.Count == 0)
            return string.Empty;

        return string.Concat(tokens);
    }

    /// <summary>
    /// Checks if a token is a word (not punctuation or whitespace).
    /// </summary>
    public static bool IsWord(string token)
    {
        return !string.IsNullOrWhiteSpace(token) &&
               token.Length > 0 &&
               char.IsLetterOrDigit(token[0]);
    }
}
