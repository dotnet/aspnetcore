``` ini

BenchmarkDotNet=v0.13.0, OS=ubuntu 22.04
13th Gen Intel Core i7-13800H, 1 CPU, 20 logical and 10 physical cores
.NET SDK=11.0.100-rc.1.26420.103
  [Host]     : .NET 11.0.0 (11.0.26.45408), X64 RyuJIT
  DefaultJob : .NET 11.0.0 (11.0.26.42103), X64 RyuJIT


```
|                     Method |        Mean |        Error |       StdDev |      Median | Ratio | RatioSD |  Gen 0 |  Gen 1 | Gen 2 | Allocated |
|--------------------------- |------------:|-------------:|-------------:|------------:|------:|--------:|-------:|-------:|------:|----------:|
|              CompileBundle | 30,509.3 ns |  1,258.90 ns |  3,692.15 ns | 29,535.1 ns | 1.000 |    0.00 | 4.8828 | 0.4883 |     - |  63,027 B |
|           LoadProgramImage | 10,680.2 ns |    753.62 ns |  2,222.06 ns | 10,295.5 ns | 0.358 |    0.10 | 3.6316 | 0.3815 |     - |  45,752 B |
|              CompiledValid |    343.3 ns |      8.19 ns |     23.77 ns |    339.6 ns | 0.011 |    0.00 | 0.0134 |      - |     - |     168 B |
|            CompiledInvalid |    323.2 ns |     14.55 ns |     41.51 ns |    313.9 ns | 0.011 |    0.00 | 0.0134 |      - |     - |     168 B |
|                 ImageValid |    318.0 ns |      9.50 ns |     27.10 ns |    312.1 ns | 0.010 |    0.00 | 0.0134 |      - |     - |     168 B |
|               ImageInvalid |    295.0 ns |      8.58 ns |     24.88 ns |    290.3 ns | 0.010 |    0.00 | 0.0134 |      - |     - |     168 B |
|   JsonSchemaNetNativeValid | 83,507.8 ns | 11,095.17 ns | 32,714.36 ns | 86,373.4 ns | 2.831 |    1.20 | 1.4648 |      - |     - |  19,642 B |
| JsonSchemaNetNativeInvalid | 93,591.6 ns | 19,011.44 ns | 55,757.27 ns | 65,111.1 ns | 3.102 |    1.85 | 1.9531 |      - |     - |  25,460 B |
