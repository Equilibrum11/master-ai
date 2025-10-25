using System.Text.Json;

namespace Compression_Worker.Utils;

/// <summary>
/// Handles serialization and deserialization of ProcessingContext metadata.
/// Preserves type information for proper restoration.
/// </summary>
public static class MetadataSerializer
{
    /// <summary>
    /// Serializes metadata to JSON with type information preserved.
    /// </summary>
    public static string Serialize(Dictionary<string, object> metadata)
    {
        var typedMetadata = new Dictionary<string, TypedValue>();

        foreach (var kvp in metadata)
        {
            typedMetadata[kvp.Key] = new TypedValue
            {
                TypeName = kvp.Value.GetType().AssemblyQualifiedName!,
                Value = JsonSerializer.Serialize(kvp.Value)
            };
        }

        return JsonSerializer.Serialize(typedMetadata, new JsonSerializerOptions
        {
            WriteIndented = true
        });
    }

    /// <summary>
    /// Deserializes metadata from JSON, restoring original types.
    /// </summary>
    public static Dictionary<string, object> Deserialize(string json)
    {
        var typedMetadata = JsonSerializer.Deserialize<Dictionary<string, TypedValue>>(json);
        if (typedMetadata == null)
            return new Dictionary<string, object>();

        var metadata = new Dictionary<string, object>();

        foreach (var kvp in typedMetadata)
        {
            var type = Type.GetType(kvp.Value.TypeName);
            if (type == null)
            {
                throw new InvalidOperationException(
                    $"Cannot deserialize metadata key '{kvp.Key}': type '{kvp.Value.TypeName}' not found");
            }

            var value = JsonSerializer.Deserialize(kvp.Value.Value, type);
            if (value != null)
            {
                metadata[kvp.Key] = value;
            }
        }

        return metadata;
    }

    /// <summary>
    /// Internal structure to store value with type information.
    /// </summary>
    private class TypedValue
    {
        public string TypeName { get; set; } = "";
        public string Value { get; set; } = "";
    }
}
