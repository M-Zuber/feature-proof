# FeatureProof Format v1

FeatureProof uses JSON content with the `.fproof` extension. The custom part is the versioned schema, stable field names, and parity-oriented semantics. Keeping the payload JSON makes the format easy for C# tooling, editors, validation tools, and future agents.

## Files

- `.fproof`: A parity project with checks and run results.
- `.fproofdef.json`: A source-system definition that can be handed to an agent to generate an initial `.fproof`.
- `schema/feature-proof.schema.json`: JSON Schema for parity files.
- `schema/feature-proof-definition.schema.json`: JSON Schema for source definitions.

## Key Ideas

- Checks have stable ids so results can accumulate across many test runs.
- Runs capture tester, target version, environment, and one result per checked item.
- Evidence is referenced by path instead of embedded in the parity file.
- Agent hints are optional and should not make the human workflow harder.

## Status Values

- `untested`
- `pass`
- `partial`
- `fail`
- `blocked`
- `notApplicable`
- `needsReview`

## Priority Values

- `must`
- `should`
- `could`

## Versioning

The root `schema` field must be `feature-proof/v1`. Future versions should be migrated by code rather than silently interpreted as v1.
