using System.Text;

namespace Compression_Worker.Data;

/// <summary>
/// Byte-Pair Encoding (BPE) vocabulary for subword tokenization.
/// Learns merge operations from training data to split words into subword units.
/// Used in Phase 3 for statistical compression.
/// </summary>
public class BPEVocabulary
{
    private readonly List<(string, string)> _mergeOperations;
    private readonly Dictionary<string, int> _vocabulary;
    private const int DefaultMergeCount = 5000;

    public IReadOnlyList<(string, string)> MergeOperations => _mergeOperations.AsReadOnly();
    public IReadOnlyDictionary<string, int> Vocabulary => _vocabulary;

    public BPEVocabulary(int vocabSize = DefaultMergeCount)
    {
        _mergeOperations = new List<(string, string)>();
        _vocabulary = new Dictionary<string, int>();
    }

    /// <summary>
    /// Trains BPE vocabulary from a corpus of words.
    /// Learns most frequent character pairs and merges them iteratively.
    /// </summary>
    /// <param name="words">List of words to train on</param>
    /// <param name="numMerges">Number of merge operations to learn (default 5000)</param>
    public void Train(IEnumerable<string> words, int numMerges = DefaultMergeCount)
    {
        _mergeOperations.Clear();
        _vocabulary.Clear();

        // Step 1: Initialize vocabulary with character-level tokens
        // Each word is split into characters with end-of-word marker
        var wordFrequencies = new Dictionary<string, int>();
        foreach (var word in words)
        {
            if (string.IsNullOrWhiteSpace(word)) continue;

            var splitWord = string.Join(" ", word.ToCharArray()) + " </w>";
            wordFrequencies[splitWord] = wordFrequencies.GetValueOrDefault(splitWord, 0) + 1;
        }

        // Step 2: Learn merge operations iteratively
        for (int i = 0; i < numMerges; i++)
        {
            // Find most frequent pair
            var pairCounts = GetPairFrequencies(wordFrequencies);
            if (pairCounts.Count == 0) break;

            var mostFrequentPair = pairCounts.OrderByDescending(p => p.Value).First().Key;

            // Merge the pair in all words
            var newWordFreqs = new Dictionary<string, int>();
            foreach (var (wordForm, frequency) in wordFrequencies)
            {
                var newForm = wordForm.Replace($"{mostFrequentPair.Item1} {mostFrequentPair.Item2}",
                                               $"{mostFrequentPair.Item1}{mostFrequentPair.Item2}");
                newWordFreqs[newForm] = frequency;
            }

            wordFrequencies = newWordFreqs;
            _mergeOperations.Add(mostFrequentPair);
        }

        // Step 3: Build final vocabulary from learned merges
        BuildVocabulary(wordFrequencies);
    }

    /// <summary>
    /// Tokenizes a word into subword units using learned BPE merges.
    /// </summary>
    public List<string> Tokenize(string word)
    {
        if (string.IsNullOrWhiteSpace(word))
            return new List<string>();

        // Start with character-level tokens
        var tokens = word.ToCharArray().Select(c => c.ToString()).ToList();
        tokens.Add("</w>"); // End-of-word marker

        // Apply merge operations
        foreach (var (first, second) in _mergeOperations)
        {
            for (int i = 0; i < tokens.Count - 1; i++)
            {
                if (tokens[i] == first && tokens[i + 1] == second)
                {
                    tokens[i] = first + second;
                    tokens.RemoveAt(i + 1);
                    i--; // Recheck same position
                }
            }
        }

        return tokens;
    }

    /// <summary>
    /// Detokenizes subword units back to original word.
    /// </summary>
    public string Detokenize(List<string> tokens)
    {
        var sb = new StringBuilder();
        foreach (var token in tokens)
        {
            // Skip standalone end-of-word marker
            if (token == "</w>")
                continue;

            // Remove end-of-word marker if it's part of a merged token
            var cleaned = token.Replace("</w>", "");
            sb.Append(cleaned);
        }
        return sb.ToString();
    }

    /// <summary>
    /// Gets the vocabulary size (number of unique subword tokens).
    /// </summary>
    public int GetVocabSize() => _vocabulary.Count;

    /// <summary>
    /// Gets the number of merge operations learned.
    /// </summary>
    public int GetMergeCount() => _mergeOperations.Count;

    /// <summary>
    /// Gets the merge operations as a list of (pair, merged) tuples.
    /// </summary>
    public List<((string, string) pair, string merged)> GetMerges()
    {
        return _mergeOperations.Select(m => (m, $"{m.Item1}{m.Item2}")).ToList();
    }

    /// <summary>
    /// Serializes vocabulary to dictionary for metadata storage.
    /// </summary>
    public Dictionary<string, object> ToMetadata()
    {
        return new Dictionary<string, object>
        {
            ["merge_operations"] = _mergeOperations.Select(m => $"{m.Item1}|{m.Item2}").ToList(),
            ["vocab_size"] = _vocabulary.Count
        };
    }

    /// <summary>
    /// Deserializes vocabulary from metadata.
    /// </summary>
    public static BPEVocabulary FromMetadata(Dictionary<string, object> metadata)
    {
        var vocab = new BPEVocabulary();

        if (metadata.TryGetValue("merge_operations", out var mergeOpsObj))
        {
            // Handle various serialization formats
            IEnumerable<object> mergeOps = mergeOpsObj switch
            {
                List<string> stringList => stringList.Cast<object>(),
                List<object> objectList => objectList,
                System.Text.Json.JsonElement jsonElement when jsonElement.ValueKind == System.Text.Json.JsonValueKind.Array =>
                    jsonElement.EnumerateArray().Select(e => (object)e.GetString()!),
                System.Collections.IEnumerable enumerable => enumerable.Cast<object>(),
                _ => throw new InvalidOperationException($"Unexpected type for merge_operations: {mergeOpsObj.GetType()}")
            };

            foreach (var op in mergeOps)
            {
                var parts = op.ToString()!.Split('|');
                if (parts.Length == 2)
                {
                    vocab._mergeOperations.Add((parts[0], parts[1]));
                }
            }
        }

        return vocab;
    }

    private Dictionary<(string, string), int> GetPairFrequencies(Dictionary<string, int> wordFreqs)
    {
        var pairCounts = new Dictionary<(string, string), int>();

        foreach (var (word, frequency) in wordFreqs)
        {
            var symbols = word.Split(' ');
            for (int i = 0; i < symbols.Length - 1; i++)
            {
                var pair = (symbols[i], symbols[i + 1]);
                pairCounts[pair] = pairCounts.GetValueOrDefault(pair, 0) + frequency;
            }
        }

        return pairCounts;
    }

    private void BuildVocabulary(Dictionary<string, int> wordFreqs)
    {
        foreach (var (word, frequency) in wordFreqs)
        {
            var tokens = word.Split(' ');
            foreach (var token in tokens)
            {
                if (!string.IsNullOrWhiteSpace(token))
                {
                    _vocabulary[token] = _vocabulary.GetValueOrDefault(token, 0) + frequency;
                }
            }
        }
    }
}
