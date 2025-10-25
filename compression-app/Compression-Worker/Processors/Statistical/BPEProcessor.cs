using Compression_Worker.Core.Models;
using Compression_Worker.Data;
using Compression_Worker.Processors.Base;
using System.Text.Json;

namespace Compression_Worker.Processors.Statistical;

/// <summary>
/// Byte-Pair Encoding (BPE) processor for subword tokenization.
/// Phase 3: Statistical Compression - Step 1
///
/// Splits words into subword units (e.g., "frumoase" → "fru", "moa", "se")
/// This reduces vocabulary size and improves compression for rare words.
///
/// Reversible: 100% - uses pre-trained persistent BPE model.
/// </summary>
public class BPEProcessor : ReversibleProcessorBase
{
    private const string MetadataKey = "bpe_data";
    private const string TokenMappingKey = "bpe_token_mapping";
    private const string DefaultModelPath = "Models/romanian_bpe_5000.model";

    private static BPEVocabulary? _cachedVocabulary;
    private static readonly object _cacheLock = new object();

    /// <summary>
    /// Loads pre-trained BPE vocabulary from persistent model file.
    /// Uses caching to avoid reloading on every compression.
    /// </summary>
    private BPEVocabulary LoadPretrainedModel()
    {
        lock (_cacheLock)
        {
            if (_cachedVocabulary != null)
            {
                Console.WriteLine("[BPE] Using cached pre-trained model");
                return _cachedVocabulary;
            }

            Console.WriteLine($"[BPE] Loading pre-trained model from: {DefaultModelPath}");

            if (!File.Exists(DefaultModelPath))
            {
                throw new FileNotFoundException($"Pre-trained BPE model not found: {DefaultModelPath}. Please train the model first using --mode train-bpe.");
            }

            var jsonText = File.ReadAllText(DefaultModelPath);
            var modelData = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonText);

            if (modelData == null || !modelData.ContainsKey("vocabulary"))
            {
                throw new InvalidOperationException("Invalid model file format");
            }

            var vocabMetadata = JsonSerializer.Deserialize<Dictionary<string, object>>(modelData["vocabulary"].ToString()!);
            _cachedVocabulary = BPEVocabulary.FromMetadata(vocabMetadata!);

            Console.WriteLine($"[BPE] Loaded model with {_cachedVocabulary.GetVocabSize()} vocabulary entries, {_cachedVocabulary.GetMergeCount()} merges");

            return _cachedVocabulary;
        }
    }

    protected override ProcessingContext ProcessCore(ProcessingContext context)
    {
        var tokens = context.Tokens;

        // Step 1: Load pre-trained BPE vocabulary (cached for performance)
        var vocabulary = LoadPretrainedModel();

        // Step 2: Tokenize each word into subword units
        var subwordTokens = new List<string>();
        var tokenMapping = new Dictionary<int, (int originalIndex, List<string> subwords)>();
        int subwordIndex = 0;

        for (int i = 0; i < tokens.Count; i++)
        {
            var token = tokens[i];

            if (IsWord(token))
            {
                // Apply BPE tokenization
                var subwords = vocabulary.Tokenize(token);

                // Store mapping for reversibility
                tokenMapping[i] = (subwordIndex, subwords);

                // Add subwords to output
                subwordTokens.AddRange(subwords);
                subwordIndex += subwords.Count;
            }
            else
            {
                // Keep punctuation and whitespace as-is
                subwordTokens.Add(token);
                subwordIndex++;
            }
        }

        // Step 3: Update context with subword tokens
        context.Tokens = subwordTokens;

        // Step 4: Store ONLY token mapping metadata (positions + counts, NOT actual subwords!)
        // The subwords are already in the arithmetic coded stream, so we don't need to duplicate them
        // This dramatically reduces .cmp file size from 19MB to ~1MB!
        var mappingData = new List<string>();
        foreach (var (originalIdx, (subwordStart, subwords)) in tokenMapping)
        {
            // Store only: original_index | subword_start_position | subword_count
            mappingData.Add($"{originalIdx}|{subwordStart}|{subwords.Count}");
        }
        context.SetMetadata(TokenMappingKey, mappingData);
        context.SetMetadata("bpe_original_token_count", tokens.Count); // Store original count
        context.SetMetadata("bpe_model_path", DefaultModelPath); // Store model path for verification

        Console.WriteLine($"[BPE] Tokenized {tokens.Count} tokens → {subwordTokens.Count} subwords");
        Console.WriteLine($"[BPE] Metadata size: {mappingData.Count} token mappings (positions only, subwords in AC stream)");

        return context;
    }

    protected override ProcessingContext ReverseCore(ProcessingContext context)
    {
        // Step 1: Load BPE vocabulary from persistent model (NOT from metadata)
        var vocabulary = LoadPretrainedModel();

        // Step 2: Load token mapping
        var mappingDataObj = context.GetMetadata<object>(TokenMappingKey);
        if (mappingDataObj == null)
            return context;

        // Deserialize token mapping
        IEnumerable<object> mappingData = mappingDataObj switch
        {
            List<string> stringList => stringList.Cast<object>(),
            List<object> objectList => objectList,
            System.Text.Json.JsonElement jsonElement when jsonElement.ValueKind == System.Text.Json.JsonValueKind.Array =>
                jsonElement.EnumerateArray().Select(e => (object)e.GetString()!),
            System.Collections.IEnumerable enumerable => enumerable.Cast<object>(),
            _ => throw new InvalidOperationException($"Unexpected type for token_mapping: {mappingDataObj.GetType()}")
        };

        var tokenMapping = new Dictionary<int, (int subwordStart, int subwordCount)>();
        foreach (var item in mappingData)
        {
            var parts = item.ToString()!.Split('|');
            if (parts.Length == 3)
            {
                var originalIdx = int.Parse(parts[0]);
                var subwordStart = int.Parse(parts[1]);
                var subwordCount = int.Parse(parts[2]); // Now storing count, not the actual subwords
                tokenMapping[originalIdx] = (subwordStart, subwordCount);
            }
        }

        // Step 3: Reconstruct original tokens
        // Key insight: context.Tokens has the FULL subword sequence after arithmetic decoding
        // We need to read subwords at their absolute positions and reconstruct words

        var originalTokenCount = context.GetMetadata<int>("bpe_original_token_count");
        var originalTokens = new List<string>();

        // DEBUG: Print what we have
        Console.WriteLine($"[BPE DEBUG] Original token count: {originalTokenCount}");
        Console.WriteLine($"[BPE DEBUG] Current tokens count: {context.Tokens.Count}");
        Console.WriteLine($"[BPE DEBUG] Token mapping entries: {tokenMapping.Count}");
        Console.WriteLine($"[BPE DEBUG] First 10 tokens: {string.Join(", ", context.Tokens.Take(10).Select(t => $"\"{t}\""))}");

        foreach (var kvp in tokenMapping.OrderBy(x => x.Key).Take(5))
        {
            Console.WriteLine($"[BPE DEBUG] Mapping[{kvp.Key}] = subwordStart:{kvp.Value.subwordStart}, subwordCount:{kvp.Value.subwordCount}");
        }

        // OPTIMIZATION: Pre-calculate mapping from original token index to subword index
        // This changes algorithm from O(n²) to O(n)
        var origIdxToSubwordIdx = new Dictionary<int, int>();
        int currentSubwordIdx = 0;

        for (int origIdx = 0; origIdx < originalTokenCount; origIdx++)
        {
            origIdxToSubwordIdx[origIdx] = currentSubwordIdx;

            if (tokenMapping.TryGetValue(origIdx, out var mapping))
            {
                // Word token: advances by number of subwords
                currentSubwordIdx += mapping.subwordCount;
            }
            else
            {
                // Non-word token: advances by 1
                currentSubwordIdx++;
            }
        }

        Console.WriteLine($"[BPE DEBUG] Pre-calculated subword index mapping for {originalTokenCount} tokens");

        // Process original tokens in order
        for (int origIdx = 0; origIdx < originalTokenCount; origIdx++)
        {
            if (tokenMapping.TryGetValue(origIdx, out var mapping))
            {
                // This original token was a word that was BPE-tokenized
                var (subwordStart, subwordCount) = mapping;

                // Extract actual subwords from context.Tokens at their stored positions
                var actualSubwords = new List<string>();
                for (int i = 0; i < subwordCount; i++)
                {
                    if (subwordStart + i < context.Tokens.Count)
                    {
                        actualSubwords.Add(context.Tokens[subwordStart + i]);
                    }
                }

                // Detokenize subwords back to original word
                var word = vocabulary.Detokenize(actualSubwords);
                if (origIdx < 100) // Only debug first 100 to avoid spam
                {
                    Console.WriteLine($"[BPE DEBUG] origIdx={origIdx} (word): reconstructed \"{word}\" from {subwordCount} subwords at [{subwordStart}..{subwordStart + subwordCount - 1}]");
                }
                originalTokens.Add(word);
            }
            else
            {
                // This original token was NOT a word (punctuation, whitespace, etc.)
                // It was kept as-is in the subword sequence
                // Use pre-calculated subword index
                int subwordIdx = origIdxToSubwordIdx[origIdx];

                // Now get the non-word token at this calculated position
                if (subwordIdx < context.Tokens.Count)
                {
                    var token = context.Tokens[subwordIdx];
                    if (origIdx < 100) // Only debug first 100 to avoid spam
                    {
                        Console.WriteLine($"[BPE DEBUG] origIdx={origIdx} (non-word): got \"{token}\" from subwordIdx={subwordIdx}");
                    }
                    originalTokens.Add(token);
                }
            }

            // Progress indicator every 10000 tokens
            if ((origIdx + 1) % 10000 == 0)
            {
                Console.WriteLine($"[BPE DEBUG] Progress: {origIdx + 1}/{originalTokenCount} tokens reconstructed ({(origIdx + 1) * 100.0 / originalTokenCount:F1}%)");
            }
        }

        Console.WriteLine($"[BPE DEBUG] Reconstructed {originalTokens.Count} tokens");

        context.Tokens = originalTokens;
        return context;
    }

    /// <summary>
    /// Determines if a token is a word (as opposed to punctuation/whitespace).
    /// </summary>
    private static bool IsWord(string token)
    {
        return !string.IsNullOrWhiteSpace(token) &&
               token.Length > 0 &&
               char.IsLetter(token[0]);
    }
}
