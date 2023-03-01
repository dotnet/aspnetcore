# Running the SSR benchmark

## To run the benchmark locally

1. Install Crank *and* Crank Agent as per instructions at https://github.com/dotnet/crank/blob/main/docs/getting_started.md
1. In a command prompt in any directory, start the agent by running `crank-agent`. Leave that running.
1. In a command prompt in this directory, build and this project via `dotnet build -c Release` and optionally test it works using `dotnet run -c Release`
1. Now submit the crank job:

    crank --config ssr-benchmarks.yml --scenario ssr --profile local

## To run the job in the ASP.NET Core perf infrastructure (works for .NET team only)

1. Install Crank as per instructions at https://github.com/dotnet/crank/blob/main/docs/getting_started.md (no need for the agent)
1. In a command prompt in this directory, build and this project via `dotnet build -c Release` and optionally test it works using `dotnet run -c Release`
1. Now submit the crank job. You can use `aspnet-perf-lin` or `aspnet-perf-win`, e.g.:

    crank --config ssr-benchmarks.yml --scenario ssr --profile aspnet-perf-lin

## Notes

You may have to update the `runtimeVersion` in `benchmarks.yml`. Ideally it would be possible to specify `latest` there but that seems not to work currently.

The benchmark uses the ASP.NET Core dlls you're building locally (it zips and uploads your files from `artifacts/bin/...`). It does not rebuild the entire repo on the Crank agent, as that would take too long for inner-loop.
