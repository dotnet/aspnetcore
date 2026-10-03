``` ini

BenchmarkDotNet=v0.13.0, OS=ubuntu 22.04
13th Gen Intel Core i7-13800H, 1 CPU, 20 logical and 10 physical cores
.NET SDK=11.0.100-rc.1.26420.103
  [Host]     : .NET 11.0.0 (11.0.26.45408), X64 RyuJIT
  Job-KVXYWO : .NET 11.0.0 (11.0.26.42103), X64 RyuJIT
  Dry        : .NET 11.0.0 (11.0.26.42103), X64 RyuJIT


```
|                Method |        Job | Server |      Toolchain | IterationCount | LaunchCount | RunStrategy | UnrollFactor | WarmupCount |             Mean |     Error |     StdDev |          Op/s | Ratio |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|---------------------- |----------- |------- |--------------- |--------------- |------------ |------------ |------------- |------------ |-----------------:|----------:|-----------:|--------------:|------:|-------:|------:|------:|----------:|
|   RuntimeRegistration | Job-KVXYWO |   True | .NET Core 11.0 |        Default |     Default |  Throughput |           16 |     Default |      3,060.70 ns | 61.955 ns | 182.676 ns |    326,723.00 | 1.000 | 0.0763 |     - |     - |   4,088 B |
| GeneratedRegistration | Job-KVXYWO |   True | .NET Core 11.0 |        Default |     Default |  Throughput |           16 |     Default |         22.78 ns |  0.481 ns |   1.215 ns | 43,898,266.31 | 0.007 | 0.0017 |     - |     - |      80 B |
|                       |            |        |                |                |             |             |              |             |                  |           |            |               |       |        |       |       |           |
|   RuntimeRegistration |        Dry |  False |        Default |              1 |           1 |   ColdStart |            1 |           1 | 13,655,807.00 ns |        NA |   0.000 ns |         73.23 |  1.00 |      - |     - |     - |   5,528 B |
| GeneratedRegistration |        Dry |  False |        Default |              1 |           1 |   ColdStart |            1 |           1 |  1,071,881.00 ns |        NA |   0.000 ns |        932.94 |  0.08 |      - |     - |     - |     416 B |
