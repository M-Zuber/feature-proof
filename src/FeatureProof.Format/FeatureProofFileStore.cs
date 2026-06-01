using FeatureProof.Core;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace FeatureProof.Format;

public sealed class FeatureProofFileStore
{
    public async Task<FeatureProofDocument> LoadAsync(string path, CancellationToken cancellationToken = default)
    {
        var content = await File.ReadAllTextAsync(path, cancellationToken);
        return FeatureProofJson.Deserialize(content);
    }

    public async Task SaveAsync(
        string path,
        FeatureProofDocument document,
        CancellationToken cancellationToken = default)
    {
        var issues = FeatureProofValidation.Validate(document);
        if (issues.Count > 0)
        {
            throw new FeatureProofFormatException("The feature proof document is invalid.", issues);
        }

        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var content = File.Exists(path)
            ? await MergeRunsIntoExistingFileAsync(path, document, cancellationToken)
            : FeatureProofJson.Serialize(document);

        await File.WriteAllTextAsync(path, content.TrimEnd() + "\n", cancellationToken);
    }

    private static async Task<string> MergeRunsIntoExistingFileAsync(
        string path,
        FeatureProofDocument document,
        CancellationToken cancellationToken)
    {
        var existingContent = await File.ReadAllTextAsync(path, cancellationToken);
        var root = JsonNode.Parse(
            existingContent,
            new JsonNodeOptions { PropertyNameCaseInsensitive = true },
            new JsonDocumentOptions
            {
                AllowTrailingCommas = true,
                CommentHandling = JsonCommentHandling.Skip
            }) as JsonObject;

        if (root is null)
        {
            return FeatureProofJson.Serialize(document);
        }

        root["runs"] = JsonSerializer.SerializeToNode(document.Runs, FeatureProofJson.Options);
        return root.ToJsonString(FeatureProofJson.Options);
    }
}
