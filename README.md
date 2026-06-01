# FeatureProof

FeatureProof is a local-first feature testing tool for teams validating that a product, migration, rewrite, integration, or release delivers the expected behavior.

The app stores feature proof projects as JSON-backed `.fproof` files on the local filesystem so teams can collaborate through git, OneDrive, SharePoint, Google Drive, or any other file-sharing workflow.

## Run

```powershell
dotnet run --project src\FeatureProof.App
```

Then open the local URL printed by `dotnet run`.

## First Sample

The app opens `samples\featureproof-dogfood.fproof` by default when launched from the repository. Use it to acceptance-test FeatureProof itself:

- inspect dashboard counts
- start a new run
- record a result for a check
- save the file
- preview the Markdown report

## Format

See `docs\format-v1.md` and the schemas in `schema\`.

## Generating `.fproof` Files With An Agent

Agents can help turn requirements, existing-system notes, product docs, or release plans into a first-pass `.fproof` file. Treat agent output as a draft for product owners, QA, system SMEs, and engineers to review before using it as acceptance evidence.

### Files To Provide As Agent Context

For best results, give the agent these repository files:

- `docs/format-v1.md`
- `schema/feature-proof.schema.json`
- `schema/feature-proof-definition.schema.json`
- `samples/source-system-definition.fproofdef.json`
- `samples/featureproof-dogfood.fproof`

Then add your project-specific source material:

- A completed `.fproofdef.json` source definition, based on `samples/source-system-definition.fproofdef.json`.
- Existing workflow docs, SOPs, training guides, screenshots, field lists, reports, user stories, release notes, or requirements.
- Target-system notes that explain module names, known gaps, test environment URLs, and test-data constraints.
- Any terminology map between source material and the product being tested.

Avoid giving the agent production secrets, real payment credentials, private donor data, or unrestricted exports. Use sanitized examples and describe sensitive data requirements instead.

### Recommended Prompt

Use a prompt shaped like this:

```text
You are generating a FeatureProof parity file.

Use the provided FeatureProof v1 docs, JSON Schema, sample .fproof file, and source-system definition.

Create a valid feature-proof/v1 .fproof JSON document for:
- project name: <project name>
- target system: <product or release name>
- source systems: <requirements, legacy systems, docs, or reference sources>

Requirements:
- Output only valid JSON. Do not wrap it in Markdown.
- Follow schema/feature-proof.schema.json exactly.
- Use stable lower-kebab-case ids.
- Create areas that map to user-facing or business capabilities, not merely UI pages.
- Create checks that a human tester can run manually today.
- For each check, include clear steps, expected results, sourceSystem, priority, and agentHints.
- Prefer several specific checks over one broad check.
- Do not invent credentials, URLs, selectors, or test data. If unknown, describe the requirement in agentHints.dataRequirements or agentHints.risks.
- Leave runs as an empty array unless real test results were provided.
- Include only checks that can be traced back to the provided source material.

After generating the JSON, validate it mentally against the schema and fix any invalid fields before responding.
```

### Source Definition Prompt

If you are starting from unstructured notes, ask the agent to create a `.fproofdef.json` first:

```text
Create a FeatureProof source-system definition using schema feature-proof-definition/v1.

Use the provided notes to identify source systems, modules, capabilities, business outcomes, priorities, acceptance signals, and data requirements.

Do not create the final .fproof file yet. Output only valid JSON for a .fproofdef.json draft.
Flag unclear areas in capability dataRequirements or acceptanceSignals rather than inventing behavior.
```

Review the `.fproofdef.json` with SMEs before asking the agent to generate the final `.fproof` file.

### Review Checklist

Before using an agent-generated `.fproof` file:

- Confirm every check maps to a real requirement, capability, release goal, or reference behavior.
- Split checks that test multiple unrelated outcomes.
- Make sure `priority` reflects business risk, not implementation difficulty.
- Make sure manual `steps` are concrete enough for a tester unfamiliar with the legacy system.
- Move unknowns into `agentHints.dataRequirements` or `agentHints.risks`.
- Keep `runs` empty until someone has actually tested the target system.
- Validate the file with the app by opening it before sharing it with the team.

### Good Agent Output

Good generated checks are specific, observable, and evidence-friendly. For example, "Search donors by last name and open the correct profile" is better than "Donor search works". The expected result should say what the tester must see, not just that the operation succeeds.

## Package

Create a manually shareable artifact with:

```powershell
pwsh ./eng/package.ps1 -Version 0.1.0
```

The script creates `artifacts/FeatureProof-0.1.0.zip`. The archive contains self-contained app folders for Windows, Linux, and macOS, so team members do not need to install .NET. Sample `.fproof` files are intentionally excluded from the package.
