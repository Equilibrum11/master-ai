namespace Compression_Worker.Data;

/// <summary>
/// N-gram language model for context-based probability estimation.
/// Uses trigrams (n=3) with Laplace smoothing for unknown sequences.
/// Used in Phase 3 for arithmetic coding probability distribution.
/// </summary>
public class NGramModel
{
    private readonly int _n;
    private readonly Dictionary<string, Dictionary<string, int>> _ngramCounts;
    private readonly Dictionary<string, int> _contextCounts;
    private readonly double _smoothingFactor = 1.0; // Laplace smoothing
    private int _vocabularySize;

    public int N => _n;
    public int VocabularySize => _vocabularySize;

    /// <summary>
    /// Creates an n-gram model.
    /// </summary>
    /// <param name="n">Order of n-gram (3 for trigram)</param>
    public NGramModel(int n = 3)
    {
        _n = n;
        _ngramCounts = new Dictionary<string, Dictionary<string, int>>();
        _contextCounts = new Dictionary<string, int>();
    }

    /// <summary>
    /// Trains the n-gram model from a sequence of tokens.
    /// </summary>
    public void Train(List<string> tokens)
    {
        _ngramCounts.Clear();
        _contextCounts.Clear();

        // Build vocabulary
        var vocabulary = new HashSet<string>(tokens);
        _vocabularySize = vocabulary.Count;

        // Add start/end markers
        var paddedTokens = new List<string>();
        for (int i = 0; i < _n - 1; i++)
            paddedTokens.Add("<START>");
        paddedTokens.AddRange(tokens);
        paddedTokens.Add("<END>");

        // Count n-grams
        for (int i = 0; i <= paddedTokens.Count - _n; i++)
        {
            // Context = first (n-1) tokens
            var context = string.Join("␣", paddedTokens.Skip(i).Take(_n - 1));

            // Target = last token
            var target = paddedTokens[i + _n - 1];

            // Update counts
            if (!_ngramCounts.ContainsKey(context))
            {
                _ngramCounts[context] = new Dictionary<string, int>();
            }

            _ngramCounts[context][target] = _ngramCounts[context].GetValueOrDefault(target, 0) + 1;
            _contextCounts[context] = _contextCounts.GetValueOrDefault(context, 0) + 1;
        }
    }

    /// <summary>
    /// Gets probability of a token given its context (previous n-1 tokens).
    /// Uses Laplace smoothing for unseen n-grams.
    /// </summary>
    /// <param name="context">Previous (n-1) tokens joined by separator</param>
    /// <param name="token">Token to predict</param>
    /// <returns>Probability between 0 and 1</returns>
    public double GetProbability(string context, string token)
    {
        // Laplace smoothing: P(token|context) = (count(context, token) + α) / (count(context) + α * V)
        // where α = smoothing factor, V = vocabulary size

        int tokenCount = 0;
        if (_ngramCounts.TryGetValue(context, out var targets))
        {
            tokenCount = targets.GetValueOrDefault(token, 0);
        }

        int contextCount = _contextCounts.GetValueOrDefault(context, 0);

        double numerator = tokenCount + _smoothingFactor;
        double denominator = contextCount + _smoothingFactor * _vocabularySize;

        return numerator / denominator;
    }

    /// <summary>
    /// Gets cumulative probability distribution for all tokens given a context.
    /// Required for arithmetic coding.
    /// </summary>
    /// <returns>Dictionary mapping token → cumulative probability</returns>
    public Dictionary<string, double> GetCumulativeDistribution(string context)
    {
        var distribution = new Dictionary<string, double>();

        // Get all possible tokens (vocabulary)
        var allTokens = new HashSet<string>();
        if (_ngramCounts.TryGetValue(context, out var targets))
        {
            allTokens.UnionWith(targets.Keys);
        }

        // Add all vocabulary tokens for smoothing
        foreach (var ngramDict in _ngramCounts.Values)
        {
            allTokens.UnionWith(ngramDict.Keys);
        }

        // Calculate probabilities and cumulative sums
        double cumulative = 0.0;
        foreach (var token in allTokens.OrderBy(t => t)) // Consistent ordering
        {
            double prob = GetProbability(context, token);
            distribution[token] = cumulative;
            cumulative += prob;
        }

        // Add end marker
        distribution["<END>"] = cumulative;

        return distribution;
    }

    /// <summary>
    /// Serializes model to metadata for storage.
    /// </summary>
    public Dictionary<string, object> ToMetadata()
    {
        var metadata = new Dictionary<string, object>
        {
            ["n"] = _n,
            ["vocab_size"] = _vocabularySize,
            ["smoothing"] = _smoothingFactor
        };

        // Serialize n-gram counts (compact format)
        var ngramData = new List<string>();
        foreach (var (context, targets) in _ngramCounts)
        {
            foreach (var (token, count) in targets)
            {
                ngramData.Add($"{context}|{token}|{count}");
            }
        }
        metadata["ngram_counts"] = ngramData;

        // Serialize context counts
        var contextData = _contextCounts.Select(kv => $"{kv.Key}|{kv.Value}").ToList();
        metadata["context_counts"] = contextData;

        return metadata;
    }

    /// <summary>
    /// Deserializes model from metadata.
    /// </summary>
    public static NGramModel FromMetadata(Dictionary<string, object> metadata)
    {
        // Handle JsonElement for numeric values
        int n = metadata["n"] switch
        {
            int intValue => intValue,
            System.Text.Json.JsonElement jsonElement when jsonElement.ValueKind == System.Text.Json.JsonValueKind.Number =>
                jsonElement.GetInt32(),
            var obj => Convert.ToInt32(obj)
        };
        var model = new NGramModel(n);

        model._vocabularySize = metadata["vocab_size"] switch
        {
            int intValue => intValue,
            System.Text.Json.JsonElement jsonElement when jsonElement.ValueKind == System.Text.Json.JsonValueKind.Number =>
                jsonElement.GetInt32(),
            var obj => Convert.ToInt32(obj)
        };

        // Deserialize n-gram counts
        if (metadata.TryGetValue("ngram_counts", out var ngramDataObj))
        {
            // Handle both List<string> and List<object> (depending on serialization method)
            IEnumerable<object> ngramData = ngramDataObj switch
            {
                List<string> stringList => stringList.Cast<object>(),
                List<object> objectList => objectList,
                System.Text.Json.JsonElement jsonElement when jsonElement.ValueKind == System.Text.Json.JsonValueKind.Array =>
                    jsonElement.EnumerateArray().Select(e => (object)e.GetString()!),
                System.Collections.IEnumerable enumerable => enumerable.Cast<object>(),
                _ => throw new InvalidOperationException($"Unexpected type for ngram_counts: {ngramDataObj.GetType()}")
            };

            foreach (var item in ngramData)
            {
                var parts = item.ToString()!.Split('|');
                if (parts.Length == 3)
                {
                    var context = parts[0];
                    var token = parts[1];
                    var count = int.Parse(parts[2]);

                    if (!model._ngramCounts.ContainsKey(context))
                    {
                        model._ngramCounts[context] = new Dictionary<string, int>();
                    }
                    model._ngramCounts[context][token] = count;
                }
            }
        }

        // Deserialize context counts
        if (metadata.TryGetValue("context_counts", out var contextDataObj))
        {
            // Handle both List<string> and List<object> (depending on serialization method)
            IEnumerable<object> contextData = contextDataObj switch
            {
                List<string> stringList => stringList.Cast<object>(),
                List<object> objectList => objectList,
                System.Text.Json.JsonElement jsonElement when jsonElement.ValueKind == System.Text.Json.JsonValueKind.Array =>
                    jsonElement.EnumerateArray().Select(e => (object)e.GetString()!),
                System.Collections.IEnumerable enumerable => enumerable.Cast<object>(),
                _ => throw new InvalidOperationException($"Unexpected type for context_counts: {contextDataObj.GetType()}")
            };

            foreach (var item in contextData)
            {
                var parts = item.ToString()!.Split('|');
                if (parts.Length == 2)
                {
                    model._contextCounts[parts[0]] = int.Parse(parts[1]);
                }
            }
        }

        return model;
    }

    /// <summary>
    /// Gets all unique tokens in the vocabulary.
    /// </summary>
    public HashSet<string> GetVocabulary()
    {
        var vocabulary = new HashSet<string>();

        // Collect all tokens from n-gram counts
        foreach (var targets in _ngramCounts.Values)
        {
            vocabulary.UnionWith(targets.Keys);
        }

        return vocabulary;
    }
}
