# Benchmarks

## Instructions

### Run All Benchmarks

To run all use `*` as parameter
```
dotnet run -c Release -- *
```

### Interactive Mode

To see the list of benchmarks run (and choose interactively):
```
dotnet run -c Release
```

### Run Specific Benchmark

To run a specific benchmark add it as parameter
```
dotnet run -c Release -- <benchmark_name>
```


## Troubleshooting

The runner will create logs in the `<project>\BenchmarkDotNet.Artifacts` directory. That should include a lot more information
than what gets printed to the console.

## Results

Also in the `<project>\BenchmarkDotNet.Artifacts\results` directive you'll find some markdown-formatted tables suitable for posting
in a github comment.