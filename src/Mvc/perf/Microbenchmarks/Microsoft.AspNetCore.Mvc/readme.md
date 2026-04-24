Compile the solution in Release mode (so binaries are available in release)

To run all benchmarks matching a name filter:

```
dotnet run -c Release -- --filter *<filter>*
```

e.g. to run all benchmarks in the `WriteLiteralUtf8Benchmark` class:

```
dotnet run -c Release -- --filter *WriteLiteralUtf8Benchmark*
```

To run a specific benchmark specify its full name as parameter:

```
dotnet run -c Release -- <benchmark_name>
```

To run all use `All` as parameter:

```
dotnet run -c Release -- All
```

Using no parameter will list all available benchmarks and allow to select one.