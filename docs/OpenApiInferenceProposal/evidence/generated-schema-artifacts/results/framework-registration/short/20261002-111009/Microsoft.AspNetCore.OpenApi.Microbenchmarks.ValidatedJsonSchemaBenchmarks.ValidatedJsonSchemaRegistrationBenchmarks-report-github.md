``` ini

BenchmarkDotNet=v0.13.0, OS=ubuntu 22.04
13th Gen Intel Core i7-13800H, 1 CPU, 20 logical and 10 physical cores
.NET SDK=11.0.100-rc.1.26420.103
  [Host]     : .NET 11.0.0 (11.0.26.45408), X64 RyuJIT
  Job-UGNHQP : .NET 11.0.0 (11.0.26.42103), X64 RyuJIT
  ShortRun   : .NET 11.0.0 (11.0.26.42103), X64 RyuJIT


```
|                Method |        Job | Server |      Toolchain | IterationCount | LaunchCount | RunStrategy | WarmupCount |        Mean |        Error |     StdDev |      Median |         Op/s | Ratio |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|---------------------- |----------- |------- |--------------- |--------------- |------------ |------------ |------------ |------------:|-------------:|-----------:|------------:|-------------:|------:|-------:|------:|------:|----------:|
|   RuntimeRegistration | Job-UGNHQP |   True | .NET Core 11.0 |        Default |     Default |  Throughput |     Default | 3,658.58 ns |   155.442 ns | 438.426 ns | 3,587.34 ns |    273,330.1 | 1.000 | 0.0763 |     - |     - |   4,088 B |
| GeneratedRegistration | Job-UGNHQP |   True | .NET Core 11.0 |        Default |     Default |  Throughput |     Default |    25.16 ns |     0.779 ns |   2.296 ns |    24.45 ns | 39,752,241.0 | 0.007 | 0.0017 |     - |     - |      80 B |
|                       |            |        |                |                |             |             |             |             |              |            |             |              |       |        |       |       |           |
|   RuntimeRegistration |   ShortRun |  False |        Default |              3 |           1 |     Default |           3 | 4,379.93 ns | 2,033.722 ns | 111.475 ns | 4,441.69 ns |    228,314.3 | 1.000 | 0.3204 |     - |     - |   4,088 B |
| GeneratedRegistration |   ShortRun |  False |        Default |              3 |           1 |     Default |           3 |    13.43 ns |    18.480 ns |   1.013 ns |    13.42 ns | 74,440,249.4 | 0.003 | 0.0063 |     - |     - |      80 B |
