## General

* Make only high confidence suggestions when reviewing code changes.
* Always use the latest version C#, currently C# 13 features.
* Never change global.json unless explicitly asked to.
* Never change package.json or package-lock.json files unless explicitly asked to.
* Never change NuGet.config files unless explicitly asked to.

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
* For specific tests, use `--filter DisplayName~TestName` syntax rather than other filter formats.
* For running multiple specific tests, use pipe syntax: `--filter DisplayName~FirstTest|DisplayName~SecondTest`.
* Run expensive E2E tests selectively using specific test filters rather than running all tests in a suite.
* For UI/interaction tests, run only one test at a time to avoid browser conflicts.

## Running tests

* To build and run tests in the repo, use the `build.sh` script that is located in each subdirectory within the `src` folder. For example, to run the build with tests in the `src/Http` directory, run `./src/Http/build.sh -test`.
* When debugging specific test failures, you can run individual tests with filtering:
  ```
  dotnet test <project-path> --filter DisplayName~TestMethodName --logger "console;verbosity=normal"
  ```
* When making changes that affect certain scenarios (like prerendering), test those specific scenarios first before running full test suites.
* Build projects locally with `dotnet build <project-path> --no-restore` for faster validation before running tests.
* When working with interactive features, check both server and WebAssembly render modes by running representative tests for each.
