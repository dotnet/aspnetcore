``` ini

BenchmarkDotNet=v0.13.0, OS=ubuntu 22.04
13th Gen Intel Core i7-13800H, 1 CPU, 20 logical and 10 physical cores
.NET SDK=11.0.100-rc.1.26420.103
  [Host] : .NET 11.0.0 (11.0.26.45408), X64 RyuJIT
  Dry    : .NET 11.0.0 (11.0.26.42103), X64 RyuJIT

Job=Dry  IterationCount=1  LaunchCount=1
RunStrategy=ColdStart  UnrollFactor=1  WarmupCount=1

```
|                          Method |      Mean | Error | Gen 0 | Gen 1 | Gen 2 | Allocated |
|-------------------------------- |----------:|------:|------:|------:|------:|----------:|
| AspNetJsonSchemaNetBindingValid | 42.822 ms |    NA |     - |     - |     - |     14 KB |
|        AspNetCorvusBindingValid |  7.487 ms |    NA |     - |     - |     - |      1 KB |
