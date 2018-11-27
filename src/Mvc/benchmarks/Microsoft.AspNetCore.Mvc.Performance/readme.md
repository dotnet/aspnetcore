Compile the solution in Release mode (so binaries are available in release)

To run a specific benchmark add it as parameter
```
dotnet run -c Release <benchmark_name>
```
To run all use `All` as parameter
```
dotnet run -c Release All
```
Using no parameter will list all available benchmarks