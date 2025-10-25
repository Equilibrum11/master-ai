using System.Text;
using Compression_Worker.Core.Models;
using Compression_Worker.Data;
using Compression_Worker.Processors.Base;
using RT.ArithmeticCoding;

namespace Compression_Worker.Processors.Statistical;

/// <summary>
/// Arithmetic coding processor using RT.ArithmeticCoding library.
/// Phase 3: Statistical Compression - Step 2 (Final)
///
/// Encodes tokens using uniform probabilities into a compact binary representation.
/// Achieves better compression than Huffman by using fractional bits.
///
/// Reversible: 100% - stores original token count and vocabulary for decoding.
/// </summary>
public class ArithmeticCodingProcessor : ReversibleProcessorBase
{
    private const string MetadataKey = "arithmetic_data";

    protected override ProcessingContext ProcessCore(ProcessingContext context)
    {
        var tokens = context.Tokens;

        // Step 1: Build vocabulary (map tokens to symbol IDs)
        var vocabulary = BuildVocabulary(tokens);
        var tokenToId = vocabulary.TokenToId;
        var idToToken = vocabulary.IdToToken;

        Console.WriteLine($"[AC-ENCODE] Encoding {tokens.Count} tokens with vocabulary size {vocabulary.Size}");

        // Step 2: Create uniform frequency distribution
        var frequencies = new uint[vocabulary.Size];
        for (int i = 0; i < vocabulary.Size; i++)
        {
            frequencies[i] = 1; // Uniform probability for all symbols
        }

        // Step 3: Encode using RT.ArithmeticCoding
        using var memStream = new MemoryStream();
        var writer = new ArithmeticCodingWriter(memStream, frequencies);

        foreach (var token in tokens)
        {
            var symbolId = tokenToId[token];
            writer.WriteSymbol(symbolId);
        }

        writer.Finalize(false);

        var encoded = memStream.ToArray();
        Console.WriteLine($"[AC-ENCODE] Encoded to {encoded.Length} bytes");

        // Step 4: Store metadata with BINARY-PACKED vocabulary (much smaller than JSON)
        var vocabBinary = PackVocabularyToBinary(vocabulary.TokenToId.Keys.ToList());

        var arithmeticData = new Dictionary<string, object>
        {
            ["encoded_bytes"] = Convert.ToBase64String(encoded),
            ["original_length"] = tokens.Count,
            ["vocabulary_binary"] = Convert.ToBase64String(vocabBinary),
            ["vocabulary_count"] = vocabulary.Size
        };

        Console.WriteLine($"[AC-ENCODE] Vocabulary: {vocabulary.Size} unique tokens, packed to {vocabBinary.Length:N0} bytes (vs {vocabulary.Size * 10:N0} bytes JSON est.)");

        context.SetMetadata(MetadataKey, arithmeticData);

        // Create visual representation of encoded data for UI display
        var encodedHex = BitConverter.ToString(encoded).Replace("-", " ");
        var encodedBase64 = Convert.ToBase64String(encoded);

        // Build descriptive text showing the actual encoded data
        var displayText = $"[Arithmetic Coding Output]\n" +
                         $"Encoded: {tokens.Count} tokens → {encoded.Length} bytes\n" +
                         $"Vocabulary: {vocabulary.Size} unique tokens\n" +
                         $"Compression: {tokens.Count * 4} bytes (tokens) → {encoded.Length} bytes (est. {((double)encoded.Length / (tokens.Count * 4)) * 100:F1}% size)\n\n" +
                         $"Binary (hex): {(encodedHex.Length > 200 ? encodedHex.Substring(0, 200) + "..." : encodedHex)}\n\n" +
                         $"Base64: {(encodedBase64.Length > 200 ? encodedBase64.Substring(0, 200) + "..." : encodedBase64)}";

        // Set tokens to make the display text appear in UI
        context.Tokens = new List<string> { displayText };

        return context;
    }

    protected override ProcessingContext ReverseCore(ProcessingContext context)
    {
        // Step 1: Get encoded data
        var arithmeticData = context.GetMetadata<Dictionary<string, object>>(MetadataKey);
        if (arithmeticData == null)
            return context;

        var encodedBase64 = arithmeticData["encoded_bytes"].ToString()!;
        var encoded = Convert.FromBase64String(encodedBase64);

        var originalLength = arithmeticData["original_length"] switch
        {
            int intValue => intValue,
            System.Text.Json.JsonElement jsonElement when jsonElement.ValueKind == System.Text.Json.JsonValueKind.Number =>
                jsonElement.GetInt32(),
            var obj => Convert.ToInt32(obj)
        };

        // Get vocabulary (try binary format first, fallback to old JSON format for compatibility)
        List<string> vocabularyTokens;
        if (arithmeticData.TryGetValue("vocabulary_binary", out var vocabBinaryObj))
        {
            // New binary format
            var vocabBinaryBase64 = vocabBinaryObj.ToString()!;
            var vocabBinary = Convert.FromBase64String(vocabBinaryBase64);
            vocabularyTokens = UnpackVocabularyFromBinary(vocabBinary);
        }
        else
        {
            // Old JSON array format (for backward compatibility)
            var vocabObj = arithmeticData["vocabulary"];
            vocabularyTokens = vocabObj switch
            {
                List<string> stringList => stringList,
                System.Text.Json.JsonElement jsonElement when jsonElement.ValueKind == System.Text.Json.JsonValueKind.Array =>
                    jsonElement.EnumerateArray().Select(e => e.GetString()!).ToList(),
                System.Collections.IEnumerable enumerable => enumerable.Cast<object>().Select(o => o.ToString()!).ToList(),
                _ => throw new InvalidOperationException($"Unexpected vocabulary type: {vocabObj.GetType()}")
            };
        }

        var idToToken = vocabularyTokens.Select((token, id) => (token, id)).ToDictionary(x => x.id, x => x.token);

        Console.WriteLine($"[AC-DECODE] Decoding {originalLength} tokens from {encoded.Length} bytes");

        // Step 2: Create same uniform frequency distribution as encoding
        var frequencies = new uint[vocabularyTokens.Count];
        for (int i = 0; i < vocabularyTokens.Count; i++)
        {
            frequencies[i] = 1; // Uniform probability for all symbols
        }

        // Step 3: Decode using RT.ArithmeticCoding
        using var memStream = new MemoryStream(encoded);
        var reader = new ArithmeticCodingReader(memStream, frequencies);

        var decodedTokens = new List<string>();
        for (int i = 0; i < originalLength; i++)
        {
            int symbolId = reader.ReadSymbol();
            var token = idToToken[symbolId];
            decodedTokens.Add(token);
        }

        Console.WriteLine($"[AC-DECODE] Successfully decoded {decodedTokens.Count} tokens");
        Console.WriteLine($"[AC-DECODE] First 10 decoded: {string.Join(", ", decodedTokens.Take(10).Select(t => $"\"{t}\""))}");

        context.Tokens = decodedTokens;

        return context;
    }

    private (Dictionary<string, int> TokenToId, Dictionary<int, string> IdToToken, int Size) BuildVocabulary(
        List<string> tokens)
    {
        // Build vocabulary from unique tokens
        var uniqueTokens = tokens.Distinct().OrderBy(t => t).ToList();

        var tokenToId = uniqueTokens.Select((token, id) => (token, id)).ToDictionary(x => x.token, x => x.id);
        var idToToken = uniqueTokens.Select((token, id) => (token, id)).ToDictionary(x => x.id, x => x.token);

        return (tokenToId, idToToken, uniqueTokens.Count);
    }

    /// <summary>
    /// Packs vocabulary into compact binary format: [count:4 bytes][length1:2 bytes][string1][length2:2 bytes][string2]...
    /// Much more efficient than JSON array for large vocabularies.
    /// </summary>
    private byte[] PackVocabularyToBinary(List<string> vocabulary)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms, Encoding.UTF8);

        // Write count
        writer.Write(vocabulary.Count);

        // Write each string with length prefix
        foreach (var token in vocabulary)
        {
            var tokenBytes = Encoding.UTF8.GetBytes(token);
            writer.Write((ushort)tokenBytes.Length); // 2 bytes for length (max 65535)
            writer.Write(tokenBytes);
        }

        return ms.ToArray();
    }

    /// <summary>
    /// Unpacks vocabulary from binary format.
    /// </summary>
    private List<string> UnpackVocabularyFromBinary(byte[] vocabBinary)
    {
        using var ms = new MemoryStream(vocabBinary);
        using var reader = new BinaryReader(ms, Encoding.UTF8);

        var count = reader.ReadInt32();
        var vocabulary = new List<string>(count);

        for (int i = 0; i < count; i++)
        {
            var length = reader.ReadUInt16();
            var tokenBytes = reader.ReadBytes(length);
            var token = Encoding.UTF8.GetString(tokenBytes);
            vocabulary.Add(token);
        }

        return vocabulary;
    }
}
