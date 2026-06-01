namespace FeatureProof.Core;

public sealed class FeatureProofDocument
{
    public string Schema { get; set; } = FeatureProofSchema.Current;
    public ProjectInfo Project { get; set; } = new();
    public List<FeatureArea> Areas { get; set; } = [];
    public List<FeatureCheck> Checks { get; set; } = [];
    public List<TestRun> Runs { get; set; } = [];
}

public static class FeatureProofSchema
{
    public const string Current = "feature-proof/v1";
}

public sealed class ProjectInfo
{
    public string Name { get; set; } = "";
    public string TargetSystem { get; set; } = "";
    public List<string> SourceSystems { get; set; } = [];
    public string? Description { get; set; }
}

public sealed class FeatureArea
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Description { get; set; }
}

public sealed class FeatureCheck
{
    public string Id { get; set; } = "";
    public string Area { get; set; } = "";
    public string Title { get; set; } = "";
    public string SourceSystem { get; set; } = "";
    public CheckPriority Priority { get; set; } = CheckPriority.Should;
    public string Description { get; set; } = "";
    public List<string> Steps { get; set; } = [];
    public List<string> Expected { get; set; } = [];
    public AgentHints AgentHints { get; set; } = new();
}

public sealed class AgentHints
{
    public string? TestType { get; set; }
    public List<string> Selectors { get; set; } = [];
    public List<string> DataRequirements { get; set; } = [];
    public List<string> Risks { get; set; } = [];
}

public sealed class TestRun
{
    public string Id { get; set; } = "";
    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.Now;
    public DateTimeOffset? CompletedAt { get; set; }
    public string Tester { get; set; } = "";
    public string TargetVersion { get; set; } = "";
    public Dictionary<string, string> Environment { get; set; } = [];
    public List<CheckResult> Results { get; set; } = [];
}

public sealed class CheckResult
{
    public string CheckId { get; set; } = "";
    public CheckStatus Status { get; set; } = CheckStatus.Untested;
    public string? Notes { get; set; }
    public List<EvidenceItem> Evidence { get; set; } = [];
    public DateTimeOffset RecordedAt { get; set; } = DateTimeOffset.Now;
}

public sealed class EvidenceItem
{
    public string Type { get; set; } = "note";
    public string Path { get; set; } = "";
    public string? Caption { get; set; }
}

public enum CheckPriority
{
    Must,
    Should,
    Could
}

public enum CheckStatus
{
    Untested,
    Pass,
    Partial,
    Fail,
    Blocked,
    NotApplicable,
    NeedsReview
}
