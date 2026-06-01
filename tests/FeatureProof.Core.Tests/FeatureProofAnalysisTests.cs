using FeatureProof.Core;

namespace FeatureProof.Core.Tests;

public sealed class FeatureProofAnalysisTests
{
    [Fact]
    public void Summarize_UsesLatestResultPerCheck()
    {
        var document = new FeatureProofDocument
        {
            Project = new ProjectInfo { Name = "Parity", TargetSystem = "CRM" },
            Areas = [new FeatureArea { Id = "donors", Name = "Donors" }],
            Checks =
            [
                new FeatureCheck { Id = "find-donor", Area = "donors", Title = "Find donor" }
            ],
            Runs =
            [
                new TestRun
                {
                    Id = "old",
                    StartedAt = DateTimeOffset.Parse("2026-06-01T10:00:00+03:00"),
                    Results =
                    [
                        new CheckResult
                        {
                            CheckId = "find-donor",
                            Status = CheckStatus.Fail,
                            RecordedAt = DateTimeOffset.Parse("2026-06-01T10:10:00+03:00")
                        }
                    ]
                },
                new TestRun
                {
                    Id = "new",
                    StartedAt = DateTimeOffset.Parse("2026-06-02T10:00:00+03:00"),
                    Results =
                    [
                        new CheckResult
                        {
                            CheckId = "find-donor",
                            Status = CheckStatus.Pass,
                            RecordedAt = DateTimeOffset.Parse("2026-06-02T10:10:00+03:00")
                        }
                    ]
                }
            ]
        };

        var summary = FeatureProofAnalysis.Summarize(document);

        Assert.Equal(CheckStatus.Pass, summary.Checks.Single().LatestStatus);
        Assert.Equal("new", summary.Checks.Single().LatestRun?.Id);
    }
}
