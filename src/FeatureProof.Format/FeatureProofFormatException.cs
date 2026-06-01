using FeatureProof.Core;

namespace FeatureProof.Format;

public sealed class FeatureProofFormatException : Exception
{
    public FeatureProofFormatException(string message)
        : base(message)
    {
        Issues = [];
    }

    public FeatureProofFormatException(string message, IReadOnlyList<ValidationIssue> issues)
        : base(message)
    {
        Issues = issues;
    }

    public IReadOnlyList<ValidationIssue> Issues { get; }
}
