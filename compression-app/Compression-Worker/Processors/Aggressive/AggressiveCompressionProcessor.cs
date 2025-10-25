using Compression_Worker.Core.Models;
using Compression_Worker.Processors.Base;
using System.Text.RegularExpressions;

namespace Compression_Worker.Processors.Aggressive;

/// <summary>
/// Aggressive compression processor that removes non-essential words.
/// Target: >40% text reduction by keeping only nouns and verbs.
/// Trade-off: Less readable but maintains core semantic meaning.
/// </summary>
public class AggressiveCompressionProcessor : ReversibleProcessorBase
{
    private const string MetadataKey = "aggressive_removed";

    // Romanian verb endings (present, past, future)
    private static readonly HashSet<string> VerbEndings = new(StringComparer.OrdinalIgnoreCase)
    {
        "ez", "ezi", "ează", "ăm", "ați", "eaz",
        "esc", "ești", "este", "im", "iți", "esc",
        "am", "ai", "ă", "ăm", "ați", "au",
        "at", "ut", "it", "ât",
        "a", "ea", "i", "ia", "e"
    };

    // Romanian noun endings (singular/plural, masculine/feminine)
    private static readonly HashSet<string> NounEndings = new(StringComparer.OrdinalIgnoreCase)
    {
        "ul", "ua", "le", "lor", "uri", "uri",
        "ie", "ție", "are", "tor", "toare", "ist", "ism"
    };

    protected override ProcessingContext ProcessCore(ProcessingContext context)
    {
        var tokens = context.Tokens;
        var removedTokens = new Dictionary<int, string>();

        for (int i = 0; i < tokens.Count; i++)
        {
            var token = tokens[i];

            if (!IsWord(token))
                continue;

            // Keep only nouns and verbs (aggressive filtering)
            if (!IsNounOrVerb(token))
            {
                removedTokens[i] = token;

                // Remove following whitespace
                if (i + 1 < tokens.Count && string.IsNullOrWhiteSpace(tokens[i + 1]))
                {
                    removedTokens[i + 1] = tokens[i + 1];
                }
            }
        }

        // Remove non-essential tokens
        context.Tokens = tokens
            .Select((token, index) => removedTokens.ContainsKey(index) ? null : token)
            .Where(token => token != null)
            .ToList()!;

        // Remove consecutive duplicate words (n-gram deduplication)
        var deduplicated = new List<string>();
        string lastWord = "";
        foreach (var token in context.Tokens)
        {
            if (IsWord(token))
            {
                if (token.ToLowerInvariant() != lastWord.ToLowerInvariant())
                {
                    deduplicated.Add(token);
                    lastWord = token;
                }
            }
            else
            {
                deduplicated.Add(token);
            }
        }
        context.Tokens = deduplicated;

        // Store metadata only if not lossy
        if (!context.Lossy)
        {
            context.SetMetadata(MetadataKey, removedTokens);
        }

        return context;
    }

    protected override ProcessingContext ReverseCore(ProcessingContext context)
    {
        // Aggressive compression is lossy - can't fully reverse
        // Just return as-is (output will be compressed form)
        return context;
    }

    private static bool IsWord(string token)
    {
        return !string.IsNullOrWhiteSpace(token) &&
               token.Length > 0 &&
               char.IsLetter(token[0]);
    }

    private static bool IsNounOrVerb(string word)
    {
        if (word.Length < 3)
            return false; // Too short to determine

        var lower = word.ToLowerInvariant();

        // Check if it ends with verb or noun suffixes
        foreach (var ending in VerbEndings)
        {
            if (lower.EndsWith(ending) && lower.Length > ending.Length + 2)
                return true;
        }

        foreach (var ending in NounEndings)
        {
            if (lower.EndsWith(ending) && lower.Length > ending.Length + 2)
                return true;
        }

        // If word is long enough (>5 chars), likely a noun/verb
        if (word.Length >= 5)
            return true;

        return false;
    }
}
