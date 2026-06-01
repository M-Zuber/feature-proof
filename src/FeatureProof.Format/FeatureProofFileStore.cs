using FeatureProof.Core;

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

        await File.WriteAllTextAsync(path, FeatureProofJson.Serialize(document), cancellationToken);
    }
}
