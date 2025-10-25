using System.Text;

namespace Compression_Worker.Data;

/// <summary>
/// Centralized loader for Romanian linguistic resources.
/// Implements lazy loading with caching for performance.
///
/// IMPORTANT: Resources in Utils/Constants/ are READ-ONLY.
/// Never modify existing resource files.
/// </summary>
public static class RomanianResourceLoader
{
    private static readonly string ResourcesBasePath =
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Utils", "Constants");

    // Lazy loading - resources are loaded only on first access and then cached
    private static readonly Lazy<List<string>> _suffixes = new(() => LoadLines("sufix_list.txt"));
    private static readonly Lazy<List<string>> _prefixes = new(() => LoadLines("prefix_list.txt"));
    private static readonly Lazy<HashSet<string>> _wordlist = new(() => new HashSet<string>(
        LoadLines("wordlist.txt"),
        StringComparer.OrdinalIgnoreCase
    ));
    private static readonly Lazy<Dictionary<string, SentiWord>> _sentiWords = new(LoadSentiWordNet);

    /// <summary>
    /// Gets Romanian suffixes list (~165 items).
    /// First call loads from file (~5ms), subsequent calls are instant (cached).
    /// </summary>
    public static List<string> GetSuffixes() => _suffixes.Value;

    /// <summary>
    /// Gets Romanian prefixes list (~120 items).
    /// First call loads from file (~5ms), subsequent calls are instant (cached).
    /// </summary>
    public static List<string> GetPrefixes() => _prefixes.Value;

    /// <summary>
    /// Gets Romanian wordlist dictionary (~200K words, 30MB memory).
    /// First call loads from file (~500ms), subsequent calls are instant (cached).
    /// Uses case-insensitive comparison.
    /// </summary>
    public static HashSet<string> GetWordlist() => _wordlist.Value;

    /// <summary>
    /// Gets SentiWordNet Romanian sentiment lexicon (~100K entries, 50MB memory).
    /// First call loads from file (~2000ms), subsequent calls are instant (cached).
    /// </summary>
    public static Dictionary<string, SentiWord> GetSentiWords() => _sentiWords.Value;

    /// <summary>
    /// Helper method to load text file lines (trims whitespace, filters empty lines).
    /// Uses FileStream with FileShare.Read to avoid access conflicts.
    /// </summary>
    private static List<string> LoadLines(string filename)
    {
        var path = GetResourcePath(filename);

        if (!File.Exists(path))
        {
            throw new FileNotFoundException(
                $"Resource file not found: {filename}\n" +
                $"Expected location: {path}\n" +
                $"Make sure Utils/Constants/{filename} exists.");
        }

        try
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var reader = new StreamReader(stream, Encoding.UTF8);

            var lines = new List<string>();
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                var trimmed = line.Trim();
                if (!string.IsNullOrWhiteSpace(trimmed))
                {
                    lines.Add(trimmed);
                }
            }
            return lines;
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new UnauthorizedAccessException(
                $"Access denied reading resource file: {filename}\n" +
                $"Path: {path}\n" +
                $"Ensure the file has read permissions.\n" +
                $"Original error: {ex.Message}", ex);
        }
        catch (IOException ex)
        {
            throw new IOException(
                $"I/O error reading resource file: {filename}\n" +
                $"Path: {path}\n" +
                $"Original error: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Loads and parses SentiWordNet format.
    /// Format: word\tindex\tobj_score\tpos_score\tneg_score\tdefinition
    /// </summary>
    private static Dictionary<string, SentiWord> LoadSentiWordNet()
    {
        var result = new Dictionary<string, SentiWord>(StringComparer.OrdinalIgnoreCase);
        var lines = LoadLines("SentiWordNet_3.0.0_RO.txt");

        foreach (var line in lines)
        {
            var parts = line.Split('\t');
            if (parts.Length >= 5)
            {
                try
                {
                    var word = parts[0].Trim();
                    var objScore = double.Parse(parts[2], System.Globalization.CultureInfo.InvariantCulture);
                    var posScore = double.Parse(parts[3], System.Globalization.CultureInfo.InvariantCulture);
                    var negScore = double.Parse(parts[4], System.Globalization.CultureInfo.InvariantCulture);

                    // Take first occurrence of word (most common meaning)
                    if (!result.ContainsKey(word))
                    {
                        result[word] = new SentiWord(word, posScore, negScore, objScore);
                    }
                }
                catch
                {
                    // Skip malformed lines
                    continue;
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Constructs full path to resource file in Utils/Constants/.
    /// </summary>
    private static string GetResourcePath(string filename)
    {
        return Path.Combine(ResourcesBasePath, filename);
    }

    /// <summary>
    /// Gets loading statistics for diagnostics.
    /// </summary>
    public static ResourceLoadingStats GetLoadingStats()
    {
        return new ResourceLoadingStats
        {
            SuffixesLoaded = _suffixes.IsValueCreated,
            PrefixesLoaded = _prefixes.IsValueCreated,
            WordlistLoaded = _wordlist.IsValueCreated,
            SentiWordNetLoaded = _sentiWords.IsValueCreated,
            SuffixCount = _suffixes.IsValueCreated ? _suffixes.Value.Count : 0,
            PrefixCount = _prefixes.IsValueCreated ? _prefixes.Value.Count : 0,
            WordlistCount = _wordlist.IsValueCreated ? _wordlist.Value.Count : 0,
            SentiWordCount = _sentiWords.IsValueCreated ? _sentiWords.Value.Count : 0
        };
    }
}

/// <summary>
/// Represents a word with sentiment scores from SentiWordNet.
/// </summary>
public record SentiWord(string Word, double Positive, double Negative, double Objective)
{
    /// <summary>
    /// Gets the dominant sentiment (Positive, Negative, or Neutral).
    /// </summary>
    public string DominantSentiment
    {
        get
        {
            if (Objective > Positive && Objective > Negative)
                return "Neutral";
            return Positive > Negative ? "Positive" : "Negative";
        }
    }

    /// <summary>
    /// Gets sentiment polarity score (-1 = negative, 0 = neutral, +1 = positive).
    /// </summary>
    public double Polarity => Positive - Negative;
}

/// <summary>
/// Statistics about loaded resources (for diagnostics).
/// </summary>
public record ResourceLoadingStats
{
    public bool SuffixesLoaded { get; init; }
    public bool PrefixesLoaded { get; init; }
    public bool WordlistLoaded { get; init; }
    public bool SentiWordNetLoaded { get; init; }
    public int SuffixCount { get; init; }
    public int PrefixCount { get; init; }
    public int WordlistCount { get; init; }
    public int SentiWordCount { get; init; }

    public override string ToString()
    {
        return $"""
                Resource Loading Statistics:
                - Suffixes: {(SuffixesLoaded ? $"Loaded ({SuffixCount} items)" : "Not loaded")}
                - Prefixes: {(PrefixesLoaded ? $"Loaded ({PrefixCount} items)" : "Not loaded")}
                - Wordlist: {(WordlistLoaded ? $"Loaded ({WordlistCount} words)" : "Not loaded")}
                - SentiWordNet: {(SentiWordNetLoaded ? $"Loaded ({SentiWordCount} entries)" : "Not loaded")}
                """;
    }
}
