using System.Text;
using System.Text.Json;

namespace Compression_Worker.Data;

/// <summary>
/// Persistent Romanian language model that contains all static linguistic data.
/// This data is shared across all compressed files, dramatically reducing metadata size.
///
/// Model contains:
/// - Stopwords list (300+ Romanian stopwords)
/// - Synonym dictionary (word pairs for normalization)
/// - Lemmatization rules (165 suffix transformation rules)
/// - Wordlist for validation
/// </summary>
public class RomanianLanguageModel
{
    private const string DefaultModelPath = "Models/romanian_language.model";

    public List<string> Stopwords { get; set; } = new();
    public Dictionary<string, string> Synonyms { get; set; } = new();
    public List<(string suffix, string replacement, int minLength)> LemmatizationRules { get; set; } = new();
    public HashSet<string> Wordlist { get; set; } = new();

    /// <summary>
    /// Builds the default Romanian language model from existing static data in the application.
    /// Extracts data from RomanianStopwords, SynonymReplacer, RomanianResourceLoader.
    /// </summary>
    public static RomanianLanguageModel BuildDefault()
    {
        var model = new RomanianLanguageModel();

        Console.WriteLine("[BUILD MODEL] Extracting linguistic data from application...\n");

        // 1. Extract stopwords from existing static class
        model.Stopwords = RomanianStopwords.GetStopwords().ToList();
        Console.WriteLine($"[BUILD MODEL] ✓ Stopwords: {model.Stopwords.Count}");

        // 2. Extract synonyms from SynonymReplacer
        model.Synonyms = Processors.Semantic.SynonymReplacer.GetSynonymGroups();
        Console.WriteLine($"[BUILD MODEL] ✓ Synonyms: {model.Synonyms.Count}");

        // 3. Extract lemmatization suffix rules from RomanianResourceLoader
        try
        {
            var suffixes = RomanianResourceLoader.GetSuffixes();
            model.LemmatizationRules = suffixes
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => (suffix: s.Trim(), replacement: "", minLength: 3))
                .ToList();
            Console.WriteLine($"[BUILD MODEL] ✓ Lemmatization rules: {model.LemmatizationRules.Count}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[BUILD MODEL] ⚠ Lemmatization rules: 0 (error: {ex.Message})");
            model.LemmatizationRules = new List<(string suffix, string replacement, int minLength)>();
        }

        // 4. Extract wordlist from RomanianResourceLoader (for validation in Accurate mode)
        try
        {
            model.Wordlist = RomanianResourceLoader.GetWordlist();
            Console.WriteLine($"[BUILD MODEL] ✓ Wordlist: {model.Wordlist.Count} words");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[BUILD MODEL] ⚠ Wordlist: 0 (error: {ex.Message})");
            model.Wordlist = new HashSet<string>();
        }

        Console.WriteLine($"\n[BUILD MODEL] Total persistent data extracted successfully!");

        return model;
    }

    /// <summary>
    /// Saves the language model to a file for persistent storage.
    /// </summary>
    public void Save(string path)
    {
        var modelData = new
        {
            version = "1.0",
            language = "Romanian",
            created = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC"),
            stopwords = Stopwords,
            synonyms = Synonyms,
            lemmatization_rules = LemmatizationRules.Select(r => new { r.suffix, r.replacement, r.minLength }),
            wordlist_count = Wordlist.Count,
            // Don't store full wordlist in JSON - too large (store separately or compress)
        };

        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(modelData, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        File.WriteAllText(path, json, Encoding.UTF8);

        // Store wordlist separately (compressed)
        var wordlistPath = path + ".wordlist";
        File.WriteAllLines(wordlistPath, Wordlist, Encoding.UTF8);

        Console.WriteLine($"[LANGUAGE MODEL] Saved to: {path}");
        Console.WriteLine($"[LANGUAGE MODEL] Stopwords: {Stopwords.Count}");
        Console.WriteLine($"[LANGUAGE MODEL] Synonyms: {Synonyms.Count}");
        Console.WriteLine($"[LANGUAGE MODEL] Rules: {LemmatizationRules.Count}");
        Console.WriteLine($"[LANGUAGE MODEL] Wordlist: {Wordlist.Count}");
    }

    /// <summary>
    /// Loads the language model from a file.
    /// </summary>
    public static RomanianLanguageModel Load(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Language model not found: {path}");
        }

        var json = File.ReadAllText(path, Encoding.UTF8);
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var model = new RomanianLanguageModel();

        // Load stopwords
        if (root.TryGetProperty("stopwords", out var stopwordsElement))
        {
            model.Stopwords = stopwordsElement.EnumerateArray()
                .Select(e => e.GetString()!)
                .ToList();
        }

        // Load synonyms
        if (root.TryGetProperty("synonyms", out var synonymsElement))
        {
            model.Synonyms = new Dictionary<string, string>();
            foreach (var prop in synonymsElement.EnumerateObject())
            {
                model.Synonyms[prop.Name] = prop.Value.GetString()!;
            }
        }

        // Load lemmatization rules
        if (root.TryGetProperty("lemmatization_rules", out var rulesElement))
        {
            model.LemmatizationRules = rulesElement.EnumerateArray()
                .Select(e =>
                {
                    var suffix = e.GetProperty("suffix").GetString()!;
                    var replacement = e.GetProperty("replacement").GetString()!;
                    var minLength = e.TryGetProperty("minLength", out var minLenProp) ? minLenProp.GetInt32() : 0;
                    return (suffix, replacement, minLength);
                })
                .ToList();
        }

        // Load wordlist separately
        var wordlistPath = path + ".wordlist";
        if (File.Exists(wordlistPath))
        {
            model.Wordlist = File.ReadAllLines(wordlistPath, Encoding.UTF8)
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .ToHashSet();
        }

        Console.WriteLine($"[LANGUAGE MODEL] Loaded from: {path}");
        Console.WriteLine($"[LANGUAGE MODEL] Stopwords: {model.Stopwords.Count}");
        Console.WriteLine($"[LANGUAGE MODEL] Synonyms: {model.Synonyms.Count}");
        Console.WriteLine($"[LANGUAGE MODEL] Rules: {model.LemmatizationRules.Count}");
        Console.WriteLine($"[LANGUAGE MODEL] Wordlist: {model.Wordlist.Count}");

        return model;
    }

    /// <summary>
    /// Gets or creates the default language model (with caching).
    /// </summary>
    public static RomanianLanguageModel GetDefault()
    {
        if (File.Exists(DefaultModelPath))
        {
            return Load(DefaultModelPath);
        }
        else
        {
            // Build from source files and save
            var model = BuildDefault();
            model.Save(DefaultModelPath);
            return model;
        }
    }
}
