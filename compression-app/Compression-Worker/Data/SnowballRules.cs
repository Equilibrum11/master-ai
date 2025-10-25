namespace Compression_Worker.Data;

/// <summary>
/// Stemming modes for Romanian text processing.
/// </summary>
public enum StemmingMode
{
    /// <summary>
    /// Fast mode: No wordlist validation, uses only suffix rules.
    /// Best for: real-time processing, interactive applications.
    /// Performance: ~5ms first load, 0ms cached.
    /// </summary>
    Fast,

    /// <summary>
    /// Accurate mode: Validates stems against Romanian wordlist.
    /// Best for: batch processing, offline compression, maximum quality.
    /// Performance: ~500ms first load (wordlist), 0ms cached.
    /// </summary>
    Accurate
}

/// <summary>
/// Snowball stemming algorithm for Romanian language.
/// Uses suffix rules from Utils/Constants/sufix_list.txt (~165 rules).
/// Supports two modes: Fast (no validation) and Accurate (with wordlist validation).
///
/// NOTE: Suffix list in Utils/Constants/ is READ-ONLY. Do not modify.
/// </summary>
public class SnowballRules
{
    private readonly List<SuffixRule> _rules;
    private readonly HashSet<string>? _wordlist;
    private readonly StemmingMode _mode;

    private SnowballRules(StemmingMode mode = StemmingMode.Accurate)
    {
        _mode = mode;
        _rules = BuildRulesFromFile();

        // Load wordlist only in Accurate mode
        if (_mode == StemmingMode.Accurate)
        {
            _wordlist = RomanianResourceLoader.GetWordlist();
        }
    }

    /// <summary>
    /// Factory method to create a Romanian stemmer instance.
    /// </summary>
    /// <param name="mode">Stemming mode (Fast or Accurate)</param>
    public static SnowballRules CreateRomanianStemmer(StemmingMode mode = StemmingMode.Accurate)
    {
        return new SnowballRules(mode);
    }

    /// <summary>
    /// Stems a Romanian word by removing appropriate suffixes.
    /// In Accurate mode, validates that the resulting stem exists in the Romanian wordlist.
    /// Preserves minimum word length to avoid over-stemming.
    /// </summary>
    public string Stem(string word)
    {
        if (string.IsNullOrWhiteSpace(word) || word.Length < 4)
            return word; // Don't stem very short words

        string normalized = word.ToLowerInvariant();

        // Apply rules in order of suffix length (longest first for greedy matching)
        foreach (var rule in _rules.OrderByDescending(r => r.Suffix.Length))
        {
            if (normalized.EndsWith(rule.Suffix))
            {
                // Check minimum stem length after removal
                int stemLength = normalized.Length - rule.Suffix.Length;
                if (stemLength >= rule.MinStemLength)
                {
                    string stem = normalized.Substring(0, stemLength) + rule.Replacement;

                    // In Accurate mode, validate stem exists in dictionary
                    if (_mode == StemmingMode.Fast || IsValidStem(stem))
                    {
                        return stem;
                    }
                    // If stem invalid, continue trying other rules
                }
            }
        }

        return word; // Return original if no rule produced valid stem
    }

    /// <summary>
    /// Validates that a stem exists in the Romanian wordlist.
    /// Only used in Accurate mode.
    /// </summary>
    private bool IsValidStem(string stem)
    {
        if (_wordlist == null)
            return true; // No validation in Fast mode

        // Check if stem exists in dictionary
        return _wordlist.Contains(stem);
    }

    /// <summary>
    /// Builds suffix rules from Utils/Constants/sufix_list.txt.
    /// Replaces hardcoded rules with dynamic loading from file.
    /// </summary>
    private static List<SuffixRule> BuildRulesFromFile()
    {
        var suffixes = RomanianResourceLoader.GetSuffixes();
        var rules = new List<SuffixRule>();

        foreach (var suffix in suffixes)
        {
            if (!string.IsNullOrWhiteSpace(suffix))
            {
                // Default rule: remove suffix, no replacement, min stem length = 3
                rules.Add(new SuffixRule(suffix.Trim(), "", 3));
            }
        }

        return rules;
    }

    /// <summary>
    /// Gets the number of suffix rules loaded.
    /// </summary>
    public int RuleCount => _rules.Count;

    /// <summary>
    /// Gets the stemming mode used by this instance.
    /// </summary>
    public StemmingMode Mode => _mode;

    /// <summary>
    /// Represents a stemming rule with suffix, replacement, and minimum stem length.
    /// </summary>
    private record SuffixRule(string Suffix, string Replacement, int MinStemLength);
}
