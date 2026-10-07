``` ini

BenchmarkDotNet=v0.13.0, OS=ubuntu 22.04
13th Gen Intel Core i7-13800H, 1 CPU, 20 logical and 10 physical cores
.NET SDK=11.0.100-rc.1.26420.103
  [Host]     : .NET 11.0.0 (11.0.26.45408), X64 RyuJIT
  Job-UGNHQP : .NET 11.0.0 (11.0.26.42103), X64 RyuJIT
  ShortRun   : .NET 11.0.0 (11.0.26.42103), X64 RyuJIT


```
|                                  Method |        Job | Server |      Toolchain | IterationCount | LaunchCount | RunStrategy | WarmupCount |      Mean |      Error |    StdDev |    Median |         Op/s | Ratio | RatioSD | Gen 0 | Gen 1 | Gen 2 | Allocated |
|---------------------------------------- |----------- |------- |--------------- |--------------- |------------ |------------ |------------ |----------:|-----------:|----------:|----------:|-------------:|------:|--------:|------:|------:|------:|----------:|
|                             PassThrough | Job-UGNHQP |   True | .NET Core 11.0 |        Default |     Default |  Throughput |     Default |  11.85 ns |   0.237 ns |  0.243 ns |  11.81 ns | 84,354,524.4 |  1.00 |    0.00 |     - |     - |     - |         - |
|                  ValidatedFrameworkOnly | Job-UGNHQP |   True | .NET Core 11.0 |        Default |     Default |  Throughput |     Default | 115.50 ns |   2.557 ns |  7.499 ns | 112.73 ns |  8,658,335.8 | 10.39 |    0.64 |     - |     - |     - |         - |
|          ValidatedFrameworkOnlyResponse | Job-UGNHQP |   True | .NET Core 11.0 |        Default |     Default |  Throughput |     Default | 185.42 ns |   4.813 ns | 13.810 ns | 181.10 ns |  5,393,248.7 | 14.69 |    0.83 |     - |     - |     - |         - |
|         GeneratedValidatedFrameworkOnly | Job-UGNHQP |   True | .NET Core 11.0 |        Default |     Default |  Throughput |     Default | 120.01 ns |   0.972 ns |  0.759 ns | 120.23 ns |  8,332,712.1 | 10.05 |    0.22 |     - |     - |     - |         - |
| GeneratedValidatedFrameworkOnlyResponse | Job-UGNHQP |   True | .NET Core 11.0 |        Default |     Default |  Throughput |     Default | 219.24 ns |  10.660 ns | 30.926 ns | 216.02 ns |  4,561,268.4 | 19.02 |    2.59 |     - |     - |     - |         - |
|                                         |            |        |                |                |             |             |             |           |            |           |           |              |       |         |       |       |       |           |
|                             PassThrough |   ShortRun |  False |        Default |              3 |           1 |     Default |           3 |  18.66 ns |  39.765 ns |  2.180 ns |  18.68 ns | 53,577,356.9 |  1.00 |    0.00 |     - |     - |     - |         - |
|                  ValidatedFrameworkOnly |   ShortRun |  False |        Default |              3 |           1 |     Default |           3 | 130.60 ns | 157.203 ns |  8.617 ns | 130.11 ns |  7,657,151.6 |  7.04 |    0.74 |     - |     - |     - |         - |
|          ValidatedFrameworkOnlyResponse |   ShortRun |  False |        Default |              3 |           1 |     Default |           3 | 236.18 ns | 571.486 ns | 31.325 ns | 241.70 ns |  4,234,129.6 | 12.64 |    0.33 |     - |     - |     - |         - |
|         GeneratedValidatedFrameworkOnly |   ShortRun |  False |        Default |              3 |           1 |     Default |           3 | 117.93 ns |  18.041 ns |  0.989 ns | 118.14 ns |  8,479,805.8 |  6.38 |    0.80 |     - |     - |     - |         - |
| GeneratedValidatedFrameworkOnlyResponse |   ShortRun |  False |        Default |              3 |           1 |     Default |           3 | 183.68 ns | 147.770 ns |  8.100 ns | 187.89 ns |  5,444,120.3 |  9.96 |    1.55 |     - |     - |     - |         - |
