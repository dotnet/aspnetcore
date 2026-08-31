## General

* Make only high confidence suggestions when reviewing code changes.
* Always use the latest version C#, currently C# 13 features.
* Never change global.json unless explicitly asked to.
* Never change package.json or package-lock.json files unless explicitly asked to.
* Never change NuGet.config files unless explicitly asked to.

## Public API Changes

* Treat any new or changed `public` or `protected` type, member, signature, default, or convention as a potential public API change. Before implementing it, verify that the linked issue is `api-approved`.
* If approval is missing, stop and explain the required [API review process](../docs/APIReviewProcess.md): an issue owner or champion drives an `api-suggestion` with the proposal in ref-assembly form, then applies `api-ready-for-review` and notifies `@dotnet/aspnet-api-review` when it is mature. Implementation starts only after `api-approved`.
* When reporting this gate, explicitly say that `PublicAPI.Unshipped.txt` tracks compatibility but does not grant API approval, and that any implementation change to the approved API shape must return to API review.

## Framework assembly boundaries

* In shipping framework code, do not add `InternalsVisibleTo` or use `[UnsafeAccessor]` to access non-public members in another framework assembly. Existing uses of these mechanisms are not precedent for new uses.
* Redesign the assembly boundary instead. If that requires a public API, follow the repository API-review and baseline process.

## Formatting

* Apply code-formatting style defined in `.editorconfig`.
* Prefer file-scoped namespace declarations and single-line using directives.
* Insert a newline before the opening curly brace of any code block (e.g., after `if`, `for`, `while`, `foreach`, `using`, `try`, etc.).
* Ensure that the final return statement of a method is on its own line.
* Use pattern matching and switch expressions wherever possible.
* Use `nameof` instead of string literals when referring to member names.
* Ensure that XML doc comments are created for any public APIs. When applicable, include `<example>` and `<code>` documentation in the comments.

### Nullable Reference Types

* Declare variables non-nullable, and check for `null` at entry points.
* Always use `is null` or `is not null` instead of `== null` or `!= null`.
* Trust the C# null annotations and don't add null checks when the type system says a value cannot be null.

### Testing

* We use xUnit SDK v3 for tests.
* Do not emit "Act", "Arrange" or "Assert" comments.
* Use Moq for mocking in tests.
* Copy existing style in nearby files for test method names and capitalization.

## Running tests

* To build and run tests in the repo, use the `build.sh` script that is located in each subdirectory within the `src` folder. For example, to run the build with tests in the `src/Http` directory, run `./src/Http/build.sh -test`.
* Before claiming a bug fix is verified, confirm that the relevant test or check fails for the expected reason without the fix and passes with it. Reading the source or seeing a test pass on its own is not proof that the bug is fixed.
* For a `[Theory]` or other parameterized test, confirm that each row fails for the expected reason without the fix and passes with it; a red test proves only that at least one row failed. `dotnet test --filter` cannot select an individual `InlineData` row by parameter value, so inspect every case in the test output instead of relying on the `Failed!` or `Passed!` summary. A row that passes because its targeted scenario or code path never ran, such as from unmet setup, a missing prerequisite, or conditional execution, does not verify the fix.
* For behavioral review findings and bug-fix verification, use the smallest faithful test path. Include the component, service, runtime, or browser mechanism that owns or produces each disputed precondition, and observe the claimed material effect at the appropriate boundary, such as UI, protocol, persisted state, resource use, timing or performance, logging, or another contract-relevant behavior. Any test establishes only the downstream response, not producer reachability, if it directly injects callbacks or events or otherwise bypasses the owning producer. An isolated test can provide faithful evidence when it exercises the real producer.
* For behavioral findings and bug-fix verification, E2E validation is unnecessary when the disputed preconditions and material effects are fully established at a lower faithful boundary. This does not waive E2E coverage required for shipped implementation work.
* If faithful validation is impractical, state the observed boundary and limitation, and do not describe the behavioral claim as verified.
* If that red/green verification isn't practical, explain why, state what you did verify, and don't describe the fix as verified.

## .NET Environment

* Before running any `dotnet` commands in this repository, always activate the locally installed .NET environment first by running the appropriate activation script from the repository root:
  * On Windows: `. ./activate.ps1` (from repository root)
  * On Linux/Mac: `source activate.sh` (from repository root)
* If not in the repository root, navigate there first or use the full path to the activation script.
* This ensures that the correct version of .NET SDK is used for the repository.

## ASP.NET Core Components Area
* When working on issues under the src/Components area, follow the instructions in [./instructions/components.instructions.md](./instructions/components.instructions.md).
