# ProjectTemplates guidance

## Validation preflight

1. Read [README.md](README.md), including the focused `Templates.Tests` workflow, before choosing a build or test command.
2. In a fresh worktree, initialize the required submodules before dependency-aware template test builds:

   ```powershell
   git submodule update --init --recursive
   ```

3. Activate the repository SDK before running `dotnet` directly: `. ./activate.ps1` on Windows or `source activate.sh` on Linux and macOS.
4. Map every package consumed by the generated applications to its producer build or pack step before running `Templates.Tests`. Include the complete matching Shipping and NonShipping package graph, not only the template packages.

## Choose the validation boundary

- Use the `src\ProjectTemplates\build.cmd` or `build.sh` area entry point for template generation, build, and package validation.
- Use `Templates.Tests` when the change must be validated by creating, restoring, building, publishing, or running generated applications. An area package build does not establish generated-application behavior.
- For manual generated-application validation, follow the README workflow through the full package/runtime producers, the matching `Run-*-Locally.ps1` script, and an application-boundary probe. Do not treat script completion alone as runtime validation.
- Prefer the smallest generated-app test that reaches the changed template and requested assertion. If a prerequisite prevents that assertion from running, report the blocked test and prerequisite, then identify the lower faithful boundary that was validated.

Do not create an empty artifact or package-source directory to bypass restore validation. Produce the missing artifacts with the responsible build or pack step.
