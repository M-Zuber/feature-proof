namespace FeatureProof.Core;

public sealed record ValidationIssue(string Code, string Message);

public static class FeatureProofValidation
{
    public static IReadOnlyList<ValidationIssue> Validate(FeatureProofDocument document)
    {
        var issues = new List<ValidationIssue>();

        if (!string.Equals(document.Schema, FeatureProofSchema.Current, StringComparison.OrdinalIgnoreCase))
        {
            issues.Add(new("schema.unsupported", $"Expected schema '{FeatureProofSchema.Current}'."));
        }

        if (document.Project is null)
        {
            issues.Add(new("project.required", "Project is required."));
        }
        else
        {
            Require(document.Project.Name, "project.name", "Project name is required.", issues);
            Require(document.Project.TargetSystem, "project.targetSystem", "Target system is required.", issues);
        }

        var areas = document.Areas ?? [];
        var checks = document.Checks ?? [];
        var runs = document.Runs ?? [];

        var areaIds = FindDuplicateIds(areas.Where(area => area is not null).Select(area => area.Id));
        issues.AddRange(areaIds.Select(id => new ValidationIssue("areas.duplicateId", $"Area id '{id}' is duplicated.")));

        var checkIds = FindDuplicateIds(checks.Where(check => check is not null).Select(check => check.Id));
        issues.AddRange(checkIds.Select(id => new ValidationIssue("checks.duplicateId", $"Check id '{id}' is duplicated.")));

        var knownAreas = areas
            .Where(area => area is not null)
            .Select(area => area.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var check in checks)
        {
            if (check is null)
            {
                issues.Add(new("checks.required", "Every check entry must be an object."));
                continue;
            }

            Require(check.Id, "checks.id", "Every check needs a stable id.", issues);
            Require(check.Title, $"checks.{check.Id}.title", $"Check '{check.Id}' needs a title.", issues);
            Require(check.Area, $"checks.{check.Id}.area", $"Check '{check.Id}' needs an area.", issues);

            if (!string.IsNullOrWhiteSpace(check.Area) && !knownAreas.Contains(check.Area))
            {
                issues.Add(new("checks.unknownArea", $"Check '{check.Id}' references unknown area '{check.Area}'."));
            }
        }

        var knownChecks = checks
            .Where(check => check is not null)
            .Select(check => check.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var run in runs)
        {
            if (run is null)
            {
                issues.Add(new("runs.required", "Every run entry must be an object."));
                continue;
            }

            Require(run.Id, "runs.id", "Every run needs a stable id.", issues);

            foreach (var result in run.Results ?? [])
            {
                if (result is null)
                {
                    issues.Add(new("runs.results.required", $"Run '{run.Id}' contains an invalid result entry."));
                    continue;
                }

                if (!knownChecks.Contains(result.CheckId))
                {
                    issues.Add(new("runs.unknownCheck", $"Run '{run.Id}' references unknown check '{result.CheckId}'."));
                }
            }
        }

        return issues;
    }

    private static void Require(string? value, string code, string message, List<ValidationIssue> issues)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            issues.Add(new(code, message));
        }
    }

    private static IEnumerable<string> FindDuplicateIds(IEnumerable<string> ids) =>
        ids.Where(id => !string.IsNullOrWhiteSpace(id))
            .GroupBy(id => id, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key);
}
