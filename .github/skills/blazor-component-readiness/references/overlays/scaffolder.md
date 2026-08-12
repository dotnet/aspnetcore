# Scaffolder readiness overlay

**Overlay version:** 1.0.0

Apply this six-ID overlay only when the bounded deliverable includes a supported scaffolder.
A sample generator or handwritten template is not a scaffolder without an explicit product claim.

- **SCF-01** Integrates through the documented scaffolding mechanism for the target ecosystem.
- **SCF-02** Generated output meets the core accessibility and Blazor requirements.
- **SCF-03** Dependencies are limited to .NET and explicitly approved third-party libraries.
- **SCF-04** Packages and generated dependency versions are signed and pinned.
- **SCF-05** Scaffolding does not perform undocumented arbitrary script execution.
- **SCF-06** The scaffolder stays current with supported library versions.

`dotnet scaffold` is one qualifying .NET ecosystem mechanism, not the only possible implementation.
