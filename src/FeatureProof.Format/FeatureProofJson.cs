using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Encodings.Web;
using FeatureProof.Core;

namespace FeatureProof.Format;

public static class FeatureProofJson
{
    public static JsonSerializerOptions Options { get; } = CreateOptions();

    public static string Serialize(FeatureProofDocument document) =>
        JsonSerializer.Serialize(document, Options);

    public static FeatureProofDocument Deserialize(string content)
    {
        var document = JsonSerializer.Deserialize<FeatureProofDocument>(content, Options)
            ?? throw new FeatureProofFormatException("The file did not contain a feature proof document.");

        var issues = FeatureProofValidation.Validate(document);
        if (issues.Count > 0)
        {
            throw new FeatureProofFormatException(
                "The feature proof file is invalid.",
                issues);
        }

        return document;
    }

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        return options;
    }
}
