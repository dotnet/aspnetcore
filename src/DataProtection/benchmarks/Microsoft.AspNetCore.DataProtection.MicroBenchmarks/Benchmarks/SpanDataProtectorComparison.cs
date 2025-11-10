// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.DataProtection.MicroBenchmarks.Benchmarks;

/*

    BenchmarkDotNet=v0.13.0, OS=Windows 10.0.26100
    AMD Ryzen 9 7950X3D, 1 CPU, 32 logical and 16 physical cores
    .NET SDK=10.0.100-rc.1.25420.111
      [Host]     : .NET 10.0.0 (10.0.25.42121), X64 RyuJIT
      DefaultJob : .NET 10.0.0 (10.0.25.42111), X64 RyuJIT
      Job-UEQIYD : .NET 10.0.0 (10.0.25.42111), X64 RyuJIT

    Server=True

|                                 Method |        Job |      Toolchain | RunStrategy | PlaintextLength |     Mean |     Error |    StdDev |   Median |      Op/s |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|--------------------------------------- |----------- |--------------- |------------ |---------------- |---------:|----------:|----------:|---------:|----------:|-------:|------:|------:|----------:|
|    ByteArray_ProtectUnprotectRoundtrip | DefaultJob |        Default |     Default |               5 | 3.802 us | 0.0716 us | 0.0669 us | 3.777 us | 263,047.7 |      - |     - |     - |     360 B |
| PooledWriter_ProtectUnprotectRoundtrip | DefaultJob |        Default |     Default |               5 | 3.510 us | 0.0252 us | 0.0210 us | 3.516 us | 284,935.8 | 0.0038 |     - |     - |     224 B |
|    RefWriter_ProtectUnprotectRoundtrip | DefaultJob |        Default |     Default |               5 | 3.445 us | 0.0247 us | 0.0219 us | 3.455 us | 290,263.1 |      - |     - |     - |     160 B |
|    ByteArray_ProtectUnprotectRoundtrip | Job-RLSKMM | .NET Core 10.0 |  Throughput |               5 | 3.734 us | 0.0221 us | 0.0196 us | 3.727 us | 267,834.2 |      - |     - |     - |     360 B |
| PooledWriter_ProtectUnprotectRoundtrip | Job-RLSKMM | .NET Core 10.0 |  Throughput |               5 | 3.565 us | 0.0216 us | 0.0191 us | 3.556 us | 280,493.6 |      - |     - |     - |     224 B |
|    RefWriter_ProtectUnprotectRoundtrip | Job-RLSKMM | .NET Core 10.0 |  Throughput |               5 | 3.487 us | 0.0191 us | 0.0178 us | 3.484 us | 286,810.2 |      - |     - |     - |     160 B |
|    ByteArray_ProtectUnprotectRoundtrip | DefaultJob |        Default |     Default |              50 | 3.783 us | 0.0069 us | 0.0054 us | 3.784 us | 264,364.6 | 0.0076 |     - |     - |     456 B |
| PooledWriter_ProtectUnprotectRoundtrip | DefaultJob |        Default |     Default |              50 | 3.558 us | 0.0114 us | 0.0101 us | 3.554 us | 281,052.4 | 0.0038 |     - |     - |     224 B |
|    RefWriter_ProtectUnprotectRoundtrip | DefaultJob |        Default |     Default |              50 | 3.517 us | 0.0169 us | 0.0158 us | 3.518 us | 284,303.0 |      - |     - |     - |     160 B |
|    ByteArray_ProtectUnprotectRoundtrip | Job-RLSKMM | .NET Core 10.0 |  Throughput |              50 | 3.853 us | 0.0309 us | 0.0274 us | 3.848 us | 259,547.1 |      - |     - |     - |     456 B |
| PooledWriter_ProtectUnprotectRoundtrip | Job-RLSKMM | .NET Core 10.0 |  Throughput |              50 | 3.715 us | 0.0704 us | 0.1270 us | 3.650 us | 269,174.9 |      - |     - |     - |     224 B |
|    RefWriter_ProtectUnprotectRoundtrip | Job-RLSKMM | .NET Core 10.0 |  Throughput |              50 | 3.560 us | 0.0246 us | 0.0218 us | 3.552 us | 280,872.1 |      - |     - |     - |     160 B |
|    ByteArray_ProtectUnprotectRoundtrip | DefaultJob |        Default |     Default |              80 | 3.823 us | 0.0339 us | 0.0283 us | 3.808 us | 261,554.7 | 0.0076 |     - |     - |     512 B |
| PooledWriter_ProtectUnprotectRoundtrip | DefaultJob |        Default |     Default |              80 | 3.606 us | 0.0286 us | 0.0267 us | 3.597 us | 277,308.2 | 0.0038 |     - |     - |     224 B |
|    RefWriter_ProtectUnprotectRoundtrip | DefaultJob |        Default |     Default |              80 | 3.583 us | 0.0143 us | 0.0120 us | 3.581 us | 279,067.2 |      - |     - |     - |     160 B |
|    ByteArray_ProtectUnprotectRoundtrip | Job-RLSKMM | .NET Core 10.0 |  Throughput |              80 | 3.833 us | 0.0243 us | 0.0215 us | 3.825 us | 260,922.1 |      - |     - |     - |     512 B |
| PooledWriter_ProtectUnprotectRoundtrip | Job-RLSKMM | .NET Core 10.0 |  Throughput |              80 | 3.664 us | 0.0284 us | 0.0221 us | 3.664 us | 272,954.0 |      - |     - |     - |     224 B |
|    RefWriter_ProtectUnprotectRoundtrip | Job-RLSKMM | .NET Core 10.0 |  Throughput |              80 | 3.612 us | 0.0190 us | 0.0178 us | 3.605 us | 276,892.3 |      - |     - |     - |     160 B |
|    ByteArray_ProtectUnprotectRoundtrip | DefaultJob |        Default |     Default |             100 | 3.827 us | 0.0176 us | 0.0147 us | 3.825 us | 261,281.5 | 0.0076 |     - |     - |     552 B |
| PooledWriter_ProtectUnprotectRoundtrip | DefaultJob |        Default |     Default |             100 | 3.687 us | 0.0380 us | 0.0297 us | 3.685 us | 271,208.3 |      - |     - |     - |     224 B |
|    RefWriter_ProtectUnprotectRoundtrip | DefaultJob |        Default |     Default |             100 | 4.503 us | 0.1894 us | 0.5583 us | 4.465 us | 222,075.1 |      - |     - |     - |     160 B |
|    ByteArray_ProtectUnprotectRoundtrip | Job-RLSKMM | .NET Core 10.0 |  Throughput |             100 | 4.758 us | 0.2173 us | 0.6409 us | 4.608 us | 210,150.6 |      - |     - |     - |     552 B |
| PooledWriter_ProtectUnprotectRoundtrip | Job-RLSKMM | .NET Core 10.0 |  Throughput |             100 | 4.282 us | 0.1473 us | 0.4178 us | 4.075 us | 233,544.8 |      - |     - |     - |     224 B |
|    RefWriter_ProtectUnprotectRoundtrip | Job-RLSKMM | .NET Core 10.0 |  Throughput |             100 | 3.941 us | 0.0735 us | 0.1565 us | 3.921 us | 253,712.1 |      - |     - |     - |     160 B |

*/

[SimpleJob, MemoryDiagnoser]
public class SpanDataProtectorComparison
{
    private IDataProtector _dataProtector = null!;
    private ISpanDataProtector _spanDataProtector = null!;

    private byte[] _plaintext = null!;

    [Params(5, 50, 80, 100)]
    public int PlaintextLength { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        // Setup DataProtection as in DI
        var services = new ServiceCollection();
        services.AddDataProtection();
        var serviceProvider = services.BuildServiceProvider();

        _dataProtector = serviceProvider.GetDataProtector("benchmark", "test");
        _spanDataProtector = (ISpanDataProtector)_dataProtector;

        // Setup test data for different lengths
        var random = new Random(42); // Fixed seed for consistent results

        _plaintext = new byte[PlaintextLength];
        random.NextBytes(_plaintext);
    }

    [Benchmark]
    public int ByteArray_ProtectUnprotectRoundtrip()
    {
        // Traditional approach with allocations
        var protectedData = _dataProtector.Protect(_plaintext);
        var unprotectedData = _dataProtector.Unprotect(protectedData);
        return protectedData.Length + unprotectedData.Length;
    }

    [Benchmark]
    public int PooledWriter_ProtectUnprotectRoundtrip()
    {
        var protectBuffer = new PooledArrayBufferWriter<byte>(initialCapacity: 255);
        var unprotectBuffer = new PooledArrayBufferWriter<byte>(initialCapacity: PlaintextLength);
        try
        {
            _spanDataProtector.Protect(_plaintext, ref protectBuffer);
            var protectedSpan = protectBuffer.WrittenSpan;

            _spanDataProtector.Unprotect(protectedSpan, ref unprotectBuffer);
            var unProtectedSpan = protectBuffer.WrittenSpan;

            return protectedSpan.Length + unProtectedSpan.Length;
        }
        finally
        {
            protectBuffer.Dispose();
            unprotectBuffer.Dispose();
        }
    }

    [Benchmark]
    public unsafe int RefWriter_ProtectUnprotectRoundtrip()
    {
        var protectBuffer = new RefPooledArrayBufferWriter<byte>(stackalloc byte[255]);
        var unprotectBuffer = new RefPooledArrayBufferWriter<byte>(stackalloc byte[255]);
        try
        {
            _spanDataProtector.Protect(_plaintext, ref protectBuffer);
            var protectedSpan = protectBuffer.WrittenSpan;

            _spanDataProtector.Unprotect(protectedSpan, ref unprotectBuffer);
            var unProtectedSpan = unprotectBuffer.WrittenSpan;

            return protectedSpan.Length + unProtectedSpan.Length;
        }
        finally
        {
            protectBuffer.Dispose();
            unprotectBuffer.Dispose();
        }
    }
}
