``` ini

BenchmarkDotNet=v0.13.0, OS=ubuntu 22.04
13th Gen Intel Core i7-13800H, 1 CPU, 20 logical and 10 physical cores
.NET SDK=11.0.100-rc.1.26420.103
  [Host]     : .NET 11.0.0 (11.0.26.45408), X64 RyuJIT
  Job-YFNDBW : .NET 11.0.0 (11.0.26.42103), X64 RyuJIT

Server=True  Toolchain=.NET Core 11.0  RunStrategy=Throughput

```
|                          Method |          Mean |       Error |      StdDev |        Median |            Op/s |  Ratio | RatioSD |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|-------------------------------- |--------------:|------------:|------------:|--------------:|----------------:|-------:|--------:|-------:|------:|------:|----------:|
|                     PassThrough |    26.6890 ns |   1.6918 ns |   4.9883 ns |    26.5790 ns |    37,468,665.5 |   1.00 |    0.00 |      - |     - |     - |         - |
|          ValidatedFrameworkOnly |   260.7751 ns |  20.3523 ns |  60.0092 ns |   251.0916 ns |     3,834,722.1 |  10.07 |    2.80 |      - |     - |     - |         - |
|  ValidatedFrameworkOnlyResponse |   564.6002 ns |  44.0005 ns | 129.7363 ns |   579.5550 ns |     1,771,164.8 |  21.95 |    6.60 |      - |     - |     - |         - |
|                   ValidatorNoOp |     0.8785 ns |   0.1499 ns |   0.4421 ns |     0.7734 ns | 1,138,253,684.4 |   0.03 |    0.02 |      - |     - |     - |         - |
|                     CorvusValid |   284.7105 ns |  31.8466 ns |  93.9005 ns |   292.9418 ns |     3,512,340.1 |  10.91 |    3.96 | 0.0033 |     - |     - |     168 B |
|              JsonSchemaNetValid | 1,603.8690 ns | 189.9686 ns | 554.1474 ns | 1,410.0420 ns |       623,492.3 |  62.75 |   26.24 | 0.0305 |     - |     - |   1,616 B |
|        CorvusInvalidDiagnostics |   204.3949 ns |  17.8571 ns |  52.6519 ns |   201.5671 ns |     4,892,490.5 |   7.98 |    2.70 | 0.0057 |     - |     - |     288 B |
| JsonSchemaNetInvalidDiagnostics | 4,019.0986 ns | 165.1442 ns | 479.1133 ns | 3,994.8445 ns |       248,812.0 | 157.37 |   36.07 | 0.0610 |     - |     - |   3,080 B |
