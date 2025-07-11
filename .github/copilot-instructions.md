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
* Do not add XML documentation comments to non-public API members (internal, private). Only public APIs should have XML documentation.
* Do not add comments that explain the code itself, such as "This method does X". Only add comments that explain why a specific approach was chosen or why something is done in a particular way.
  * Comments are needed when the code relies on specific behavior or implementation details of another component or system.

### Nullable Reference Types

* Declare variables non-nullable, and check for `null` at entry points.
* Always use `is null` or `is not null` instead of `== null` or `!= null`.
* Trust the C# null annotations and don't add null checks when the type system says a value cannot be null.

### Testing

* We use xUnit SDK v3 for tests.
* Do not emit "Act", "Arrange" or "Assert" comments.
* Use Moq for mocking in tests.
* Copy existing style in nearby files for test method names and capitalization.
* Do not test simple members.
  * As a rule of thumb, if a member does not contain any control flow instruction, you don't need to test it.
  * Do not test constructor parameters for null or any internal methods for null.

## Running tests

* To build and run tests in the repo, use the `build.sh` script that is located in each subdirectory within the `src` folder. For example, to run the build with tests in the `src/Http` directory, run `./src/Http/build.sh -test`.
* When adding new tests, ALWAYS run the specific tests you are adding before running the full test suite.
* When your new tests pass, ALWAYS run the tests on the class before running the full test suite.
* ALWAYS ask for confirmation before running the full test suite.
* When debugging specific test failures, you can run individual tests with filtering:
  ```
  dotnet test <project-path> --filter DisplayName~TestMethodName --logger "console;verbosity=normal"
  ```
* For running multiple specific tests, use pipe syntax: `--filter DisplayName~FirstTest|DisplayName~SecondTest`.
* Build projects locally with `dotnet build <project-path> --no-restore` for faster validation before running tests.
* For UI/interaction tests, run only one test at a time using filters as those tests are very expensive to run.

