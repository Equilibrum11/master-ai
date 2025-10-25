using System.Text;
using System.Text.Json;

namespace Compression_Worker.Utils;

/// <summary>
/// Handles self-contained compression format that includes both binary metadata
/// and human-readable header in a single file.
///
/// Format structure:
/// 1. Binary metadata section (base64 encoded for text compatibility)
/// 2. Human-readable header with statistics
/// 3. Compressed text
///
/// Benefits:
/// - Single file output (no separate .meta file needed)
/// - Binary metadata for efficiency
/// - Human-readable header for debugging
/// - 100% reversible
/// </summary>
public static class SelfContainedFormat
{
    private const string HEADER_BIN_START = "===COMPRESSION-HEADER-BIN-START===";
    private const string HEADER_BIN_END = "===COMPRESSION-HEADER-BIN-END===";
    private const string HEADER_TEXT_START = "===COMPRESSION-HEADER-TEXT-START===";
    private const string HEADER_TEXT_END = "===COMPRESSION-HEADER-TEXT-END===";
    private const string COMPRESSED_START = "===COMPRESSED-TEXT-START===";
    private const string COMPRESSED_END = "===COMPRESSED-TEXT-END===";

    /// <summary>
    /// Saves compressed text with metadata in self-contained format.
    /// </summary>
    /// <param name="outputFile">Path to output file (.cmp extension recommended)</param>
    /// <param name="context">Processing context with metadata</param>
    /// <param name="originalText">Original uncompressed text (for statistics)</param>
    /// <param name="pipelineInfo">Optional: names of processors used</param>
    public static void SaveCompressed(
        string outputFile,
        Core.Models.ProcessingContext context,
        string originalText,
        IReadOnlyList<string>? pipelineInfo = null)
    {
        var sb = new StringBuilder();

        // 1. Binary metadata section (base64 encoded)
        sb.AppendLine(HEADER_BIN_START);
        var metadataJson = MetadataSerializer.Serialize(context.Metadata);
        var metadataBytes = Encoding.UTF8.GetBytes(metadataJson);
        sb.AppendLine(Convert.ToBase64String(metadataBytes));
        sb.AppendLine(HEADER_BIN_END);

        // 2. Human-readable header
        sb.AppendLine(HEADER_TEXT_START);
        sb.AppendLine("Romanian Text Compression - Self-Contained Format");
        sb.AppendLine("================================================");
        sb.AppendLine($"Timestamp:        {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"Original Size:    {originalText.Length} bytes ({CountWords(originalText)} words)");
        sb.AppendLine($"Compressed Size:  {context.Text.Length} bytes ({CountWords(context.Text)} words)");
        sb.AppendLine($"Compression Ratio: {CalculateRatio(originalText.Length, context.Text.Length):F2}x");
        sb.AppendLine($"Space Savings:    {CalculateSavings(originalText.Length, context.Text.Length):F2}%");

        if (pipelineInfo != null && pipelineInfo.Count > 0)
        {
            sb.AppendLine($"Pipeline:         {string.Join(" → ", pipelineInfo)}");
        }

        sb.AppendLine("================================================");
        sb.AppendLine(HEADER_TEXT_END);

        // 3. Compressed text
        sb.AppendLine(COMPRESSED_START);
        sb.Append(context.Text);
        if (!context.Text.EndsWith('\n'))
        {
            sb.AppendLine();
        }
        sb.AppendLine(COMPRESSED_END);

        // Write with FileShare to avoid conflicts
        using (var stream = new FileStream(outputFile, FileMode.Create, FileAccess.Write, FileShare.None))
        using (var writer = new StreamWriter(stream, Encoding.UTF8))
        {
            writer.Write(sb.ToString());
        }
    }

    /// <summary>
    /// Loads compressed file and extracts all components.
    /// </summary>
    /// <param name="inputFile">Path to compressed file</param>
    /// <returns>Tuple of (compressed text, metadata dictionary, human-readable header)</returns>
    public static (string compressedText, Dictionary<string, object> metadata, string humanHeader)
        LoadCompressed(string inputFile)
    {
        if (!File.Exists(inputFile))
        {
            throw new FileNotFoundException($"Compressed file not found: {inputFile}");
        }

        // Read with FileShare to avoid conflicts
        string content;
        using (var stream = new FileStream(inputFile, FileMode.Open, FileAccess.Read, FileShare.Read))
        using (var reader = new StreamReader(stream, Encoding.UTF8))
        {
            content = reader.ReadToEnd();
        }
        var lines = content.Split('\n');

        // Extract sections
        string? metadataBase64 = null;
        StringBuilder humanHeaderBuilder = new();
        StringBuilder compressedTextBuilder = new();

        string currentSection = "";

        foreach (var line in lines)
        {
            var trimmedLine = line.TrimEnd('\r');

            // Section markers
            if (trimmedLine == HEADER_BIN_START)
            {
                currentSection = "metadata_bin";
                continue;
            }
            else if (trimmedLine == HEADER_BIN_END)
            {
                currentSection = "";
                continue;
            }
            else if (trimmedLine == HEADER_TEXT_START)
            {
                currentSection = "header_text";
                continue;
            }
            else if (trimmedLine == HEADER_TEXT_END)
            {
                currentSection = "";
                continue;
            }
            else if (trimmedLine == COMPRESSED_START)
            {
                currentSection = "compressed";
                continue;
            }
            else if (trimmedLine == COMPRESSED_END)
            {
                currentSection = "";
                continue;
            }

            // Content extraction
            switch (currentSection)
            {
                case "metadata_bin":
                    metadataBase64 = trimmedLine;
                    break;

                case "header_text":
                    humanHeaderBuilder.AppendLine(trimmedLine);
                    break;

                case "compressed":
                    compressedTextBuilder.AppendLine(trimmedLine);
                    break;
            }
        }

        // Decode metadata
        if (string.IsNullOrWhiteSpace(metadataBase64))
        {
            throw new InvalidOperationException("No metadata found in compressed file");
        }

        var metadataBytes = Convert.FromBase64String(metadataBase64);
        var metadataJson = Encoding.UTF8.GetString(metadataBytes);
        var metadata = MetadataSerializer.Deserialize(metadataJson);

        // Clean up compressed text (remove trailing newline from builder)
        var compressedText = compressedTextBuilder.ToString().TrimEnd('\r', '\n');

        return (compressedText, metadata, humanHeaderBuilder.ToString().TrimEnd());
    }

    /// <summary>
    /// Validates that a file is in self-contained format.
    /// </summary>
    public static bool IsValidFormat(string filePath)
    {
        if (!File.Exists(filePath))
            return false;

        try
        {
            string content;
            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var reader = new StreamReader(stream, Encoding.UTF8))
            {
                content = reader.ReadToEnd();
            }
            return content.Contains(HEADER_BIN_START) &&
                   content.Contains(HEADER_BIN_END) &&
                   content.Contains(COMPRESSED_START) &&
                   content.Contains(COMPRESSED_END);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Extracts just the human-readable header without loading full file.
    /// Useful for quick inspection.
    /// </summary>
    public static string? GetHumanHeader(string filePath)
    {
        if (!File.Exists(filePath))
            return null;

        try
        {
            string[] lines;
            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var reader = new StreamReader(stream, Encoding.UTF8))
            {
                lines = reader.ReadToEnd().Split('\n');
            }
            var headerBuilder = new StringBuilder();
            bool inHeader = false;

            foreach (var line in lines)
            {
                if (line.Trim() == HEADER_TEXT_START)
                {
                    inHeader = true;
                    continue;
                }
                else if (line.Trim() == HEADER_TEXT_END)
                {
                    break;
                }

                if (inHeader)
                {
                    headerBuilder.AppendLine(line);
                }
            }

            return headerBuilder.ToString().TrimEnd();
        }
        catch
        {
            return null;
        }
    }

    // Helper methods
    private static double CalculateRatio(int originalSize, int compressedSize)
    {
        return compressedSize == 0 ? 0 : (double)originalSize / compressedSize;
    }

    private static double CalculateSavings(int originalSize, int compressedSize)
    {
        return originalSize == 0 ? 0 : (1 - (double)compressedSize / originalSize) * 100;
    }

    private static int CountWords(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 0;

        return text.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;
    }
}
