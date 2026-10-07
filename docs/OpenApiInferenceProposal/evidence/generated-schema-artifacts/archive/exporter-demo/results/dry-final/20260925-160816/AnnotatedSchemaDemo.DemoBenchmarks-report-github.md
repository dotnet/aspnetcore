``` ini

BenchmarkDotNet=v0.13.0, OS=ubuntu 22.04
13th Gen Intel Core i7-13800H, 1 CPU, 20 logical and 10 physical cores
.NET SDK=11.0.100-rc.1.26420.103
  [Host] : .NET 11.0.0 (11.0.26.45408), X64 RyuJIT
  Dry    : .NET 11.0.0 (11.0.26.42103), X64 RyuJIT

Job=Dry  IterationCount=1  LaunchCount=1
RunStrategy=ColdStart  UnrollFactor=1  WarmupCount=1

```
|                     Method |      Mean | Error | Ratio | Gen 0 | Gen 1 | Gen 2 | Allocated |
|--------------------------- |----------:|------:|------:|------:|------:|------:|----------:|
|      ExportSupportedBundle | 19.499 ms |    NA | 12.88 |     - |     - |     - |     95 KB |
|              CompileCorvus |  1.514 ms |    NA |  1.00 |     - |     - |     - |     73 KB |
|            LoadCorvusImage |  1.178 ms |    NA |  0.78 |     - |     - |     - |     53 KB |
|   JsonSchemaNetNativeValid | 38.142 ms |    NA | 25.20 |     - |     - |     - |     18 KB |
| JsonSchemaNetNativeInvalid | 32.667 ms |    NA | 21.58 |     - |     - |     - |     16 KB |
|   JsonSchemaNetBundleValid | 26.536 ms |    NA | 17.53 |     - |     - |     - |     20 KB |
| JsonSchemaNetBundleInvalid | 25.454 ms |    NA | 16.82 |     - |     - |     - |     19 KB |
|           CorvusImageValid |  8.117 ms |    NA |  5.36 |     - |     - |     - |      1 KB |
|         CorvusImageInvalid |  7.683 ms |    NA |  5.08 |     - |     - |     - |      1 KB |
|         CorvusBindingValid |  9.581 ms |    NA |  6.33 |     - |     - |     - |      1 KB |
