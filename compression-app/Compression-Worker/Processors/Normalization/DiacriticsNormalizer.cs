using Compression_Worker.Core.Models;
using Compression_Worker.Processors.Base;

namespace Compression_Worker.Processors.Normalization;

/// <summary>
/// Processor that normalizes Romanian diacritics and capitalization.
/// Implements reversible normalization by tracking original casing and special characters.
/// Romanian has 5 special diacritics: ă, â, î, ș, ț
/// </summary>
public class DiacriticsNormalizer : ReversibleProcessorBase
{
    private const string CaseMetadataKey = "capitalization_map";
    private const string DiacriticsMetadataKey = "diacritics_map";

    protected override ProcessingContext ProcessCore(ProcessingContext context)
    {
        var tokens = context.Tokens;
        var caseMap = new Dictionary<int, CapitalizationInfo>();

        for (int i = 0; i < tokens.Count; i++)
        {
            string original = tokens[i];

            if (!IsWord(original))
                continue; // Skip punctuation

            // Store capitalization information
            var capInfo = ExtractCapitalization(original);
            if (capInfo.HasCapitalization)
            {
                caseMap[i] = capInfo;
            }

            // Normalize to lowercase (diacritics are preserved for now)
            tokens[i] = original.ToLowerInvariant();
        }

        context.Tokens = tokens;

        // OPTIMIZATION: Store compact list [[pos, type, indices], ...] instead of Dictionary<int, CapitalizationInfo>
        // Skip metadata storage in lossy mode (lowercase output is acceptable)
        if (!context.Lossy)
        {
            var compactCaseData = caseMap
                .OrderBy(kvp => kvp.Key)
                .Select(kvp => new object[]
                {
                    kvp.Key,  // position
                    (int)kvp.Value.Type,  // type as int (0=None, 1=FirstUpper, 2=AllUpper, 3=Mixed)
                    kvp.Value.UpperIndices ?? new List<int>()  // indices (empty list if null)
                })
                .ToList();

            context.SetMetadata(CaseMetadataKey, compactCaseData);
        }

        return context;
    }

    protected override ProcessingContext ReverseCore(ProcessingContext context)
    {
        // Try compact format first (new format)
        var compactData = context.GetMetadata<List<object[]>>(CaseMetadataKey);
        Dictionary<int, CapitalizationInfo> caseMap;

        if (compactData != null)
        {
            caseMap = new Dictionary<int, CapitalizationInfo>();
            foreach (var arr in compactData)
            {
                var pos = arr[0] is System.Text.Json.JsonElement je0 ? je0.GetInt32() : Convert.ToInt32(arr[0]);
                var typeInt = arr[1] is System.Text.Json.JsonElement je1 ? je1.GetInt32() : Convert.ToInt32(arr[1]);
                var type = (CapitalizationType)typeInt;

                var indices = arr[2] is System.Text.Json.JsonElement jsonElement
                    ? jsonElement.EnumerateArray().Select(e => e.GetInt32()).ToList()
                    : ((IEnumerable<object>)arr[2]).Select(o => Convert.ToInt32(o)).ToList();

                caseMap[pos] = new CapitalizationInfo(type, indices.Count > 0 ? indices : null);
            }
        }
        else
        {
            // Fallback: old dictionary format (backward compatibility)
            caseMap = context.GetMetadata<Dictionary<int, CapitalizationInfo>>(CaseMetadataKey);
        }

        if (caseMap == null || caseMap.Count == 0)
            return context; // Nothing to restore

        // Restore capitalization
        foreach (var kvp in caseMap)
        {
            if (kvp.Key < context.Tokens.Count)
            {
                context.Tokens[kvp.Key] = ApplyCapitalization(context.Tokens[kvp.Key], kvp.Value);
            }
        }

        return context;
    }

    /// <summary>
    /// Extracts capitalization pattern from a word.
    /// </summary>
    private static CapitalizationInfo ExtractCapitalization(string word)
    {
        if (string.IsNullOrEmpty(word))
            return new CapitalizationInfo(CapitalizationType.None);

        bool isAllUpper = word.All(c => !char.IsLetter(c) || char.IsUpper(c));
        bool isFirstUpper = char.IsUpper(word[0]) && word.Skip(1).All(c => !char.IsLetter(c) || char.IsLower(c));

        if (isAllUpper)
            return new CapitalizationInfo(CapitalizationType.AllUpper);
        if (isFirstUpper)
            return new CapitalizationInfo(CapitalizationType.FirstUpper);

        // Check for mixed case (like "McDonald")
        if (word.Any(char.IsUpper))
        {
            var upperIndices = word.Select((c, i) => new { Char = c, Index = i })
                                   .Where(x => char.IsUpper(x.Char))
                                   .Select(x => x.Index)
                                   .ToList();
            return new CapitalizationInfo(CapitalizationType.Mixed, upperIndices);
        }

        return new CapitalizationInfo(CapitalizationType.None);
    }

    /// <summary>
    /// Applies stored capitalization pattern to a word.
    /// </summary>
    private static string ApplyCapitalization(string word, CapitalizationInfo info)
    {
        if (string.IsNullOrEmpty(word))
            return word;

        switch (info.Type)
        {
            case CapitalizationType.AllUpper:
                return word.ToUpperInvariant();

            case CapitalizationType.FirstUpper:
                return char.ToUpperInvariant(word[0]) + word.Substring(1);

            case CapitalizationType.Mixed:
                if (info.UpperIndices != null)
                {
                    var chars = word.ToCharArray();
                    foreach (var index in info.UpperIndices)
                    {
                        if (index < chars.Length)
                            chars[index] = char.ToUpperInvariant(chars[index]);
                    }
                    return new string(chars);
                }
                return word;

            default:
                return word;
        }
    }

    private static bool IsWord(string token)
    {
        return !string.IsNullOrWhiteSpace(token) &&
               token.Length > 0 &&
               char.IsLetter(token[0]);
    }

    /// <summary>
    /// Record to store capitalization information for reversibility.
    /// </summary>
    private record CapitalizationInfo
    {
        public CapitalizationType Type { get; }
        public List<int>? UpperIndices { get; }

        public bool HasCapitalization => Type != CapitalizationType.None;

        public CapitalizationInfo(CapitalizationType type, List<int>? upperIndices = null)
        {
            Type = type;
            UpperIndices = upperIndices;
        }
    }

    private enum CapitalizationType
    {
        None,       // all lowercase
        FirstUpper, // First letter uppercase (Capitalized)
        AllUpper,   // ALL UPPERCASE
        Mixed       // MiXeD case (rare, like "McDonald")
    }
}
