using FeatureProof.Core;

namespace FeatureProof.Core.Tests;

public sealed class FeatureProofValidationTests
{
    [Fact]
    public void Validate_FlagsUnknownCheckReferences()
    {
        var document = new FeatureProofDocument
        {
            Project = new ProjectInfo { Name = "Parity", TargetSystem = "CRM" },
            Runs =
            [
                new TestRun
                {
                    Id = "run-1",
                    Results = [new CheckResult { CheckId = "missing-check" }]
                }
            ]
        };

        var issues = FeatureProofValidation.Validate(document);

        Assert.Contains(issues, issue => issue.Code == "runs.unknownCheck");
    }
}
