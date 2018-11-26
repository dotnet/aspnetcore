Compile the solution in Release mode (so binaries are available in release)

To run a specific benchmark add it as parameter.
```
dotnet run -c Release --framework <tfm> <benchmark_name>
```

To run all benchmarks use '*' as the name.
```
dotnet run -c Release --framework <tfm> *
```

If you run without any parameters, you'll be offered the list of all benchmarks and get to choose.
```
dotnet run -c Release --framework <tfm> 
```