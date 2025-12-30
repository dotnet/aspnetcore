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

## Running tests

* To build and run tests in the repo, use the `build.sh` script that is located in each subdirectory within the `src` folder. For example, to run the build with tests in the `src/Http` directory, run `./src/Http/build.sh -test`.

## .NET Environment

* Before running any `dotnet` commands in this repository, always activate the locally installed .NET environment first by running the appropriate activation script from the repository root:
  * On Windows: `. ./activate.ps1` (from repository root)
  * On Linux/Mac: `source activate.sh` (from repository root)
* If not in the repository root, navigate there first or use the full path to the activation script.
* This ensures that the correct version of .NET SDK is used for the repository.

## ASP.NET Core Components Area
* When working on issues under the src/Components area, follow the instructions in [./instructions/components.instructions.md](./instructions/components.instructions.md).
