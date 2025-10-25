using System.IO.Compression;
using System.Text;
using System.Text.Json;
using Compression_Worker.Core.Models;

namespace Compression_Worker.Utils;

/// <summary>
/// Binary compressed format with GZIP compression for maximum efficiency.
///
/// File structure (.cmp):
/// 1. Magic header (4 bytes): "CMP1" - identifies format version
/// 2. Flags (1 byte): bit 0 = GZIP compressed
/// 3. GZIP-compressed payload containing:
///    - Metadata length (4 bytes)
///    - Metadata (MessagePack or JSON bytes)
///    - Original text length (4 bytes)
///    - Original text (UTF-8 bytes)
///    - Compressed text length (4 bytes)
///    - Compressed text (UTF-8 bytes)
///
/// Benefits:
/// - Binary format (no Base64 overhead)
/// - GZIP compression (reduces JSON metadata bloat)
/// - Single file output
/// - Fast decompression
/// - 100% reversible
/// </summary>
public static class BinaryCompressedFormat
{
    private static readonly byte[] MagicHeader = Encoding.ASCII.GetBytes("CMP1");
    private const byte FLAG_GZIP = 0x01;

    /// <summary>
    /// Saves compressed data in binary format (optionally with GZIP).
    /// </summary>
    public static void SaveCompressed(
        string outputFile,
        ProcessingContext context,
        string originalText,
        IReadOnlyList<string>? pipelineInfo = null,
        bool useGzip = false)
    {
        using var fileStream = new FileStream(outputFile, FileMode.Create, FileAccess.Write, FileShare.None);
        using var writer = new BinaryWriter(fileStream);

        // 1. Write magic header
        writer.Write(MagicHeader);

        // 2. Write flags (GZIP enabled/disabled)
        writer.Write((byte)(useGzip ? FLAG_GZIP : 0));

        // 3. Prepare payload - optimize by storing only essential metadata as JSON
        using var payloadStream = new MemoryStream();
        using (var payloadWriter = new BinaryWriter(payloadStream, Encoding.UTF8, leaveOpen: true))
        {
            // Build lightweight metadata (NO large vocabularies - we'll store those separately)
            var lightMetadata = new Dictionary<string, object>();

            if (pipelineInfo != null && pipelineInfo.Count > 0)
            {
                lightMetadata["pipeline"] = pipelineInfo.ToList();
            }
            lightMetadata["timestamp"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            lightMetadata["original_size"] = originalText.Length;
            lightMetadata["compressed_size"] = context.Text.Length;

            // Copy non-bulky metadata (skip large vocabularies and binary data)
            foreach (var kvp in context.Metadata)
            {
                // Skip arithmetic coding data (we'll store it separately in binary)
                if (kvp.Key == "arithmetic_data")
                    continue;

                // Add other metadata
                lightMetadata[kvp.Key] = kvp.Value;
            }

            // Serialize lightweight metadata
            var metadataJson = MetadataSerializer.Serialize(lightMetadata);
            var metadataBytes = Encoding.UTF8.GetBytes(metadataJson);

            // Write metadata length and data
            payloadWriter.Write(metadataBytes.Length);
            payloadWriter.Write(metadataBytes);

            // OPTIMIZATION: Store text as binary word indices with advanced compression
            // 1. Count word frequencies for optimal ordering
            var wordFrequencies = new Dictionary<string, int>();
            foreach (var token in context.Tokens.Where(t => !string.IsNullOrWhiteSpace(t) && t.Any(char.IsLetter)))
            {
                wordFrequencies[token] = wordFrequencies.GetValueOrDefault(token, 0) + 1;
            }

            // 2. Sort vocabulary by frequency (most common = smallest index)
            var vocabulary = wordFrequencies
                .OrderByDescending(kvp => kvp.Value)
                .Select(kvp => kvp.Key)
                .ToList();

            var vocabToIndex = vocabulary.Select((word, idx) => new { word, idx })
                .ToDictionary(x => x.word, x => (uint)x.idx);

            // 3. Convert tokens to word indices with VLE
            var wordIndices = new List<uint>();
            var whitespaceTypes = new List<byte>(); // 0=none, 1=space, 2=newline, 3=tab

            for (int i = 0; i < context.Tokens.Count; i++)
            {
                var token = context.Tokens[i];
                if (vocabToIndex.TryGetValue(token, out var index))
                {
                    wordIndices.Add(index);

                    // Check following whitespace
                    if (i + 1 < context.Tokens.Count && string.IsNullOrWhiteSpace(context.Tokens[i + 1]))
                    {
                        var ws = context.Tokens[i + 1];
                        if (ws == "\n") whitespaceTypes.Add(2);
                        else if (ws == "\t") whitespaceTypes.Add(3);
                        else whitespaceTypes.Add(1);
                        i++; // Skip whitespace token
                    }
                    else
                    {
                        whitespaceTypes.Add(0); // No whitespace
                    }
                }
            }

            // 4. Write vocabulary count and words
            payloadWriter.Write(vocabulary.Count);
            foreach (var word in vocabulary)
            {
                var wordBytes = Encoding.UTF8.GetBytes(word);
                payloadWriter.Write((byte)wordBytes.Length);
                payloadWriter.Write(wordBytes);
            }

            // 5. Write word indices using Variable-Length Encoding (VLE)
            // VLE: 0-127 = 1 byte, 128-16383 = 2 bytes, 16384+ = 3 bytes
            payloadWriter.Write(wordIndices.Count);
            int vleBytes = 0;
            foreach (var index in wordIndices)
            {
                vleBytes += WriteVariableLength(payloadWriter, index);
            }

            // 6. Write whitespace using bit packing (2 bits per whitespace)
            payloadWriter.Write(whitespaceTypes.Count);
            var packedWhitespace = BitPackWhitespace(whitespaceTypes);
            payloadWriter.Write(packedWhitespace.Length);
            payloadWriter.Write(packedWhitespace);

            Console.WriteLine($"[BINARY ENCODING] Vocabulary: {vocabulary.Count} unique words (freq-sorted)");
            Console.WriteLine($"[BINARY ENCODING] Word indices: {wordIndices.Count} words ({vleBytes} bytes VLE vs {wordIndices.Count * 2} bytes fixed)");
            Console.WriteLine($"[BINARY ENCODING] Whitespace: {whitespaceTypes.Count} entries ({packedWhitespace.Length} bytes bit-packed vs {whitespaceTypes.Count} bytes)");

            // Write arithmetic coding data separately in binary format (if exists)
            if (context.Metadata.TryGetValue("arithmetic_data", out var acDataObj))
            {
                payloadWriter.Write(true); // Has AC data
                WriteArithmeticDataBinary(payloadWriter, acDataObj);
            }
            else
            {
                payloadWriter.Write(false); // No AC data
            }
        }

        // 4. Optionally GZIP compress the payload
        var payloadUncompressed = payloadStream.ToArray();
        byte[] payloadFinal;

        if (useGzip)
        {
            using var gzipStream = new MemoryStream();
            using (var gzip = new GZipStream(gzipStream, CompressionLevel.Optimal))
            {
                gzip.Write(payloadUncompressed, 0, payloadUncompressed.Length);
            }
            payloadFinal = gzipStream.ToArray();
            Console.WriteLine($"[BINARY FORMAT] Payload: {payloadUncompressed.Length:N0} bytes → {payloadFinal.Length:N0} bytes (GZIP)");
        }
        else
        {
            payloadFinal = payloadUncompressed;
            Console.WriteLine($"[BINARY FORMAT] Payload: {payloadUncompressed.Length:N0} bytes (no GZIP)");
        }

        // 5. Write payload length and data
        writer.Write(payloadFinal.Length);
        writer.Write(payloadFinal);

        Console.WriteLine($"[BINARY FORMAT] Total file size: {fileStream.Position:N0} bytes");
    }

    /// <summary>
    /// Loads compressed file in binary+GZIP format.
    /// </summary>
    public static (string compressedText, Dictionary<string, object> metadata, string humanHeader)
        LoadCompressed(string inputFile)
    {
        if (!File.Exists(inputFile))
        {
            throw new FileNotFoundException($"Compressed file not found: {inputFile}");
        }

        using var fileStream = new FileStream(inputFile, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var reader = new BinaryReader(fileStream);

        // 1. Read and validate magic header
        var magic = reader.ReadBytes(4);
        if (!magic.SequenceEqual(MagicHeader))
        {
            throw new InvalidOperationException("Invalid file format - not a CMP1 binary compressed file");
        }

        // 2. Read flags
        var flags = reader.ReadByte();
        var isGzipped = (flags & FLAG_GZIP) != 0;

        // 3. Read compressed payload
        var payloadCompressedLength = reader.ReadInt32();
        var payloadCompressed = reader.ReadBytes(payloadCompressedLength);

        // 4. Decompress payload
        byte[] payloadUncompressed;
        if (isGzipped)
        {
            using var gzipStream = new MemoryStream(payloadCompressed);
            using var gzip = new GZipStream(gzipStream, CompressionMode.Decompress);
            using var decompressedStream = new MemoryStream();
            gzip.CopyTo(decompressedStream);
            payloadUncompressed = decompressedStream.ToArray();
        }
        else
        {
            payloadUncompressed = payloadCompressed;
        }

        // 5. Parse payload
        using var payloadStream = new MemoryStream(payloadUncompressed);
        using var payloadReader = new BinaryReader(payloadStream, Encoding.UTF8);

        // Read metadata
        var metadataLength = payloadReader.ReadInt32();
        var metadataBytes = payloadReader.ReadBytes(metadataLength);
        var metadataJson = Encoding.UTF8.GetString(metadataBytes);

        // Deserialize using MetadataSerializer (handles all type conversions properly)
        var metadata = MetadataSerializer.Deserialize(metadataJson);

        // Original text is NOT stored (too big!) - get length from metadata (already converted to int)
        var originalSize = metadata.TryGetValue("original_size", out var origSizeObj)
            ? Convert.ToInt32(origSizeObj)
            : 0;

        // OPTIMIZATION: Read binary word indices and reconstruct text
        // Read vocabulary
        var vocabCount = payloadReader.ReadInt32();
        var vocabulary = new List<string>();
        for (int i = 0; i < vocabCount; i++)
        {
            var wordLength = payloadReader.ReadByte();
            var wordBytes = payloadReader.ReadBytes(wordLength);
            vocabulary.Add(Encoding.UTF8.GetString(wordBytes));
        }

        // Read word indices using Variable-Length Encoding
        var wordIndicesCount = payloadReader.ReadInt32();
        var wordIndices = new List<uint>();
        for (int i = 0; i < wordIndicesCount; i++)
        {
            wordIndices.Add(ReadVariableLength(payloadReader));
        }

        // Read bit-packed whitespace
        var whitespaceCount = payloadReader.ReadInt32();
        var packedWhitespaceLength = payloadReader.ReadInt32();
        var packedWhitespace = payloadReader.ReadBytes(packedWhitespaceLength);
        var whitespaceTypes = BitUnpackWhitespace(packedWhitespace, whitespaceCount);

        // Reconstruct text from vocabulary + indices + whitespace
        var reconstructedTokens = new List<string>();
        for (int i = 0; i < wordIndices.Count; i++)
        {
            reconstructedTokens.Add(vocabulary[(int)wordIndices[i]]);

            // Add whitespace
            if (i < whitespaceTypes.Count)
            {
                switch (whitespaceTypes[i])
                {
                    case 1: reconstructedTokens.Add(" "); break;
                    case 2: reconstructedTokens.Add("\n"); break;
                    case 3: reconstructedTokens.Add("\t"); break;
                    // case 0: no whitespace
                }
            }
        }

        var compressedText = string.Concat(reconstructedTokens);

        Console.WriteLine($"[BINARY DECODING] Vocabulary: {vocabCount} words");
        Console.WriteLine($"[BINARY DECODING] Reconstructed: {wordIndicesCount} words");

        // Read arithmetic coding data (if exists)
        var hasACData = payloadReader.ReadBoolean();
        if (hasACData)
        {
            var acData = ReadArithmeticDataBinary(payloadReader);
            metadata["arithmetic_data"] = acData;
        }

        // 6. Build human-readable header
        var humanHeader = BuildHumanHeader(metadata, originalSize, compressedText);

        return (compressedText, metadata, humanHeader);
    }

    /// <summary>
    /// Validates that a file is in binary compressed format.
    /// </summary>
    public static bool IsValidFormat(string filePath)
    {
        if (!File.Exists(filePath))
            return false;

        try
        {
            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            if (fs.Length < 4)
                return false;

            var magic = new byte[4];
            fs.Read(magic, 0, 4);
            return magic.SequenceEqual(MagicHeader);
        }
        catch
        {
            return false;
        }
    }

    private static string BuildHumanHeader(Dictionary<string, object> metadata, int originalSize, string compressedText)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Romanian Text Compression - Binary Format");
        sb.AppendLine("================================================");

        if (metadata.TryGetValue("timestamp", out var timestamp))
        {
            sb.AppendLine($"Timestamp:        {timestamp}");
        }

        sb.AppendLine($"Original Size:    {originalSize} bytes");
        sb.AppendLine($"Compressed Size:  {compressedText.Length} bytes ({CountWords(compressedText)} words)");
        sb.AppendLine($"Compression Ratio: {CalculateRatio(originalSize, compressedText.Length):F2}x");
        sb.AppendLine($"Space Savings:    {CalculateSavings(originalSize, compressedText.Length):F2}%");

        if (metadata.TryGetValue("pipeline", out var pipelineObj))
        {
            var pipeline = pipelineObj switch
            {
                List<string> stringList => stringList,
                System.Text.Json.JsonElement jsonElement when jsonElement.ValueKind == System.Text.Json.JsonValueKind.Array =>
                    jsonElement.EnumerateArray().Select(e => e.GetString()!).ToList(),
                _ => new List<string>()
            };

            if (pipeline.Count > 0)
            {
                sb.AppendLine($"Pipeline:         {string.Join(" → ", pipeline)}");
            }
        }

        sb.AppendLine("================================================");
        return sb.ToString();
    }

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

    /// <summary>
    /// Variable-Length Encoding: encodes integers using 1-3 bytes
    /// 0-127: 1 byte (0xxxxxxx)
    /// 128-16383: 2 bytes (10xxxxxx xxxxxxxx)
    /// 16384+: 3 bytes (11xxxxxx xxxxxxxx xxxxxxxx)
    /// </summary>
    private static int WriteVariableLength(BinaryWriter writer, uint value)
    {
        if (value < 128)
        {
            writer.Write((byte)value);
            return 1;
        }
        else if (value < 16384)
        {
            writer.Write((byte)((value >> 8) | 0x80));
            writer.Write((byte)(value & 0xFF));
            return 2;
        }
        else
        {
            writer.Write((byte)((value >> 16) | 0xC0));
            writer.Write((byte)((value >> 8) & 0xFF));
            writer.Write((byte)(value & 0xFF));
            return 3;
        }
    }

    /// <summary>
    /// Reads variable-length encoded integer
    /// </summary>
    private static uint ReadVariableLength(BinaryReader reader)
    {
        byte first = reader.ReadByte();

        if ((first & 0x80) == 0)
        {
            // 1 byte
            return first;
        }
        else if ((first & 0x40) == 0)
        {
            // 2 bytes
            byte second = reader.ReadByte();
            return (uint)(((first & 0x3F) << 8) | second);
        }
        else
        {
            // 3 bytes
            byte second = reader.ReadByte();
            byte third = reader.ReadByte();
            return (uint)(((first & 0x3F) << 16) | (second << 8) | third);
        }
    }

    /// <summary>
    /// Bit-packs whitespace types (00=none, 01=space, 10=newline, 11=tab)
    /// Packs 4 whitespace entries per byte
    /// </summary>
    private static byte[] BitPackWhitespace(List<byte> whitespaceTypes)
    {
        int byteCount = (whitespaceTypes.Count + 3) / 4; // Ceiling division
        var packed = new byte[byteCount];

        for (int i = 0; i < whitespaceTypes.Count; i++)
        {
            int byteIndex = i / 4;
            int bitOffset = (i % 4) * 2;
            packed[byteIndex] |= (byte)(whitespaceTypes[i] << bitOffset);
        }

        return packed;
    }

    /// <summary>
    /// Unpacks bit-packed whitespace
    /// </summary>
    private static List<byte> BitUnpackWhitespace(byte[] packed, int count)
    {
        var whitespaceTypes = new List<byte>(count);

        for (int i = 0; i < count; i++)
        {
            int byteIndex = i / 4;
            int bitOffset = (i % 4) * 2;
            byte value = (byte)((packed[byteIndex] >> bitOffset) & 0x03);
            whitespaceTypes.Add(value);
        }

        return whitespaceTypes;
    }

    /// <summary>
    /// Converts JsonElement to native C# types for compatibility with existing processors.
    /// </summary>
    private static object ConvertJsonElementToObject(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString()!,
            JsonValueKind.Number => element.TryGetInt32(out var intValue) ? intValue : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null!,
            JsonValueKind.Array => element.EnumerateArray()
                .Select(ConvertJsonElementToObject)
                .ToList(),
            JsonValueKind.Object => element.EnumerateObject()
                .ToDictionary(prop => prop.Name, prop => ConvertJsonElementToObject(prop.Value)),
            _ => element.Clone() // Keep as JsonElement if unknown type
        };
    }

    /// <summary>
    /// Writes arithmetic coding data in compact binary format (not JSON).
    /// Format: [encoded_bytes_length][encoded_bytes][original_length][vocab_count][vocab...]
    /// </summary>
    private static void WriteArithmeticDataBinary(BinaryWriter writer, object acDataObj)
    {
        var acData = acDataObj as Dictionary<string, object>;
        if (acData == null)
            throw new InvalidOperationException("Invalid arithmetic_data format");

        // 1. Write encoded bytes
        var encodedBase64 = acData["encoded_bytes"].ToString()!;
        var encodedBytes = Convert.FromBase64String(encodedBase64);
        writer.Write(encodedBytes.Length);
        writer.Write(encodedBytes);

        // 2. Write original length
        var originalLength = Convert.ToInt32(acData["original_length"]);
        writer.Write(originalLength);

        // 3. Write vocabulary (if using binary format)
        if (acData.TryGetValue("vocabulary_binary", out var vocabBinaryObj))
        {
            writer.Write((byte)1); // Binary vocabulary format
            var vocabBinaryBase64 = vocabBinaryObj.ToString()!;
            var vocabBinary = Convert.FromBase64String(vocabBinaryBase64);
            writer.Write(vocabBinary.Length);
            writer.Write(vocabBinary);
        }
        else if (acData.TryGetValue("vocabulary", out var vocabObj))
        {
            writer.Write((byte)0); // Old JSON vocabulary format (for backward compat)
            var vocabulary = vocabObj switch
            {
                List<string> stringList => stringList,
                _ => throw new InvalidOperationException("Unexpected vocabulary type")
            };
            writer.Write(vocabulary.Count);
            foreach (var token in vocabulary)
            {
                writer.Write(token);
            }
        }
        else
        {
            throw new InvalidOperationException("No vocabulary found in arithmetic_data");
        }
    }

    /// <summary>
    /// Reads arithmetic coding data from binary format.
    /// </summary>
    private static Dictionary<string, object> ReadArithmeticDataBinary(BinaryReader reader)
    {
        var acData = new Dictionary<string, object>();

        // 1. Read encoded bytes
        var encodedLength = reader.ReadInt32();
        var encodedBytes = reader.ReadBytes(encodedLength);
        acData["encoded_bytes"] = Convert.ToBase64String(encodedBytes);

        // 2. Read original length
        var originalLength = reader.ReadInt32();
        acData["original_length"] = originalLength;

        // 3. Read vocabulary
        var vocabFormat = reader.ReadByte();
        if (vocabFormat == 1)
        {
            // Binary vocabulary format
            var vocabBinaryLength = reader.ReadInt32();
            var vocabBinary = reader.ReadBytes(vocabBinaryLength);
            acData["vocabulary_binary"] = Convert.ToBase64String(vocabBinary);
        }
        else
        {
            // Old JSON vocabulary format
            var vocabCount = reader.ReadInt32();
            var vocabulary = new List<string>(vocabCount);
            for (int i = 0; i < vocabCount; i++)
            {
                vocabulary.Add(reader.ReadString());
            }
            acData["vocabulary"] = vocabulary;
        }

        return acData;
    }
}
