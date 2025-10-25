using Compression_Worker.Core.Models;
using Compression_Worker.Data;
using Compression_Worker.Processors.Base;

namespace Compression_Worker.Processors.Lemmatization;

/// <summary>
/// Processor that reduces Romanian words to their base form (lemma/stem).
/// Implements reversible compression by storing the original inflected forms.
/// Eliminates morphological redundancy specific to Romanian's rich inflection system.
///
/// Supports two stemming modes:
/// - Fast: Quick processing without wordlist validation
/// - Accurate: Validates stems against Romanian dictionary (default)
/// </summary>
public class RomanianLemmatizer : ReversibleProcessorBase
{
    private const string MetadataKey = "lemma_mapping";
    private readonly SnowballRules _stemmer;

    /// <summary>
    /// Creates a new Romanian lemmatizer with specified stemming mode.
    /// </summary>
    /// <param name="mode">Stemming mode (Fast or Accurate, default: Accurate)</param>
    public RomanianLemmatizer(StemmingMode mode = StemmingMode.Accurate)
    {
        _stemmer = SnowballRules.CreateRomanianStemmer(mode);
    }

    /// <summary>
    /// Gets the stemming mode used by this lemmatizer.
    /// </summary>
    public StemmingMode Mode => _stemmer.Mode;

    protected override ProcessingContext ProcessCore(ProcessingContext context)
    {
        var tokens = context.Tokens;
        var lemmaMapping = new Dictionary<int, string>();

        // Apply stemming and track original forms
        for (int i = 0; i < tokens.Count; i++)
        {
            string original = tokens[i];

            // Only stem words (not punctuation)
            if (IsWord(original))
            {
                string lemma = _stemmer.Stem(original);

                // Only store mapping if word was actually modified
                if (!string.Equals(lemma, original, StringComparison.OrdinalIgnoreCase))
                {
                    lemmaMapping[i] = original;
                    tokens[i] = lemma;
                }
            }
        }

        context.Tokens = tokens;
        context.SetMetadata(MetadataKey, lemmaMapping);

        return context;
    }

    protected override ProcessingContext ReverseCore(ProcessingContext context)
    {
        var lemmaMapping = context.GetMetadata<Dictionary<int, string>>(MetadataKey);

        if (lemmaMapping == null || lemmaMapping.Count == 0)
            return context; // Nothing to restore

        // Restore original inflected forms
        foreach (var kvp in lemmaMapping)
        {
            if (kvp.Key < context.Tokens.Count)
            {
                context.Tokens[kvp.Key] = kvp.Value;
            }
        }

        return context;
    }

    /// <summary>
    /// Determines if a token is a word (as opposed to punctuation).
    /// </summary>
    private static bool IsWord(string token)
    {
        return !string.IsNullOrWhiteSpace(token) &&
               token.Length > 0 &&
               char.IsLetter(token[0]);
    }
}
