# Fuzzing ASP.NET Core

This project contains fuzzing targets for various ASP.NET Core libraries, as well as supporting code for generating OneFuzz deployments from them.
Targets are instrumented using [SharpFuzz](https://github.com/Metalnem/sharpfuzz), and run using [libFuzzer](https://llvm.org/docs/LibFuzzer.html).

Useful links:
- [libFuzzer documentation](https://llvm.org/docs/LibFuzzer.html)
- [libFuzzer tutorial with examples](https://github.com/google/fuzzing/blob/master/tutorial/libFuzzerTutorial.md)
- [More SharpFuzz samples](https://github.com/Metalnem/dotnet-fuzzers)
- [Runtime fuzzing infrastructure](https://github.com/dotnet/runtime/tree/main/src/libraries/Fuzzing) (the project this was modeled after)

## Running locally

> [!NOTE]
> The instructions assume you are running on Windows as that is what the continuous fuzzing runs currently use.

### Prerequisites

Activate the local .NET environment from the repo root:
```cmd
.\activate.ps1
```

Install the SharpFuzz command line tool:
```cmd
dotnet tool install --global SharpFuzz.CommandLine
```

### Fuzzing locally

Build the `AspNetCoreFuzzing` fuzzing project. It is self-contained, so it will produce `AspNetCoreFuzzing.exe` along with a copy of all required libraries.

```cmd
cd src/Fuzzing/AspNetCoreFuzzing

dotnet build
```

Run `run.bat`, which will create separate directories for each fuzzing target, instrument the relevant assemblies, and generate a helper script for running them locally.
When iterating on changes, remember to rebuild the project again.

```cmd
dotnet build && run.bat
```

Start fuzzing by running the `local-run.bat` script in the folder of the fuzzer you are interested in.
```cmd
deployment\MultipartReaderFuzzer\local-run.bat
```

By default, libFuzzer will run indefinitely, generating and testing random inputs. Press `Ctrl+C` to stop. When it discovers interesting inputs or crashes, it writes files to the current directory:

- `crash-<hash>` — an input that caused the fuzzer to crash. **This is a bug.**
- `timeout-<hash>` — an input that caused the fuzzer to hang beyond the timeout. **This may be a bug.**

See the [libFuzzer options](https://llvm.org/docs/LibFuzzer.html#options) documentation for more information on how to customize the fuzzing process.

**Useful options:**

| Option | Description |
|--------|-------------|
| `-max_total_time=600` | Stop after 600 seconds (10 minutes) |
| `-timeout=30` | Treat any single input taking >30s as a timeout |
| `-jobs=5` | Run 5 parallel fuzzer instances |
| `-max_len=1024` | Limit generated input size to 1024 bytes |

For example, here is how you can run the fuzzer against a `multipart-inputs` corpus directory for 10 minutes, running multiple instances in parallel:
```cmd
deployment\MultipartReaderFuzzer\local-run.bat multipart-inputs -timeout=30 -max_total_time=600 -jobs=5
```

### Investigating crashes and timeouts

When libFuzzer finds a problem, it writes a `crash-<hash>` or `timeout-<hash>` file. To reproduce and debug:

```cmd
cd src/Fuzzing/AspNetCoreFuzzing

:: Reproduce a single crash file
dotnet run -- MultipartReaderFuzzer C:\path\to\crash-abc123

:: Reproduce all crash/timeout files in a directory
dotnet run -- MultipartReaderFuzzer C:\path\to\directory-with-crash-files\
```

> [!TIP]
> Since the project is self-contained, you can also run the exe directly without `dotnet run`:
> ```cmd
> artifacts\bin\AspNetCoreFuzzing\Debug\net11.0\win-x64\AspNetCoreFuzzing.exe MultipartReaderFuzzer crash-abc123
> ```

To debug interactively, set a breakpoint in the relevant fuzzer's `FuzzTarget` method and launch with a debugger:
```cmd
dotnet run --no-build -- MultipartReaderFuzzer crash-abc123
```

### Available fuzzers

To list all discovered fuzzers, run the program with no arguments:
```cmd
dotnet run --no-build
```

### Generating coverage reports

After letting the fuzzer run for a while, you can use the generated inputs to test code coverage.

```cmd
mkdir multipart-inputs
deployment\MultipartReaderFuzzer\local-run.bat multipart-inputs

.\collect-coverage.ps1 MultipartReaderFuzzer multipart-inputs
```

The HTML report can be opened from
```cmd
.\coverage-report\html\index.html
```

## Creating a new fuzzing target

To create a new fuzzing target, you need to create a new class that implements the `IFuzzer` interface.
See existing implementations in the `Fuzzers` directory for reference.

As an example, let's test that `MediaTypeHeaderValue.TryParse` never throws on invalid input:
```csharp
internal sealed class MediaTypeFuzzer : IFuzzer
{
    public string[] TargetAssemblies => ["Microsoft.Net.Http.Headers"];

    public void FuzzTarget(ReadOnlySpan<byte> bytes)
    {
        string input = Encoding.UTF8.GetString(bytes);
        _ = MediaTypeHeaderValue.TryParse(input, out _);
    }
}
```

- `TargetAssemblies` is a list of assemblies where the tested code lives and that must be instrumented.
- `FuzzTarget` is the logic that the fuzzer will run for every test input. It should exercise code from the target assemblies.

Once you've created the new target, you can follow instructions above to run it locally.
Targets are discovered via reflection, so they will automatically become available for local runs and continuous fuzzing in CI.

> [!NOTE]
> The CI pipeline entries in `.azure/pipelines/fuzzing/deploy-to-onefuzz.yml` are auto-generated.
> When you run `run.bat` (or `prepare-onefuzz`), the tool updates the YAML between the
> `ONEFUZZ_TASK_WORKAROUND_START` / `ONEFUZZ_TASK_WORKAROUND_END` markers automatically.
> Commit the resulting YAML changes alongside your new fuzzer.

### Running against a sample input

The program accepts two arguments: the name of the fuzzer and the path to a sample input file or directory.
To run the `MultipartReaderFuzzer` target against the `inputs` directory, use the following command:

```cmd
cd src/Fuzzing/AspNetCoreFuzzing

dotnet run -- MultipartReaderFuzzer inputs
```

You can also pass a single file:
```cmd
dotnet run -- MultipartReaderFuzzer inputs/sample.bin
```

This is useful for regression testing — save interesting inputs or crash files and re-run them after a fix to confirm the issue is resolved.
