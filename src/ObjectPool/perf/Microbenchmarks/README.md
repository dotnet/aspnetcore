There are two primary types of uses for pools:

* Short Rental Times. This is when code needs objects for a short amount of time
and then returns them to the pool. For example, a pool of StringBuilder instances
typically behaves this way. Often, this means that the process has a whole will
often just have one, or few, instances of the pooled objects in flight at any one
time.

* Long Rental Times. This is when code needs objects for a long amount of time. For
example, a service that needs an object for the time it takes to process an
incoming request. This means that a busy service could have thousands of instances
of the pooled objects in flight at any one time. And if the service receives spiky
traffic, then on a regular basis large number of objects will be added, then removed,
then added again.

We try to capture this duality of use by having two different kinds of benchmarks:

* GetReturn just try to get and return a small number of items.

* DrainRefill empty and fill the pool

We have single-threaded versions of the benchmarks which runs full out. Then we have multi-threaded
versions which start concurrent threads to test how well the pool implementation scales. In the
threaded tests, we inject some busy loops in order to simulate work being done in the application,
to give a more representative view of real world contention on the pool (without this,
the benchmarks create an unnatural amount of contention on the pool which
wouldn't happen in the real world)

Note in the results below, the Default pool is the original implementation
of the default object pool, while Scaled represents an updated version built
around ConcurrentQueue. The checked in benchmark code no longer includes 
test cases for the legacy pool

```
BenchmarkDotNet=v0.13.2, OS=Windows 11 (10.0.22621.819)
Intel Core i7-9700K CPU 3.60GHz (Coffee Lake), 1 CPU, 8 logical and 8 physical cores
.NET SDK=7.0.100
  [Host] : .NET 7.0.0 (7.0.22.51805), X64 RyuJIT AVX2

Job=MediumRun  Toolchain=InProcessEmitToolchain  IterationCount=15
LaunchCount=2  WarmupCount=10

|          Method |    Pool |     Mean |    Error |   StdDev | Allocated |
|---------------- |-------- |---------:|---------:|---------:|----------:|
| GetReturnSingle | Default | 14.85 ns | 2.542 ns | 0.139 ns |         - |
| GetReturnSingle |  Scaled | 13.87 ns | 3.672 ns | 0.201 ns |         - |

|         Method | ThreadCount |    Pool |       Mean |       Error |    StdDev | Allocated |
|--------------- |------------ |-------- |-----------:|------------:|----------:|----------:|
| GetReturnMulti |           1 | Default |   236.8 ns |    14.93 ns |   0.82 ns |         - |
| GetReturnMulti |           1 |  Scaled |   230.5 ns |    31.11 ns |   1.70 ns |         - |
| GetReturnMulti |           2 | Default |   311.6 ns |    83.49 ns |   4.58 ns |         - |
| GetReturnMulti |           2 |  Scaled |   271.8 ns |    74.50 ns |   4.08 ns |         - |
| GetReturnMulti |           4 | Default |   561.3 ns |   523.72 ns |  28.71 ns |         - |
| GetReturnMulti |           4 |  Scaled |   692.5 ns |   167.71 ns |   9.19 ns |         - |
| GetReturnMulti |           8 | Default |   857.2 ns | 1,892.29 ns | 103.72 ns |         - |
| GetReturnMulti |           8 |  Scaled | 1,021.1 ns |   500.68 ns |  27.44 ns |         - |

|            Method | Count |    Pool |            Mean |         Error |       StdDev | Allocated |
|------------------ |------ |-------- |----------------:|--------------:|-------------:|----------:|
| DrainRefillSingle |     8 | Default |        253.3 ns |       9.31 ns |      0.51 ns |         - |
| DrainRefillSingle |     8 |  Scaled |        227.9 ns |       3.57 ns |      0.20 ns |         - |
| DrainRefillSingle |    16 | Default |        901.8 ns |     109.49 ns |      6.00 ns |         - |
| DrainRefillSingle |    16 |  Scaled |        455.1 ns |      45.04 ns |      2.47 ns |         - |
| DrainRefillSingle |    64 | Default |     14,087.7 ns |     709.81 ns |     38.91 ns |         - |
| DrainRefillSingle |    64 |  Scaled |      1,855.6 ns |     240.62 ns |     13.19 ns |         - |
| DrainRefillSingle |   256 | Default |    212,392.1 ns |  15,057.03 ns |    825.33 ns |         - |
| DrainRefillSingle |   256 |  Scaled |      7,490.5 ns |     278.66 ns |     15.27 ns |         - |
| DrainRefillSingle |  1024 | Default |  3,350,706.9 ns | 324,499.03 ns | 17,786.89 ns |       5 B |
| DrainRefillSingle |  1024 |  Scaled |     29,987.0 ns |   1,420.77 ns |     77.88 ns |         - |
| DrainRefillSingle |  2048 | Default | 13,269,695.8 ns | 814,400.01 ns | 44,640.01 ns |      20 B |
| DrainRefillSingle |  2048 |  Scaled |     66,323.3 ns |  11,331.39 ns |    621.11 ns |         - |

|           Method | Count | ThreadCount |    Pool |              Mean |             Error |         StdDev | Allocated |
|----------------- |------ |------------ |-------- |------------------:|------------------:|---------------:|----------:|
| DrainRefillMulti |     8 |           1 | Default |      3,402.040 ns |       600.7019 ns |     32.9265 ns |         - |
| DrainRefillMulti |     8 |           1 |  Scaled |      3,340.664 ns |       603.0542 ns |     33.0554 ns |         - |
| DrainRefillMulti |     8 |           2 | Default |      2,342.273 ns |     1,601.9624 ns |     87.8090 ns |         - |
| DrainRefillMulti |     8 |           2 |  Scaled |      2,243.490 ns |     1,048.4364 ns |     57.4683 ns |         - |
| DrainRefillMulti |     8 |           4 | Default |      1,369.241 ns |       552.3366 ns |     30.2754 ns |         - |
| DrainRefillMulti |     8 |           4 |  Scaled |      1,013.460 ns |     2,311.3719 ns |    126.6941 ns |         - |
| DrainRefillMulti |     8 |           8 | Default |          2.490 ns |         0.0310 ns |      0.0017 ns |         - |
| DrainRefillMulti |     8 |           8 |  Scaled |          2.490 ns |         0.0176 ns |      0.0010 ns |         - |
| DrainRefillMulti |    16 |           1 | Default |      7,696.936 ns |     1,441.3538 ns |     79.0055 ns |         - |
| DrainRefillMulti |    16 |           1 |  Scaled |      6,976.167 ns |       677.5599 ns |     37.1393 ns |         - |
| DrainRefillMulti |    16 |           2 | Default |      4,672.361 ns |        89.7377 ns |      4.9188 ns |         - |
| DrainRefillMulti |    16 |           2 |  Scaled |      4,462.518 ns |       784.9351 ns |     43.0249 ns |         - |
| DrainRefillMulti |    16 |           4 | Default |      3,027.048 ns |       806.3016 ns |     44.1961 ns |         - |
| DrainRefillMulti |    16 |           4 |  Scaled |      2,524.434 ns |     1,692.4096 ns |     92.7667 ns |         - |
| DrainRefillMulti |    16 |           8 | Default |      2,359.546 ns |       139.8285 ns |      7.6645 ns |         - |
| DrainRefillMulti |    16 |           8 |  Scaled |      1,318.973 ns |     2,819.4887 ns |    154.5457 ns |         - |
| DrainRefillMulti |    64 |           1 | Default |     42,156.917 ns |     6,252.2089 ns |    342.7047 ns |         - |
| DrainRefillMulti |    64 |           1 |  Scaled |     34,420.998 ns |     4,824.3074 ns |    264.4366 ns |         - |
| DrainRefillMulti |    64 |           2 | Default |     20,528.296 ns |    10,638.3506 ns |    583.1239 ns |         - |
| DrainRefillMulti |    64 |           2 |  Scaled |     19,258.943 ns |     1,403.3149 ns |     76.9204 ns |         - |
| DrainRefillMulti |    64 |           4 | Default |     10,090.506 ns |     1,897.5224 ns |    104.0096 ns |         - |
| DrainRefillMulti |    64 |           4 |  Scaled |     10,361.626 ns |     1,210.3394 ns |     66.3428 ns |         - |
| DrainRefillMulti |    64 |           8 | Default |      6,266.454 ns |     1,319.7780 ns |     72.3415 ns |         - |
| DrainRefillMulti |    64 |           8 |  Scaled |      5,802.875 ns |     2,609.1042 ns |    143.0138 ns |         - |
| DrainRefillMulti |   256 |           1 | Default |    329,837.142 ns |    28,180.8139 ns |  1,544.6855 ns |         - |
| DrainRefillMulti |   256 |           1 |  Scaled |    118,653.739 ns |     5,552.4422 ns |    304.3481 ns |         - |
| DrainRefillMulti |   256 |           2 | Default |     76,942.582 ns |   156,667.0744 ns |  8,587.4510 ns |         - |
| DrainRefillMulti |   256 |           2 |  Scaled |     76,145.129 ns |    27,993.7132 ns |  1,534.4299 ns |         - |
| DrainRefillMulti |   256 |           4 | Default |     38,844.476 ns |     5,150.0739 ns |    282.2929 ns |         - |
| DrainRefillMulti |   256 |           4 |  Scaled |     42,601.213 ns |    18,958.2889 ns |  1,039.1678 ns |         - |
| DrainRefillMulti |   256 |           8 | Default |     21,718.325 ns |     1,174.3632 ns |     64.3708 ns |         - |
| DrainRefillMulti |   256 |           8 |  Scaled |     24,667.206 ns |    15,298.1144 ns |    838.5413 ns |         - |
| DrainRefillMulti |  1024 |           1 | Default |  3,931,928.906 ns |   502,418.7402 ns | 27,539.2665 ns |       5 B |
| DrainRefillMulti |  1024 |           1 |  Scaled |    475,052.637 ns |    62,820.3506 ns |  3,443.3954 ns |         - |
| DrainRefillMulti |  1024 |           2 | Default |    428,420.475 ns |   949,652.4645 ns | 52,053.6560 ns |         - |
| DrainRefillMulti |  1024 |           2 |  Scaled |    312,404.215 ns |    31,360.8510 ns |  1,718.9941 ns |         - |
| DrainRefillMulti |  1024 |           4 | Default |    153,662.158 ns |    25,638.8645 ns |  1,405.3527 ns |         - |
| DrainRefillMulti |  1024 |           4 |  Scaled |    158,768.604 ns |    68,297.3126 ns |  3,743.6062 ns |         - |
| DrainRefillMulti |  1024 |           8 | Default |     86,047.257 ns |       495.9489 ns |     27.1846 ns |         - |
| DrainRefillMulti |  1024 |           8 |  Scaled |     88,594.971 ns |     8,521.3088 ns |    467.0817 ns |         - |
| DrainRefillMulti |  2048 |           1 | Default | 14,744,884.896 ns |   582,454.6067 ns | 31,926.3024 ns |      10 B |
| DrainRefillMulti |  2048 |           1 |  Scaled |    947,932.682 ns |    59,063.8970 ns |  3,237.4915 ns |       1 B |
| DrainRefillMulti |  2048 |           2 | Default |  1,375,669.466 ns | 1,389,879.8763 ns | 76,184.0060 ns |       1 B |
| DrainRefillMulti |  2048 |           2 |  Scaled |    614,905.078 ns |   207,382.6684 ns | 11,367.3438 ns |       1 B |
| DrainRefillMulti |  2048 |           4 | Default |    738,090.462 ns | 1,812,923.2145 ns | 99,372.4388 ns |       1 B |
| DrainRefillMulti |  2048 |           4 |  Scaled |    321,501.628 ns |    60,639.0375 ns |  3,323.8303 ns |         - |
| DrainRefillMulti |  2048 |           8 | Default |    214,069.694 ns |   116,308.1865 ns |  6,375.2442 ns |         - |
| DrainRefillMulti |  2048 |           8 |  Scaled |    175,667.798 ns |    15,600.4542 ns |    855.1135 ns |         - |
```
