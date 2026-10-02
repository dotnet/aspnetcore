``` ini

BenchmarkDotNet=v0.13.0, OS=ubuntu 22.04
13th Gen Intel Core i7-13800H, 1 CPU, 20 logical and 10 physical cores
.NET SDK=11.0.100-rc.1.26420.103
  [Host]   : .NET 11.0.0 (11.0.26.45408), X64 RyuJIT
  ShortRun : .NET 11.0.0 (11.0.26.42103), X64 RyuJIT

Job=ShortRun  IterationCount=3  LaunchCount=1
WarmupCount=3

```
|                     Method |         Mean |         Error |       StdDev | Ratio | RatioSD |  Gen 0 |  Gen 1 | Gen 2 | Allocated |
|--------------------------- |-------------:|--------------:|-------------:|------:|--------:|-------:|-------:|------:|----------:|
|      ExportSupportedBundle | 124,061.8 ns | 642,926.18 ns | 35,240.95 ns | 3.299 |    0.89 | 1.9531 |      - |     - |  95,881 B |
|              CompileCorvus |  37,502.0 ns |  10,179.78 ns |    557.99 ns | 1.000 |    0.00 | 1.4648 |      - |     - |  73,512 B |
|            LoadCorvusImage |  19,601.0 ns |  42,251.95 ns |  2,315.97 ns | 0.523 |    0.06 | 1.0986 | 0.0305 |     - |  53,085 B |
|   JsonSchemaNetNativeValid |  11,213.9 ns |   4,577.32 ns |    250.90 ns | 0.299 |    0.01 | 0.3052 |      - |     - |  16,552 B |
| JsonSchemaNetNativeInvalid |  12,384.2 ns |   9,680.95 ns |    530.65 ns | 0.330 |    0.01 | 0.3052 |      - |     - |  14,864 B |
|   JsonSchemaNetBundleValid |  21,275.3 ns |  83,532.13 ns |  4,578.68 ns | 0.566 |    0.11 | 0.3662 |      - |     - |  18,400 B |
| JsonSchemaNetBundleInvalid |  13,224.2 ns |  16,085.98 ns |    881.73 ns | 0.353 |    0.03 | 0.3662 |      - |     - |  17,520 B |
|           CorvusImageValid |     410.9 ns |      83.13 ns |      4.56 ns | 0.011 |    0.00 | 0.0033 |      - |     - |     168 B |
|         CorvusImageInvalid |     298.5 ns |     293.05 ns |     16.06 ns | 0.008 |    0.00 | 0.0033 |      - |     - |     168 B |
|         CorvusBindingValid |     392.0 ns |     129.91 ns |      7.12 ns | 0.010 |    0.00 | 0.0033 |      - |     - |     168 B |
