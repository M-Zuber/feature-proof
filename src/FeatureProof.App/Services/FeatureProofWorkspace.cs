using FeatureProof.Core;
using FeatureProof.Format;
using System.Text.Json;

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

    public async Task<bool> TryLoadAsync(string path, CancellationToken cancellationToken = default)
    {
        try
        {
            await LoadAsync(path, cancellationToken);
            return true;
        }
        catch (FeatureProofFormatException exception)
        {
            Issues = exception.Issues.Count > 0
                ? exception.Issues
                : [new ValidationIssue("format", exception.Message)];
            Message = $"Could not open {Path.GetFileName(path)}. Review the validation issues below.";
            return false;
        }
        catch (JsonException exception)
        {
            Issues = [new ValidationIssue("json", exception.Message)];
            Message = $"Could not open {Path.GetFileName(path)}. The file is not valid JSON.";
            return false;
        }
        catch (IOException exception)
        {
            Issues = [new ValidationIssue("file", exception.Message)];
            Message = $"Could not open {Path.GetFileName(path)}.";
            return false;
        }
        catch (UnauthorizedAccessException exception)
        {
            Issues = [new ValidationIssue("file", exception.Message)];
            Message = $"Could not open {Path.GetFileName(path)}.";
            return false;
        }
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

    public async Task CompleteLatestRunAndSaveAsync(CancellationToken cancellationToken = default)
    {
        var run = ActiveRun;
        if (run is null)
        {
            Message = "Start a run before completing it.";
            return;
        }

        run.CompletedAt = DateTimeOffset.Now;
        HasUnsavedChanges = true;

        try
        {
            await SaveAsync(cancellationToken);
            if (HasUnsavedChanges)
            {
                Message = $"Completed {run.Id}, but it was not saved. {Message}";
                return;
            }

            Message = $"Completed and saved {run.Id}";
        }
        catch (Exception exception) when (exception is FeatureProofFormatException or JsonException or IOException or UnauthorizedAccessException)
        {
            Message = $"Completed {run.Id}, but could not save: {exception.Message}";
        }
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
        var run = ActiveRun;
        if (run is null)
        {
            return;
        }

        run.CompletedAt = DateTimeOffset.Now;
        HasUnsavedChanges = true;
        Message = $"Completed {run.Id}";
    }

    public CheckResult? FindActiveRunResult(string checkId)
    {
        var run = ActiveRun;
        return run?.Results.FirstOrDefault(item =>
            string.Equals(item.CheckId, checkId, StringComparison.OrdinalIgnoreCase));
    }

    public void SetMessage(string message)
    {
        Message = message;
    }

    public void RefreshValidation()
    {
        Issues = Document is null ? [] : FeatureProofValidation.Validate(Document);
    }
}
