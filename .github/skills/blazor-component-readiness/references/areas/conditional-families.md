# Conditional requirement families

Applies to `SCF-*` and `AI-*`.

These are opt-in overlays, not part of the 110-ID core. Include `SCF-*` only when the bounded
deliverable contains a supported scaffolder and `AI-*` only when it contains an AI skill or plugin.
Do not emit placeholder `not applicable` rows when an overlay was not selected.

Core requirements use the versioned scope metadata in `../checklist.md`. These overlays retain
report-authored binary scope in schema 1; they cannot introduce, replace, or override a core ID.

## Scaffolders

When applicable, inspect the generated output as a deliverable:

- verify the documented scaffolding mechanism (`dotnet scaffold` is one qualifying example);
- compile and exercise generated output;
- apply the same accessibility and Blazor requirements to generated code;
- inspect dependency pinning, package signatures, and script execution;
- verify the scaffolder tracks supported library versions.

Do not treat a sample generator or handwritten template as a qualifying scaffolder without an
explicit product claim.

## AI skills

When applicable, verify the approved contribution path, repository guidance, update ownership,
dependency constraints, portability, and completed Responsible AI review. Generated code must be
reviewed as code; fluent output is not evidence of safety, accessibility, or compatibility.

Missing private Responsible AI records are `maintainer evidence required`. Incorrect contribution
placement, unsupported dependencies, or generated output that violates the quality bar can be
`defect`.
