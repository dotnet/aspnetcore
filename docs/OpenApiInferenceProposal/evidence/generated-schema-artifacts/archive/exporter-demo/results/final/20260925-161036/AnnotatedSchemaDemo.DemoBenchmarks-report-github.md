``` ini

BenchmarkDotNet=v0.13.0, OS=ubuntu 22.04
13th Gen Intel Core i7-13800H, 1 CPU, 20 logical and 10 physical cores
.NET SDK=11.0.100-rc.1.26420.103
  [Host]     : .NET 11.0.0 (11.0.26.45408), X64 RyuJIT
  DefaultJob : .NET 11.0.0 (11.0.26.42103), X64 RyuJIT


```
|                     Method |      Mean |      Error |     StdDev |    Median | Ratio | RatioSD |  Gen 0 |  Gen 1 | Gen 2 | Allocated |
|--------------------------- |----------:|-----------:|-----------:|----------:|------:|--------:|-------:|-------:|------:|----------:|
|      ExportSupportedBundle | 64.039 μs |  1.1624 μs |  1.0304 μs | 63.985 μs |  1.60 |    0.08 | 1.9531 |      - |     - |  95,848 B |
|              CompileCorvus | 39.283 μs |  0.9028 μs |  2.6336 μs | 39.206 μs |  1.00 |    0.00 | 1.4648 |      - |     - |  73,514 B |
|            LoadCorvusImage | 20.899 μs |  0.7627 μs |  2.1635 μs | 21.050 μs |  0.53 |    0.05 | 1.0986 | 0.0916 |     - |  53,104 B |
|   JsonSchemaNetNativeValid | 49.857 μs | 11.6837 μs | 33.8967 μs | 37.644 μs |  1.28 |    0.89 |      - |      - |     - |  16,553 B |
| JsonSchemaNetNativeInvalid | 48.311 μs | 10.4267 μs | 30.5796 μs | 36.327 μs |  1.25 |    0.80 |      - |      - |     - |  14,864 B |
|   JsonSchemaNetBundleValid | 35.402 μs |  4.8143 μs | 13.1791 μs | 32.606 μs |  0.92 |    0.35 |      - |      - |     - |  18,401 B |
| JsonSchemaNetBundleInvalid | 35.253 μs |  6.4151 μs | 18.0938 μs | 29.068 μs |  0.91 |    0.47 |      - |      - |     - |  17,521 B |
|           CorvusImageValid |  1.528 μs |  0.1102 μs |  0.3215 μs |  1.555 μs |  0.04 |    0.01 | 0.0019 |      - |     - |     168 B |
|         CorvusImageInvalid |  1.233 μs |  0.1417 μs |  0.4111 μs |  1.193 μs |  0.03 |    0.01 | 0.0029 |      - |     - |     168 B |
|         CorvusBindingValid |  1.754 μs |  0.1514 μs |  0.4439 μs |  1.722 μs |  0.04 |    0.01 | 0.0019 |      - |     - |     168 B |
