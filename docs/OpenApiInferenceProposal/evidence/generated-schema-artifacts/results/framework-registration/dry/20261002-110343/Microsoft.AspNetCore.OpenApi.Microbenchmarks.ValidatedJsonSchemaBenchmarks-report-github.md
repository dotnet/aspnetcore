``` ini

BenchmarkDotNet=v0.13.0, OS=ubuntu 22.04
13th Gen Intel Core i7-13800H, 1 CPU, 20 logical and 10 physical cores
.NET SDK=11.0.100-rc.1.26420.103
  [Host]     : .NET 11.0.0 (11.0.26.45408), X64 RyuJIT
  Job-KVXYWO : .NET 11.0.0 (11.0.26.42103), X64 RyuJIT
  Dry        : .NET 11.0.0 (11.0.26.42103), X64 RyuJIT


```
|                                  Method |        Job | Server |      Toolchain | IterationCount | LaunchCount | RunStrategy | UnrollFactor | WarmupCount |          Mean |    Error |   StdDev |         Op/s | Ratio | RatioSD | Gen 0 | Gen 1 | Gen 2 | Allocated |
|---------------------------------------- |----------- |------- |--------------- |--------------- |------------ |------------ |------------- |------------ |--------------:|---------:|---------:|-------------:|------:|--------:|------:|------:|------:|----------:|
|                             PassThrough | Job-KVXYWO |   True | .NET Core 11.0 |        Default |     Default |  Throughput |           16 |     Default |      11.93 ns | 0.230 ns | 0.237 ns | 83,791,920.6 |  1.00 |    0.00 |     - |     - |     - |         - |
|                  ValidatedFrameworkOnly | Job-KVXYWO |   True | .NET Core 11.0 |        Default |     Default |  Throughput |           16 |     Default |     111.71 ns | 2.214 ns | 4.158 ns |  8,951,728.2 |  9.34 |    0.31 |     - |     - |     - |         - |
|          ValidatedFrameworkOnlyResponse | Job-KVXYWO |   True | .NET Core 11.0 |        Default |     Default |  Throughput |           16 |     Default |     166.42 ns | 3.275 ns | 5.289 ns |  6,008,782.3 | 14.08 |    0.69 |     - |     - |     - |         - |
|         GeneratedValidatedFrameworkOnly | Job-KVXYWO |   True | .NET Core 11.0 |        Default |     Default |  Throughput |           16 |     Default |     110.31 ns | 2.169 ns | 3.378 ns |  9,065,265.4 |  9.35 |    0.32 |     - |     - |     - |         - |
| GeneratedValidatedFrameworkOnlyResponse | Job-KVXYWO |   True | .NET Core 11.0 |        Default |     Default |  Throughput |           16 |     Default |     173.50 ns | 3.386 ns | 4.283 ns |  5,763,527.8 | 14.53 |    0.50 |     - |     - |     - |         - |
|                                         |            |        |                |                |             |             |              |             |               |          |          |              |       |         |       |       |       |           |
|                             PassThrough |        Dry |  False |        Default |              1 |           1 |   ColdStart |            1 |           1 | 172,701.00 ns |       NA | 0.000 ns |      5,790.4 |  1.00 |    0.00 |     - |     - |     - |   1,344 B |
|                  ValidatedFrameworkOnly |        Dry |  False |        Default |              1 |           1 |   ColdStart |            1 |           1 | 413,634.00 ns |       NA | 0.000 ns |      2,417.6 |  2.40 |    0.00 |     - |     - |     - |         - |
|          ValidatedFrameworkOnlyResponse |        Dry |  False |        Default |              1 |           1 |   ColdStart |            1 |           1 | 199,389.00 ns |       NA | 0.000 ns |      5,015.3 |  1.15 |    0.00 |     - |     - |     - |         - |
|         GeneratedValidatedFrameworkOnly |        Dry |  False |        Default |              1 |           1 |   ColdStart |            1 |           1 | 212,906.00 ns |       NA | 0.000 ns |      4,696.9 |  1.23 |    0.00 |     - |     - |     - |   1,344 B |
| GeneratedValidatedFrameworkOnlyResponse |        Dry |  False |        Default |              1 |           1 |   ColdStart |            1 |           1 | 240,898.00 ns |       NA | 0.000 ns |      4,151.1 |  1.39 |    0.00 |     - |     - |     - |   1,344 B |
