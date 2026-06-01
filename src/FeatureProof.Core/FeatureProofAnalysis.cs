namespace FeatureProof.Core;

public sealed record CheckSnapshot(
    FeatureCheck Check,
    FeatureArea? Area,
    CheckResult? LatestResult,
    TestRun? LatestRun)
{
    public CheckStatus LatestStatus => LatestResult?.Status ?? CheckStatus.Untested;
}

public sealed record StatusCount(CheckStatus Status, int Count);

public sealed class ProjectSummary
{
    public int TotalChecks { get; init; }
    public int TotalRuns { get; init; }
    public int CompletedRuns { get; init; }
    public IReadOnlyList<StatusCount> StatusCounts { get; init; } = [];
    public IReadOnlyList<CheckSnapshot> Checks { get; init; } = [];
    public IReadOnlyDictionary<string, IReadOnlyList<CheckSnapshot>> ChecksByArea { get; init; } =
        new Dictionary<string, IReadOnlyList<CheckSnapshot>>();
}

public static class FeatureProofAnalysis
{
    public static ProjectSummary Summarize(FeatureProofDocument document)
    {
        var areas = document.Areas.ToDictionary(area => area.Id, StringComparer.OrdinalIgnoreCase);
        var latestByCheck = document.Runs
            .SelectMany(run => run.Results.Select(result => new { run, result }))
            .GroupBy(item => item.result.CheckId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderByDescending(item => item.result.RecordedAt)
                    .ThenByDescending(item => item.run.StartedAt)
                    .First(),
                StringComparer.OrdinalIgnoreCase);

        var snapshots = document.Checks
            .Select(check =>
            {
                latestByCheck.TryGetValue(check.Id, out var latest);
                areas.TryGetValue(check.Area, out var area);
                return new CheckSnapshot(check, area, latest?.result, latest?.run);
            })
            .OrderBy(snapshot => snapshot.Area?.Name ?? snapshot.Check.Area)
            .ThenBy(snapshot => snapshot.Check.Title)
            .ToList();

        var counts = Enum.GetValues<CheckStatus>()
            .Select(status => new StatusCount(status, snapshots.Count(snapshot => snapshot.LatestStatus == status)))
            .ToList();

        var byArea = snapshots
            .GroupBy(snapshot => snapshot.Area?.Name ?? snapshot.Check.Area)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<CheckSnapshot>)group.ToList(),
                StringComparer.OrdinalIgnoreCase);

        return new ProjectSummary
        {
            TotalChecks = document.Checks.Count,
            TotalRuns = document.Runs.Count,
            CompletedRuns = document.Runs.Count(run => run.CompletedAt is not null),
            StatusCounts = counts,
            Checks = snapshots,
            ChecksByArea = byArea
        };
    }
}
