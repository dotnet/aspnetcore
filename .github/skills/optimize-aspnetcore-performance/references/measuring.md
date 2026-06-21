# Measuring performance in this repo

Confirm a path is hot before optimizing it, and prove a non-trivial change helps before committing it. This repo has standard tools for both. Activate the local SDK first: `. ./activate.ps1` on Windows or `source activate.sh` on Linux/Mac from the repo root.

## Microbenchmarks (BenchmarkDotNet)

Most product areas have a `perf/` or `benchmarks/` project next to `src/` (for example `src/Http/Http/perf`, `src/Components/Components/perf`, `src/DataProtection/benchmarks`). Add or extend a benchmark there rather than writing a throwaway timing loop. Match the style of the existing benchmarks in that area.

```csharp
[MemoryDiagnoser]                 // allocations matter as much as time here
public class HeaderParsingBenchmarks
{
    [Benchmark(Baseline = true)]
    public void Before() { /* old path */ }

    [Benchmark]
    public void After() { /* new path */ }
}
```

Run in Release, outside the debugger. Read the `Allocated` column: most wins in this repo are removed allocations on per-request paths, which show up there before they show in raw time. Use `[DisassemblyDiagnoser]` when a change is codegen-sensitive (sealing, inlining, bounds checks).

## End-to-end scenarios (Crank and the perf lab)

For component-level throughput (Kestrel, routing, MVC), use Crank against the perf lab, as described in `docs/Benchmarks.md`.

- Install: `dotnet tool install Microsoft.Crank.Controller --version "0.2.0-*" --global`.
- Copy a scenario's command line from the perf dashboard, then add `--application.options.outputFiles <your release build output>` to test local changes.
- On a PR, comment `/benchmark <scenario> <profile> <component>` (for example `/benchmark json aspnet-citrine-lin kestrel`). Only changes inside the named component project are measured.

Use these for the TechEmpower-style scenarios (`plaintext`, `json`, `fortunes`) when a change touches a request hot path end to end.

## Confirm a path is hot first

Do not optimize on a guess. Profile to find where time and allocations actually concentrate, then optimize that.

- `dotnet-counters monitor -p <pid>` for live allocation rate, GC, and threadpool queue length.
- `dotnet-trace collect -p <pid>` then inspect in PerfView or speedscope.
- The Crank scenarios above also collect counters and traces when requested.

Cold paths (startup, configuration, options binding done once) do not need this work. See [decision-framework.md](decision-framework.md).

## Inspecting generated code

For sealing, inlining, devirtualization, and bounds-check changes, verify the JIT did what you expect rather than assuming.

- `DOTNET_JitDisasm=<MethodName>` prints the generated assembly.
- `DOTNET_TieredCompilation=0` to see final-tier code without tiering noise.
- BenchmarkDotNet `[DisassemblyDiagnoser]`.

## Run the tests after optimizing

A faster path that changes behavior is a regression. Build and test the affected area with its script, for example `./src/Http/build.sh -test`. Keep the new benchmark in the repo so the win can be re-measured later.
