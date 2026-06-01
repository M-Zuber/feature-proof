# FeatureProof

FeatureProof is a local-first parity testing tool for teams replacing several existing systems with a new all-in-one CRM.

The app stores parity projects as JSON-backed `.fproof` files on the local filesystem so teams can collaborate through git, OneDrive, SharePoint, Google Drive, or any other file-sharing workflow.

## Run

```powershell
dotnet run --project src\FeatureProof.App
```

Then open the local URL printed by `dotnet run`.

## First Sample

The app opens `samples\crm-parity.fproof` by default when launched from the repository. Use it to acceptance-test the first workflow:

- inspect dashboard counts
- start a new run
- record a result for a check
- save the file
- preview the Markdown report

## Format

See `docs\format-v1.md` and the schemas in `schema\`.

## Package

Create a manually shareable artifact with:

```powershell
pwsh ./eng/package.ps1 -Version 0.1.0
```

The script creates `artifacts/FeatureProof-0.1.0.zip`. The archive contains self-contained app folders for Windows, Linux, and macOS, so team members do not need to install .NET. Sample `.fproof` files are intentionally excluded from the package.
