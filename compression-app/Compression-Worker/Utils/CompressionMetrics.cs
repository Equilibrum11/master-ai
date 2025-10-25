namespace Compression_Worker.Utils;

/// <summary>
/// Utility class for calculating compression metrics and statistics.
/// Provides insights into compression effectiveness based on information theory.
/// </summary>
public static class CompressionMetrics
{
    /// <summary>
    /// Calculates the compression ratio (original size / compressed size).
    /// Higher values indicate better compression.
    /// </summary>
    public static double CalculateCompressionRatio(int originalSize, int compressedSize)
    {
        if (compressedSize == 0)
            return 0;

        return (double)originalSize / compressedSize;
    }

    /// <summary>
    /// Calculates space savings as a percentage.
    /// Formula: (1 - compressed/original) × 100%
    /// </summary>
    public static double CalculateSpaceSavings(int originalSize, int compressedSize)
    {
        if (originalSize == 0)
            return 0;

        return (1 - (double)compressedSize / originalSize) * 100;
    }

    /// <summary>
    /// Calculates Shannon entropy of text (information content).
    /// Formula: H(X) = -Σ p(xi) × log2(p(xi))
    /// Measures average information per character in bits.
    /// </summary>
    public static double CalculateEntropy(string text)
    {
        if (string.IsNullOrEmpty(text))
            return 0;

        // Calculate character frequency
        var frequency = new Dictionary<char, int>();
        foreach (char c in text)
        {
            frequency[c] = frequency.GetValueOrDefault(c, 0) + 1;
        }

        // Calculate entropy
        double entropy = 0;
        int totalChars = text.Length;

        foreach (var kvp in frequency)
        {
            double probability = (double)kvp.Value / totalChars;
            entropy -= probability * Math.Log2(probability);
        }

        return entropy;
    }

    /// <summary>
    /// Generates a comprehensive compression report.
    /// </summary>
    public static CompressionReport GenerateReport(string originalText, string compressedText)
    {
        int originalSize = originalText.Length;
        int compressedSize = compressedText.Length;

        return new CompressionReport
        {
            OriginalSize = originalSize,
            CompressedSize = compressedSize,
            CompressionRatio = CalculateCompressionRatio(originalSize, compressedSize),
            SpaceSavings = CalculateSpaceSavings(originalSize, compressedSize),
            OriginalEntropy = CalculateEntropy(originalText),
            CompressedEntropy = CalculateEntropy(compressedText),
            OriginalWordCount = CountWords(originalText),
            CompressedWordCount = CountWords(compressedText)
        };
    }

    private static int CountWords(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 0;

        return text.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;
    }
}

/// <summary>
/// Record containing compression statistics and metrics.
/// </summary>
public record CompressionReport
{
    public int OriginalSize { get; init; }
    public int CompressedSize { get; init; }
    public double CompressionRatio { get; init; }
    public double SpaceSavings { get; init; }
    public double OriginalEntropy { get; init; }
    public double CompressedEntropy { get; init; }
    public int OriginalWordCount { get; init; }
    public int CompressedWordCount { get; init; }

    public override string ToString()
    {
        return $"""
                === Compression Report ===
                Original Size:      {OriginalSize} characters ({OriginalWordCount} words)
                Compressed Size:    {CompressedSize} characters ({CompressedWordCount} words)
                Compression Ratio:  {CompressionRatio:F2}x
                Space Savings:      {SpaceSavings:F2}%
                Original Entropy:   {OriginalEntropy:F2} bits/char
                Compressed Entropy: {CompressedEntropy:F2} bits/char
                """;
    }
}
