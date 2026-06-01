using FeatureProof.App.Services;
using FeatureProof.Core;
using FeatureProof.Format;

namespace FeatureProof.Core.Tests;

public sealed class FeatureProofWorkspaceTests
{
    [Fact]
    public async Task StartRun_DoesNotCarryForwardPreviousNotes_AndCompletedRunsAreNotEditable()
    {
        var path = Path.Combine(Path.GetTempPath(), $"feature-proof-{Guid.NewGuid():N}.fproof");
        var fileStore = new FeatureProofFileStore();
        var workspace = new FeatureProofWorkspace(fileStore);

        try
        {
            await fileStore.SaveAsync(path, CreateDocumentWithCompletedRun());
            await workspace.LoadAsync(path);

            workspace.StartRun("tester", "crm-preview", "Edge", "");
            workspace.RecordResult("gift-one-time-card", CheckStatus.Pass, "");

            var activeResult = workspace.ActiveRun?.Results.Single();
            Assert.NotNull(activeResult);
            Assert.Null(activeResult.Notes);

            workspace.CompleteLatestRun();
            workspace.RecordResult("gift-one-time-card", CheckStatus.Fail, "should not save");

            Assert.Equal(CheckStatus.Pass, activeResult.Status);
            Assert.DoesNotContain(
                workspace.Document!.Runs.Last().Results,
                result => result.Notes == "should not save");
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    private static FeatureProofDocument CreateDocumentWithCompletedRun() =>
        new()
        {
            Project = new ProjectInfo { Name = "Parity", TargetSystem = "CRM" },
            Areas = [new FeatureArea { Id = "gift-processing", Name = "Gift Processing" }],
            Checks =
            [
                new FeatureCheck
                {
                    Id = "gift-one-time-card",
                    Area = "gift-processing",
                    Title = "Record one-time card donation",
                    SourceSystem = "Donations Portal"
                }
            ],
            Runs =
            [
                new TestRun
                {
                    Id = "run-old",
                    CompletedAt = DateTimeOffset.Parse("2026-06-02T09:20:00+03:00"),
                    Results =
                    [
                        new CheckResult
                        {
                            CheckId = "gift-one-time-card",
                            Status = CheckStatus.Blocked,
                            Notes = "Gateway sandbox credentials were not available."
                        }
                    ]
                }
            ]
        };
}
