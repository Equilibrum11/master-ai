using Compression_Worker.Core.Models;
using Compression_Worker.Data;
using Compression_Worker.Processors.Base;

namespace Compression_Worker.Processors.Semantic;

/// <summary>
/// Processor that replaces synonyms with a representative word.
/// Implements reversible semantic compression by identifying and consolidating synonyms.
///
/// NOTE: This is a simplified implementation. Full version would use SentiWordNet
/// to build comprehensive synonym groups. Currently uses a small hardcoded set
/// for demonstration purposes.
///
/// Future enhancement: Load synonym groups from SentiWordNet based on semantic similarity.
/// </summary>
public class SynonymReplacer : ReversibleProcessorBase
{
    private const string MetadataKey = "synonym_replacements";
    private readonly Dictionary<string, string> _synonymGroups;

    public SynonymReplacer()
    {
        _synonymGroups = BuildSynonymGroups();
    }

    /// <summary>
    /// Gets the synonym groups dictionary (for persistent model building).
    /// </summary>
    public static Dictionary<string, string> GetSynonymGroups() => BuildSynonymGroups();

    /// <summary>
    /// Builds synonym groups (sinonim → reprezentativ).
    /// Currently hardcoded for demonstration. Future: load from SentiWordNet.
    /// </summary>
    private static Dictionary<string, string> BuildSynonymGroups()
    {
        // Simplified version with common Romanian synonyms
        // Format: synonym → representative (shortest/most common)
        var groups = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // Casa/Locuință group
            {"locuință", "casa"},
            {"casă", "casa"},
            {"reședință", "casa"},

            // Fericit/Vesel group
            {"fericit", "vesel"},
            {"bucuros", "vesel"},
            {"încântat", "vesel"},

            // Mare/Spațios group
            {"spațios", "mare"},
            {"vast", "mare"},
            {"amplu", "mare"},

            // Frumos/Plăcut group
            {"plăcut", "frumos"},
            {"drăguț", "frumos"},
            {"atrăgător", "frumos"},

            // Rapid/Repede group
            {"rapid", "repede"},
            {"iute", "repede"},
            {"grăbit", "repede"},

            // Important/Esențial group
            {"important", "esential"},
            {"crucial", "esential"},
            {"vital", "esential"},

            // Bun/Excelent group
            {"excelent", "bun"},
            {"minunat", "bun"},
            {"extraordinar", "bun"},
        };

        return groups;
    }

    protected override ProcessingContext ProcessCore(ProcessingContext context)
    {
        var replacements = new Dictionary<int, string>();

        for (int i = 0; i < context.Tokens.Count; i++)
        {
            var token = context.Tokens[i];

            // Only process words
            if (!IsWord(token))
                continue;

            // Check if token is a synonym
            if (_synonymGroups.TryGetValue(token, out var representative))
            {
                replacements[i] = token; // Save original
                context.Tokens[i] = representative; // Replace with representative
            }
        }

        // OPTIMIZATION: Use vocabulary-based encoding (dictionary compression)
        // Skip metadata storage in lossy mode
        if (!context.Lossy)
        {
            var uniqueTokens = replacements.Values.Distinct().OrderBy(t => t).ToList();
            var tokenToIndex = uniqueTokens.Select((token, idx) => new { token, idx })
                                           .ToDictionary(x => x.token, x => x.idx);

            var positionsWithIndices = replacements
                .OrderBy(kvp => kvp.Key)
                .Select(kvp => new int[] { kvp.Key, tokenToIndex[kvp.Value] })
                .ToList();

            var vocabularyEncodedData = new Dictionary<string, object>
            {
                ["vocab"] = uniqueTokens,
                ["positions"] = positionsWithIndices
            };

            context.SetMetadata(MetadataKey, vocabularyEncodedData);
        }
        return context;
    }

    protected override ProcessingContext ReverseCore(ProcessingContext context)
    {
        Dictionary<int, string> replacements;

        // Try vocabulary-encoded format first (newest format)
        var vocabData = context.GetMetadata<Dictionary<string, object>>(MetadataKey);
        if (vocabData != null && vocabData.ContainsKey("vocab") && vocabData.ContainsKey("positions"))
        {
            // Decode vocabulary-based format
            var vocab = vocabData["vocab"] is System.Text.Json.JsonElement vocabElem
                ? vocabElem.EnumerateArray().Select(e => e.GetString()!).ToList()
                : ((IEnumerable<object>)vocabData["vocab"]).Select(o => o.ToString()!).ToList();

            var positions = vocabData["positions"] is System.Text.Json.JsonElement posElem
                ? posElem.EnumerateArray().Select(arr => new[] {
                    arr[0].GetInt32(),
                    arr[1].GetInt32()
                  }).ToList()
                : ((IEnumerable<object>)vocabData["positions"]).Select(o =>
                  {
                      var arr = (object[])o;
                      return new[] {
                          arr[0] is System.Text.Json.JsonElement je0 ? je0.GetInt32() : Convert.ToInt32(arr[0]),
                          arr[1] is System.Text.Json.JsonElement je1 ? je1.GetInt32() : Convert.ToInt32(arr[1])
                      };
                  }).ToList();

            replacements = positions.ToDictionary(
                arr => arr[0],
                arr => vocab[arr[1]]
            );
        }
        else
        {
            // Try compact format (old new format)
            var compactData = context.GetMetadata<List<object[]>>(MetadataKey);
            if (compactData != null)
            {
                replacements = new Dictionary<int, string>();
                foreach (var arr in compactData)
                {
                    var pos = arr[0] is System.Text.Json.JsonElement je0 ? je0.GetInt32() : Convert.ToInt32(arr[0]);
                    var token = arr[1] is System.Text.Json.JsonElement je1 ? je1.GetString()! : arr[1].ToString()!;
                    replacements[pos] = token;
                }
            }
            else
            {
                // Fallback: old dictionary format (backward compatibility)
                replacements = context.GetMetadata<Dictionary<int, string>>(MetadataKey);
            }
        }

        if (replacements == null || replacements.Count == 0)
            return context; // Nothing to restore

        // Restore original synonyms
        foreach (var kvp in replacements)
        {
            if (kvp.Key < context.Tokens.Count)
            {
                context.Tokens[kvp.Key] = kvp.Value;
            }
        }

        return context;
    }

    private static bool IsWord(string token)
    {
        return !string.IsNullOrWhiteSpace(token) &&
               token.Length > 0 &&
               char.IsLetter(token[0]);
    }

    /// <summary>
    /// Gets the number of synonym groups configured.
    /// </summary>
    public int SynonymGroupCount => _synonymGroups.Count;
}
