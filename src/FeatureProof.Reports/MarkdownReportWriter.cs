using System.Text;
using FeatureProof.Core;

namespace FeatureProof.Reports;

public static class MarkdownReportWriter
{
    public static string Write(FeatureProofDocument document)
    {
        var summary = FeatureProofAnalysis.Summarize(document);
        var builder = new StringBuilder();

        builder.AppendLine($"# {document.Project.Name}");
        builder.AppendLine();
        builder.AppendLine($"Target system: **{document.Project.TargetSystem}**");

        if (document.Project.SourceSystems.Count > 0)
        {
            builder.AppendLine($"Source systems: {string.Join(", ", document.Project.SourceSystems)}");
        }

        builder.AppendLine();
        builder.AppendLine("## Summary");
        builder.AppendLine();
        builder.AppendLine($"- Checks: {summary.TotalChecks}");
        builder.AppendLine($"- Runs: {summary.TotalRuns}");
        builder.AppendLine($"- Completed runs: {summary.CompletedRuns}");

        foreach (var count in summary.StatusCounts.Where(count => count.Count > 0))
        {
            builder.AppendLine($"- {FormatStatus(count.Status)}: {count.Count}");
        }

        builder.AppendLine();
        builder.AppendLine("## Latest Check Results");
        builder.AppendLine();
        builder.AppendLine("| Area | Check | Priority | Latest status | Latest run | Notes |");
        builder.AppendLine("| --- | --- | --- | --- | --- | --- |");

        foreach (var snapshot in summary.Checks)
        {
            builder.Append("| ")
                .Append(Escape(snapshot.Area?.Name ?? snapshot.Check.Area))
                .Append(" | ")
                .Append(Escape(snapshot.Check.Title))
                .Append(" | ")
                .Append(snapshot.Check.Priority)
                .Append(" | ")
                .Append(FormatStatus(snapshot.LatestStatus))
                .Append(" | ")
                .Append(Escape(snapshot.LatestRun?.Id ?? ""))
                .Append(" | ")
                .Append(Escape(snapshot.LatestResult?.Notes ?? ""))
                .AppendLine(" |");
        }

        builder.AppendLine();
        builder.AppendLine("## Run History");
        builder.AppendLine();

        foreach (var run in document.Runs.OrderByDescending(run => run.StartedAt))
        {
            builder.AppendLine($"### {run.Id}");
            builder.AppendLine();
            builder.AppendLine($"- Started: {run.StartedAt:u}");
            builder.AppendLine($"- Completed: {(run.CompletedAt?.ToString("u") ?? "not completed")}");
            builder.AppendLine($"- Tester: {run.Tester}");
            builder.AppendLine($"- Target version: {run.TargetVersion}");
            builder.AppendLine($"- Results recorded: {run.Results.Count}");
            builder.AppendLine();
        }

        return builder.ToString();
    }

    private static string FormatStatus(CheckStatus status) =>
        status switch
        {
            CheckStatus.NotApplicable => "Not applicable",
            CheckStatus.NeedsReview => "Needs review",
            _ => status.ToString()
        };

    private static string Escape(string value) =>
        value.Replace("|", "\\|", StringComparison.Ordinal);
}
