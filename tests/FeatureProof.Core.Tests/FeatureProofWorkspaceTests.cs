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

            workspace.StartRun("tester", "local-build", "Edge", "");
            workspace.RecordResult("open-local-file", CheckStatus.Pass, "");

            var activeResult = workspace.ActiveRun?.Results.Single();
            Assert.NotNull(activeResult);
            Assert.Null(activeResult.Notes);

            workspace.CompleteLatestRun();
            workspace.RecordResult("open-local-file", CheckStatus.Fail, "should not save");

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

    [Fact]
    public async Task TryLoadAsync_InvalidFile_PreservesCurrentWorkspaceAndReportsIssue()
    {
        var validPath = Path.Combine(Path.GetTempPath(), $"feature-proof-valid-{Guid.NewGuid():N}.fproof");
        var invalidPath = Path.Combine(Path.GetTempPath(), $"feature-proof-invalid-{Guid.NewGuid():N}.fproof");
        var fileStore = new FeatureProofFileStore();
        var workspace = new FeatureProofWorkspace(fileStore);

        try
        {
            await fileStore.SaveAsync(validPath, CreateDocumentWithCompletedRun());
            await File.WriteAllTextAsync(
                invalidPath,
                """
                {
                  "$schema": "feature-proof/v1",
                  "project": {
                    "name": "",
                    "targetSystem": ""
                  },
                  "areas": [],
                  "checks": [],
                  "runs": []
                }
                """);

            Assert.True(await workspace.TryLoadAsync(validPath));
            var loadedDocument = workspace.Document;

            Assert.False(await workspace.TryLoadAsync(invalidPath));

            Assert.Same(loadedDocument, workspace.Document);
            Assert.Equal(validPath, workspace.CurrentPath);
            Assert.NotEmpty(workspace.Issues);
            Assert.Contains("Could not open", workspace.Message);
        }
        finally
        {
            DeleteIfExists(validPath);
            DeleteIfExists(invalidPath);
        }
    }

    [Fact]
    public async Task CompleteLatestRunAndSaveAsync_PersistsCompletion()
    {
        var path = Path.Combine(Path.GetTempPath(), $"feature-proof-{Guid.NewGuid():N}.fproof");
        var fileStore = new FeatureProofFileStore();
        var workspace = new FeatureProofWorkspace(fileStore);

        try
        {
            await fileStore.SaveAsync(path, CreateDocumentWithCompletedRun());
            await workspace.LoadAsync(path);

            workspace.StartRun("tester", "local-build", "Edge", "");
            await workspace.CompleteLatestRunAndSaveAsync();

            var reloaded = await fileStore.LoadAsync(path);
            var completedRun = reloaded.Runs.Last();

            Assert.NotNull(completedRun.CompletedAt);
            Assert.False(workspace.HasUnsavedChanges);
            Assert.Contains("Completed and saved", workspace.Message);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public async Task FindActiveRunResult_ReturnsOnlyResultsFromActiveRun()
    {
        var path = Path.Combine(Path.GetTempPath(), $"feature-proof-{Guid.NewGuid():N}.fproof");
        var fileStore = new FeatureProofFileStore();
        var workspace = new FeatureProofWorkspace(fileStore);

        try
        {
            await fileStore.SaveAsync(path, CreateDocumentWithCompletedRun());
            await workspace.LoadAsync(path);

            Assert.Null(workspace.FindActiveRunResult("open-local-file"));

            workspace.StartRun("tester", "local-build", "Edge", "");

            Assert.Null(workspace.FindActiveRunResult("open-local-file"));

            workspace.RecordResult("open-local-file", CheckStatus.Fail, "Current run issue.");

            var activeResult = workspace.FindActiveRunResult("open-local-file");
            Assert.NotNull(activeResult);
            Assert.Equal(CheckStatus.Fail, activeResult.Status);
            Assert.Equal("Current run issue.", activeResult.Notes);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void SetMessage_UpdatesUserVisibleMessage()
    {
        var workspace = new FeatureProofWorkspace(new FeatureProofFileStore());

        workspace.SetMessage("Copied report to clipboard.");

        Assert.Equal("Copied report to clipboard.", workspace.Message);
    }

    private static FeatureProofDocument CreateDocumentWithCompletedRun() =>
        new()
        {
            Project = new ProjectInfo { Name = "FeatureProof", TargetSystem = "FeatureProof" },
            Areas = [new FeatureArea { Id = "workspace", Name = "Workspace" }],
            Checks =
            [
                new FeatureCheck
                {
                    Id = "open-local-file",
                    Area = "workspace",
                    Title = "Open local file",
                    SourceSystem = "FeatureProof Requirements"
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
                            CheckId = "open-local-file",
                            Status = CheckStatus.Blocked,
                            Notes = "The file path was missing."
                        }
                    ]
                }
            ]
        };

    private static void DeleteIfExists(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
