using Compression_Worker.Core.Models;
using Compression_Worker.Data;
using Compression_Worker.Processors.Base;
using Compression_Worker.Utils;

namespace Compression_Worker.Processors.Stopwords;

/// <summary>
/// Processor that removes Romanian stopwords while preserving positional information.
/// Implements reversible compression by storing removed words and their positions.
/// Based on linguistic redundancy principle: ~60% of words can be removed without losing core meaning.
/// </summary>
public class StopwordsProcessor : ReversibleProcessorBase
{
    private const string MetadataKey = "stopwords_removed";

    protected override ProcessingContext ProcessCore(ProcessingContext context)
    {
        var tokens = context.Tokens;
        var removedTokens = new Dictionary<int, string>();

        // Track removed stopwords and their original positions (including following whitespace)
        for (int i = 0; i < tokens.Count; i++)
        {
            if (TextTokenizer.IsWord(tokens[i]) && RomanianStopwords.IsStopword(tokens[i]))
            {
                removedTokens[i] = tokens[i];

                // Also mark the following whitespace for removal if it exists
                if (i + 1 < tokens.Count && string.IsNullOrWhiteSpace(tokens[i + 1]))
                {
                    removedTokens[i + 1] = tokens[i + 1];
                }
            }
        }

        // Remove stopwords and associated whitespace
        context.Tokens = tokens
            .Select((token, index) => removedTokens.ContainsKey(index) ? null : token)
            .Where(token => token != null)
            .ToList()!;

        // OPTIMIZATION: Use vocabulary-based encoding (dictionary compression)
        // Skip metadata storage in lossy mode (no reversibility needed)
        if (!context.Lossy)
        {
            // Extract unique tokens (vocabulary)
            var uniqueTokens = removedTokens.Values.Distinct().OrderBy(t => t).ToList();
            var tokenToIndex = uniqueTokens.Select((token, idx) => new { token, idx })
                                           .ToDictionary(x => x.token, x => x.idx);

            // Store positions with vocabulary indices instead of full strings
            var positionsWithIndices = removedTokens
                .OrderBy(kvp => kvp.Key)
                .Select(kvp => new int[] { kvp.Key, tokenToIndex[kvp.Value] })
                .ToList();

            var vocabularyEncodedData = new Dictionary<string, object>
            {
                ["vocab"] = uniqueTokens,  // Unique tokens (218 stopwords max)
                ["positions"] = positionsWithIndices  // [[position, vocab_index], ...]
            };

            context.SetMetadata(MetadataKey, vocabularyEncodedData);
        }

        return context;
    }

    protected override ProcessingContext ReverseCore(ProcessingContext context)
    {
        Dictionary<int, string> removedWords;

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

            removedWords = positions.ToDictionary(
                arr => arr[0],  // position
                arr => vocab[arr[1]]  // lookup word by vocab index
            );
        }
        else
        {
            // Try compact format (old new format)
            var compactData = context.GetMetadata<List<object[]>>(MetadataKey);
            if (compactData != null)
            {
                removedWords = new Dictionary<int, string>();
                foreach (var arr in compactData)
                {
                    var pos = arr[0] is System.Text.Json.JsonElement je0 ? je0.GetInt32() : Convert.ToInt32(arr[0]);
                    var token = arr[1] is System.Text.Json.JsonElement je1 ? je1.GetString()! : arr[1].ToString()!;
                    removedWords[pos] = token;
                }
            }
            else
            {
                // Fallback: Try old dictionary format (backward compatibility)
                removedWords = context.GetMetadata<Dictionary<int, string>>(MetadataKey);
            }
        }

        if (removedWords == null || removedWords.Count == 0)
            return context; // Nothing to restore

        // Build a complete token list by merging current tokens with removed ones
        // We know the final size will be current tokens + removed tokens
        var restoredTokens = new List<string>(context.Tokens.Count + removedWords.Count);

        int currentTokenIndex = 0;
        int maxOriginalIndex = removedWords.Keys.Max();

        // Iterate through all original positions
        for (int originalIndex = 0; originalIndex <= maxOriginalIndex || currentTokenIndex < context.Tokens.Count; originalIndex++)
        {
            if (removedWords.ContainsKey(originalIndex))
            {
                // This position had a removed token - restore it
                restoredTokens.Add(removedWords[originalIndex]);
            }
            else if (currentTokenIndex < context.Tokens.Count)
            {
                // This position should get the next current token
                restoredTokens.Add(context.Tokens[currentTokenIndex++]);
            }
        }

        context.Tokens = restoredTokens;
        return context;
    }

    protected override void SaveMetadata(ProcessingContext context)
    {
        // Metadata already saved in ProcessCore
        // This override is here to demonstrate the hook pattern
    }
}
