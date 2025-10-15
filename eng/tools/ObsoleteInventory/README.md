# ObsoleteInventory

This tool generates an inventory of all APIs currently marked with the `[Obsolete]` attribute in the repository.

## Purpose

The tool scans all C# source files in the `src/` directory and:
- Identifies APIs marked with `[Obsolete]` or `[ObsoleteAttribute]`
- Determines when each obsolete marking was first introduced using git blame
- Generates a Markdown report (`ObsoletedApis.md`) at the repository root

## Usage

### Generate the report

From the repository root:

```bash
dotnet run --project eng/tools/ObsoleteInventory/ObsoleteInventory.csproj -c Release
```

This will generate (or update) `ObsoletedApis.md` at the repository root.

### Verify the report is up to date

```bash
dotnet run --project eng/tools/ObsoleteInventory/ObsoleteInventory.csproj -c Release -- --verify
```

This will check if the existing `ObsoletedApis.md` file matches what would be generated. Exit code 0 means it's up to date, non-zero means it needs to be regenerated.

## Report Format

The generated report includes:
- **API**: Fully qualified signature of the obsolete API
- **Kind**: Type of API (Type, Method, Property, Field, Event, etc.)
- **IntroducedObsoleteDate**: Date when the `[Obsolete]` attribute was first added
- **Commit**: Git commit hash (linked to GitHub) where the obsolete marking was introduced
- **IsError**: Whether the obsolete attribute has `error: true`
- **Message**: The obsolete message string
- **File**: Repository-relative path to the source file

APIs are sorted by introduction date (oldest first).

## Exclusions

The tool automatically excludes:
- Test projects and files (directories containing `test/`, `tests/`)
- Benchmark projects (directories containing `benchmark/`, `benchmarks/`)
- Sample projects (directories containing `sample/`, `samples/`)
- Build artifacts (`obj/`, `bin/`)
- Engineering and tool directories (`eng/`, `tools/`)
- Auto-generated files (files with auto-generated headers in the first 5 non-empty lines)

## Future CI Integration

The `--verify` option can be used in CI to ensure the report stays up to date:

```bash
# In a CI script
dotnet run --project eng/tools/ObsoleteInventory/ObsoleteInventory.csproj -c Release -- --verify
```
