using FeatureProof.Core;
using FeatureProof.Format;

namespace FeatureProof.App.Services;

public sealed class FeatureProofWorkspace(FeatureProofFileStore fileStore)
{
    private readonly FeatureProofFileStore _fileStore = fileStore;

    public FeatureProofDocument? Document { get; private set; }
    public string? CurrentPath { get; private set; }
    public string? Message { get; private set; }
    public IReadOnlyList<ValidationIssue> Issues { get; private set; } = [];
    public bool HasUnsavedChanges { get; private set; }

    public ProjectSummary? Summary => Document is null ? null : FeatureProofAnalysis.Summarize(Document);
    public TestRun? ActiveRun
    {
        get
        {
            var latestRun = Document?.Runs.LastOrDefault();
            return latestRun?.CompletedAt is null ? latestRun : null;
        }
    }

    public async Task LoadAsync(string path, CancellationToken cancellationToken = default)
    {
        Document = await _fileStore.LoadAsync(path, cancellationToken);
        CurrentPath = path;
        HasUnsavedChanges = false;
        RefreshValidation();
        Message = $"Loaded {Path.GetFileName(path)}";
    }

    public async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        if (Document is null || string.IsNullOrWhiteSpace(CurrentPath))
        {
            Message = "Open a .fproof file before saving.";
            return;
        }

        RefreshValidation();
        if (Issues.Count > 0)
        {
            Message = "Fix validation issues before saving.";
            return;
        }

        await _fileStore.SaveAsync(CurrentPath, Document, cancellationToken);
        HasUnsavedChanges = false;
        Message = $"Saved {Path.GetFileName(CurrentPath)}";
    }

    public void StartRun(string tester, string targetVersion, string browser, string url)
    {
        if (Document is null)
        {
            return;
        }

        var run = new TestRun
        {
            Id = $"run-{DateTimeOffset.Now:yyyyMMdd-HHmmss}",
            StartedAt = DateTimeOffset.Now,
            Tester = tester,
            TargetVersion = targetVersion,
            Environment =
            {
                ["browser"] = browser,
                ["url"] = url
            }
        };

        Document.Runs.Add(run);
        HasUnsavedChanges = true;
        Message = $"Started {run.Id}";
    }

    public void RecordResult(string checkId, CheckStatus status, string? notes)
    {
        var run = ActiveRun;
        if (run is null)
        {
            Message = "Start a run before recording results.";
            return;
        }

        var result = run.Results.FirstOrDefault(item =>
            string.Equals(item.CheckId, checkId, StringComparison.OrdinalIgnoreCase));

        if (result is null)
        {
            result = new CheckResult { CheckId = checkId };
            run.Results.Add(result);
        }

        result.Status = status;
        result.Notes = string.IsNullOrWhiteSpace(notes) ? null : notes;
        result.RecordedAt = DateTimeOffset.Now;
        HasUnsavedChanges = true;
        Message = $"Recorded {status} for {checkId}";
    }

    public void CompleteLatestRun()
    {
        var run = Document?.Runs.LastOrDefault();
        if (run is null)
        {
            return;
        }

        run.CompletedAt = DateTimeOffset.Now;
        HasUnsavedChanges = true;
        Message = $"Completed {run.Id}";
    }

    public void RefreshValidation()
    {
        Issues = Document is null ? [] : FeatureProofValidation.Validate(Document);
    }
}
