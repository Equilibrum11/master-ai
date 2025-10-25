using Compression_Worker.Utils;

namespace Compression_Worker.Core.Models;

/// <summary>
/// Data Transfer Object that carries text data and metadata through the processing pipeline.
/// Enables reversibility by storing transformation metadata.
/// </summary>
public class ProcessingContext
{
    /// <summary>
    /// The original unprocessed text (immutable).
    /// </summary>
    public string OriginalText { get; }

    /// <summary>
    /// Tokenized representation of the current text state.
    /// Modified by each processor in the pipeline.
    /// </summary>
    public List<string> Tokens { get; set; }

    /// <summary>
    /// Metadata dictionary for storing reversibility information.
    /// Each processor stores its transformation data here.
    /// </summary>
    public Dictionary<string, object> Metadata { get; }

    /// <summary>
    /// Lossy compression mode - don't store metadata for reversibility.
    /// Produces smaller files but cannot perfectly reconstruct original.
    /// </summary>
    public bool Lossy { get; set; } = false;

    /// <summary>
    /// Gets the current text representation by concatenating tokens.
    /// Uses TextTokenizer.Detokenize to preserve exact spacing.
    /// </summary>
    public string Text => TextTokenizer.Detokenize(Tokens);

    public ProcessingContext(string text, bool lossy = false)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("Input text cannot be null or empty", nameof(text));

        OriginalText = text;
        Tokens = new List<string>();
        Metadata = new Dictionary<string, object>();
        Lossy = lossy;
    }

    /// <summary>
    /// Stores metadata for a processor.
    /// </summary>
    public void SetMetadata(string key, object value)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Metadata key cannot be null or empty", nameof(key));

        Metadata[key] = value;
    }

    /// <summary>
    /// Retrieves strongly-typed metadata.
    /// </summary>
    public T GetMetadata<T>(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Metadata key cannot be null or empty", nameof(key));

        return Metadata.TryGetValue(key, out var value) ? (T)value : default!;
    }

    /// <summary>
    /// Checks if metadata exists for a given key.
    /// </summary>
    public bool HasMetadata(string key)
    {
        return Metadata.ContainsKey(key);
    }
}
