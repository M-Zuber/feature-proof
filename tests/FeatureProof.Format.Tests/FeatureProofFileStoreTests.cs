using System.Text.Json.Nodes;
using FeatureProof.Core;
using FeatureProof.Format;

namespace FeatureProof.Format.Tests;

public sealed class FeatureProofFileStoreTests
{
    [Fact]
    public async Task SaveAsync_ForExistingFile_ReplacesOnlyRuns()
    {
        var path = Path.Combine(Path.GetTempPath(), $"feature-proof-{Guid.NewGuid():N}.fproof");
        await File.WriteAllTextAsync(path, ExistingFileContent);

        var fileStore = new FeatureProofFileStore();

        try
        {
            var document = await fileStore.LoadAsync(path);
            document.Runs.Add(new TestRun
            {
                Id = "run-new",
                StartedAt = DateTimeOffset.Parse("2026-06-02T10:00:00+03:00"),
                Tester = "tester",
                TargetVersion = "crm-preview",
                Results =
                [
                    new CheckResult
                    {
                        CheckId = "donor-search-basic",
                        Status = CheckStatus.Pass,
                        Notes = null,
                        RecordedAt = DateTimeOffset.Parse("2026-06-02T10:05:00+03:00")
                    }
                ]
            });

            await fileStore.SaveAsync(path, document);

            var saved = await File.ReadAllTextAsync(path);
            Assert.Contains("\"$schema\": \"../schema/feature-proof.schema.json\"", saved);
            Assert.Contains("[data-testid='donor-search-input']", saved);
            Assert.DoesNotContain("\\u0027", saved);
            Assert.DoesNotContain("\"selectors\": []", saved);
            Assert.DoesNotContain("\"risks\": []", saved);
            Assert.EndsWith("\n", saved);

            var originalWithoutRuns = WithoutRuns(ExistingFileContent);
            var savedWithoutRuns = WithoutRuns(saved);
            Assert.Equal(originalWithoutRuns.ToJsonString(), savedWithoutRuns.ToJsonString());
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    private static JsonNode WithoutRuns(string content)
    {
        var root = JsonNode.Parse(content)!.AsObject();
        root.Remove("runs");
        return root;
    }

    private const string ExistingFileContent = """
        {
          "$schema": "../schema/feature-proof.schema.json",
          "schema": "feature-proof/v1",
          "project": {
            "name": "All-in-One CRM Feature Parity",
            "targetSystem": "New All-in-One CRM",
            "sourceSystems": [
              "Legacy CRM"
            ]
          },
          "areas": [
            {
              "id": "donor-management",
              "name": "Donor Management"
            }
          ],
          "checks": [
            {
              "id": "donor-search-basic",
              "area": "donor-management",
              "title": "Search donors by name",
              "sourceSystem": "Legacy CRM",
              "priority": "must",
              "description": "A user can find an existing donor.",
              "steps": [
                "Open donor search."
              ],
              "expected": [
                "Matching donor records are shown."
              ],
              "agentHints": {
                "testType": "ui",
                "selectors": [
                  "[data-testid='donor-search-input']"
                ],
                "dataRequirements": [
                  "An existing donor with a known last name."
                ]
              }
            }
          ],
          "runs": [
            {
              "id": "run-old",
              "startedAt": "2026-06-02T09:00:00+03:00",
              "tester": "sample.user",
              "targetVersion": "crm-preview",
              "environment": {},
              "results": []
            }
          ]
        }
        """;
}
