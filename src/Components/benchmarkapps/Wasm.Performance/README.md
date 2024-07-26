## Blazor WASM benchmarks

These projects assist in Benchmarking Components.
See https://github.com/aspnet/Benchmarks#benchmarks for usage guidance on using the Benchmarking tool with your application

### Running the benchmarks

The TestApp is a regular BlazorWASM project and can be run using `dotnet run`. The Driver is an app that uses Playwright to launch a browser and run the test app, reporting benchmark results in Crank's protocol format. You generally do not need to run the Driver locally, you can do so if needed via `dotnet run`.

The benchmark app can also be run using [Crank](https://github.com/dotnet/crank?tab=readme-ov-file). To run the benchmark app in the Benchmark server, follow the Crank installation steps and then run:

```
crank --config https://github.com/dotnet/aspnetcore/blob/main/src/Components/benchmarkapps/Wasm.Performance/benchmarks.compose.json?raw=true --config https://github.com/aspnet/Benchmarks/blob/main/scenarios/aspnet.profiles.yml?raw=true --scenario blazorwasmbenchmark --profile aspnet-perf-lin
```

If you have local changes that you'd like to benchmark, the easiest way is to push your local changes and tell the server to use your branch:

```
crank --config https://github.com/dotnet/aspnetcore/blob/main/src/Components/benchmarkapps/Wasm.Performance/benchmarks.compose.json?raw=true --config https://github.com/aspnet/Benchmarks/blob/main/scenarios/aspnet.profiles.yml?raw=true --scenario blazorwasmbenchmark --profile aspnet-perf-lin --application.buildArguments gitBranch=myLocalChanges --application.source.branchOrCommit myLocalChanges
```
