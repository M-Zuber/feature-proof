using FeatureProof.Format;

namespace FeatureProof.Format.Tests;

public sealed class FeatureProofJsonTests
{
    [Fact]
    public void Deserialize_LoadsSampleFile()
    {
        var samplePath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "..",
            "samples",
            "featureproof-dogfood.fproof"));

        var document = FeatureProofJson.Deserialize(File.ReadAllText(samplePath));

        Assert.Equal("feature-proof/v1", document.Schema);
        Assert.NotEmpty(document.Checks);
        Assert.NotEmpty(document.Runs);
    }

    [Fact]
    public void Serialize_RoundTripsEnumsAsStrings()
    {
        var samplePath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "..",
            "samples",
            "featureproof-dogfood.fproof"));

        var document = FeatureProofJson.Deserialize(File.ReadAllText(samplePath));
        var serialized = FeatureProofJson.Serialize(document);

        Assert.Contains("\"priority\": \"must\"", serialized);
        Assert.Contains("\"status\": \"pass\"", serialized);
    }
}
