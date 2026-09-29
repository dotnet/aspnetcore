## General

* Make only high confidence suggestions when reviewing code changes.
* Always use the latest version C#, currently C# 13 features.
* Never change global.json unless explicitly asked to.
* Never change package.json or package-lock.json files unless explicitly asked to.
* Never change NuGet.config files unless explicitly asked to.
* When a user explicitly requires a named skill that is not loaded, check the configured repository and user skill/plugin sources before substituting another workflow. Load the available skill and follow it; do not install arbitrary remote sources or silently approximate its workflow.

## Task Scope and Completion

* Before implementing a reported issue, verify the behavior on the current default branch, inspect relevant history and documentation, and establish the smallest faithful reproduction. If the user asks only to investigate or characterize, do not change shipping code or create or update a pull request until implementation is explicitly requested.
* Define the acceptance criteria before implementation. Do not claim completion or create or update a pull request until the requested acceptance criteria are green; identify any intentionally excluded cases or unverified boundaries.
* Before replacing a parser, formatter, serializer, or persisted/protected payload representation, characterize valid, malformed, and legacy inputs, exceptions versus fallback, relevant OS/culture behavior, and rolling-upgrade compatibility. Agree on the intended behavior before implementing or testing a replacement.

## Minimal diffs

* Preserve untouched code exactly as written. Do not make behavior-neutral formatting, reflow, renaming, inlining, extraction, expression, or control-flow changes while implementing a functional change.
* When an existing statement must change, preserve its explanatory locals and surrounding branch structure unless the requested behavior requires a different shape.
* Behavior-neutral clarification is acceptable only at the exact changed seam. For example, when adding a Boolean argument to an invocation, prefer a named argument. Do not update otherwise-untouched invocations merely to add argument names.
* Keep optional cleanup in a separate change rather than mixing it into the functional diff.
* Before finalizing, inspect changed existing lines specifically for edits that neither alter behavior nor directly support the requested change.

## Public API Changes

* Treat any new or changed `public` or `protected` type, member, signature, default, or convention as a potential public API change.
* When opening an implementation pull request that adds or changes public API, use the [`api-review` skill](./skills/api-review/SKILL.md) to create a separate API proposal issue. Link the proposal to the originating issue and implementation pull request.
* Implementation, pull request readiness, and merge may proceed before the linked issue is `api-approved`. Before the API can be included in an RTM release, verify that the API proposal issue has the `api-approved` label and that the approval covers the final implemented API shape.
* If the `api-approved` label is missing when preparing an RTM release, explain the required [API review process](../docs/APIReviewProcess.md): an issue owner or champion drives an `api-suggestion` with the proposal in ref-assembly form, then applies `api-ready-for-review` and notifies `@dotnet/aspnet-api-review` when it is mature.
* `PublicAPI.Unshipped.txt` tracks compatibility but does not grant API approval. Any implementation change to the proposed or previously approved API shape must return to API review before the API is included in an RTM release.

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

* Check for an `AGENTS.md` file in the relevant product area and follow its more specific guidance in addition to these repository-wide conventions.
* Place unit tests under the product's `test/` directory. Name unit-test projects `<ProductAssembly>.Tests`; preserve an area's established `.Test` suffix.
* Keep established test categories separate. In areas with a `.FunctionalTests` project, use it for hosted application or server boundaries. For complete browser or external workflows, preserve the area's established `.E2ETests` or `.E2E.Tests` suffix.
* Treat test project names as build-significant. They control test-project detection and often match exact `InternalsVisibleTo` entries; do not invent alternative names or override test-project detection.
* Put supporting applications and libraries under the area's `testassets/` directory unless the area has an established alternative. Do not give a support project a test-project suffix unless it contains discovered tests.
* Name each test file after its primary test class. For type-focused tests, map `Foo` to `FooTest` or `FooTests`, following nearby convention. Name scenario tests after the behavior exercised, and extend an existing matching test class when one exists.
* Use public test classes and descriptive PascalCase test methods. Follow the containing project's test framework, method-name style, namespace, fixtures, and parallelization configuration.
* Keep helpers used by one test class private or nested. Put reused helpers in the project's established `Helpers`, `Infrastructure`, or `TestObjects` structure.
* We use xUnit SDK v3 for tests.
* Do not emit "Act", "Arrange" or "Assert" comments.
* Use Moq for mocking in tests.
* Copy existing style in nearby files for test method names and capitalization.

## Running tests

* Read the product area's `AGENTS.md` and build wrapper before choosing a validation command. In a fresh worktree, identify the SDK, submodules, generated assets, and native tools required by the intended path. Use the area's `src\<area>\build.cmd -test` on Windows or `./src/<area>/build.sh -test` on Linux/macOS for area-level validation (for example, `src\Http\build.cmd -test` on Windows). Start with the smallest project or documented focused command that faithfully covers the change, then use the area wrapper when its broader integration coverage is relevant. If an unrelated prerequisite stops the wrapper before reaching the changed target, name that prerequisite and report the narrower validation boundary; do not equate targeted validation with the area build or bypass a prerequisite needed by the changed behavior.
* Inspect a changed project's declared target frameworks and compile each target buildable in the current environment before opening or undrafting a pull request. A build of only the newest target does not establish compatibility with other targets; report any targets that could not be built.
* Do not run overlapping `dotnet build` or `dotnet test` graphs concurrently when they share the repository `artifacts` tree. Use sequential invocations unless outputs are isolated or a no-build path has been verified; if `CS2012` or file-in-use/access-denied errors occur, retry the smallest affected build serially (with `/m:1` if needed) before attributing them to the change.
* Check the observable outcome, not only the exit code: confirm that filtered tests selected a nonzero intended set and reached the relevant assertion or behavior, and that build/pack commands produced the required artifacts. A prerequisite failure before the test runs is not a test result.
* Before claiming a bug fix is verified, confirm that the relevant test or check fails for the expected reason without the fix and passes with it. Reading the source or seeing a test pass on its own is not proof that the bug is fixed.
* For a `[Theory]` or other parameterized test, confirm that each row fails for the expected reason without the fix and passes with it; a red test proves only that at least one row failed. `dotnet test --filter` cannot select an individual `InlineData` row by parameter value, so inspect every case in the test output instead of relying on the `Failed!` or `Passed!` summary. A row that passes because its targeted scenario or code path never ran, such as from unmet setup, a missing prerequisite, or conditional execution, does not verify the fix.
* For behavioral review findings and bug-fix verification, use the smallest faithful test path. Include the component, service, runtime, or browser mechanism that owns or produces each disputed precondition, and observe the claimed material effect at the appropriate boundary, such as UI, protocol, persisted state, resource use, timing or performance, logging, or another contract-relevant behavior. Any test establishes only the downstream response, not producer reachability, if it directly injects callbacks or events or otherwise bypasses the owning producer. An isolated test can provide faithful evidence when it exercises the real producer.
* For behavioral findings and bug-fix verification, E2E validation is unnecessary when the disputed preconditions and material effects are fully established at a lower faithful boundary. This does not waive E2E coverage required for shipped implementation work.
* If faithful validation is impractical, state the observed boundary and limitation, and do not describe the behavioral claim as verified.
* If that red/green verification isn't practical, explain why, state what you did verify, and don't describe the fix as verified.
* When a requested automated test cannot reach its assertion, name that test in the final response, state the prerequisite that blocked it, and identify the faithful validation boundary used instead.

## .NET Environment

* Before running any `dotnet` commands in this repository, always activate the locally installed .NET environment first by running the appropriate activation script from the repository root:
  * On Windows: `. ./activate.ps1` (from repository root)
  * On Linux/Mac: `source activate.sh` (from repository root)
* If not in the repository root, navigate there first or use the full path to the activation script.
* This ensures that the correct version of .NET SDK is used for the repository.
* If activation reports the repository SDK missing, run `restore.cmd` on Windows or `./restore.sh` on Linux/macOS from the repository root, wait for it to complete, then reactivate and retry. Do not restore repeatedly for an unrelated failure.
* On Windows, if a focused build fails before product compilation because the `Microsoft.SourceLink.AzureRepos.Git.TranslateRepositoryUrls` task host cannot load, retry that build locally with `-p:EnableSourceControlManagerQueries=false -p:EnableSourceLink=false`. Record that SourceLink was disabled for this diagnostic build and rely on normal CI to validate it; do not change repository build configuration or apply these flags to unrelated failures.

## Pull request maintenance

* Before declaring a PR feedback pass complete, inspect conversation comments, submitted review bodies (including minimized or suppressed content), inline threads (including resolved or outdated threads), requested-changes state, and checks for the current head SHA. Evaluate feedback against the issue and current target branch rather than accepting it blindly; explain with evidence when feedback is obsolete, incorrect, or out of scope.
* Reply within each addressed inline thread, and resolve it only after the response and any fix are pushed. Record which feedback was addressed or declined, the resulting head commit, validation, and outstanding blockers.
* Before opening a PR from a new worktree branch, check the intended head commit and base, publish the head branch, and check for an existing PR. Verify the created PR's head and base; if creation reports an unexpected `main/main` comparison or HTTP 422, inspect the actual branch and publication state before retrying.

## Azure Pipelines and Helix CI

* Resolve every red Azure Pipelines aggregate to its build and timeline job, and, when present, the Helix job/work item, failure stage, exit code, test results, and relevant logs. Distinguish product/test failures from pre-Helix restore, submission, reporter, post-test, merge-conflict, and infrastructure failures; a red aggregate alone is not evidence of a product regression.
* Correlate each failure with the PR diff and build progression. Use Build Analysis, Build Insights/known-issue evidence, and the exact target-parent build's matching job, batch, or test when attribution is unclear; treat classifications and historical rates as leads, not proof.
* Retry only after accounting for every current failure. When GitHub rerequest is unsupported for an external Azure Pipelines check, use a scoped `/azp run <pipeline-name>` comment rather than an empty commit or a retry of an immutable Helix monitor result. Verify the bot acknowledgement, replacement build ID, PR source SHA, and terminal build plus Build Analysis/Build Insights state; do not repeatedly retry an unmatched failure.

## ASP.NET Core Components Area
* When working on issues under the src/Components area, follow the instructions in [./instructions/components.instructions.md](./instructions/components.instructions.md).

## Security Concerns Are Out of Scope

Do NOT assess, discuss, or make recommendations about potential security implications of an issue or a PR. When reviewing a PR, never make any claims about a security vulnerability.
