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

### Numbers before improving the code flow (only span<byte> API related changes)
|                                 Method |        Job |      Toolchain | RunStrategy | PlaintextLength |     Mean |     Error |    StdDev |   Median |      Op/s |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|--------------------------------------- |----------- |--------------- |------------ |---------------- |---------:|----------:|----------:|---------:|----------:|-------:|------:|------:|----------:|
|    ByteArray_ProtectUnprotectRoundtrip | DefaultJob |        Default |     Default |               5 | 4.097 us | 0.0598 us | 0.0530 us | 4.079 us | 244,090.2 |      - |     - |     - |     360 B |
| PooledWriter_ProtectUnprotectRoundtrip | DefaultJob |        Default |     Default |               5 | 3.886 us | 0.0211 us | 0.0176 us | 3.880 us | 257,339.0 |      - |     - |     - |     224 B |
|    RefWriter_ProtectUnprotectRoundtrip | DefaultJob |        Default |     Default |               5 | 3.844 us | 0.0507 us | 0.0423 us | 3.823 us | 260,122.2 |      - |     - |     - |     160 B |
|    ByteArray_ProtectUnprotectRoundtrip | Job-REJWKR | .NET Core 10.0 |  Throughput |               5 | 4.178 us | 0.0825 us | 0.1356 us | 4.134 us | 239,344.6 |      - |     - |     - |     360 B |
| PooledWriter_ProtectUnprotectRoundtrip | Job-REJWKR | .NET Core 10.0 |  Throughput |               5 | 3.902 us | 0.0258 us | 0.0202 us | 3.896 us | 256,296.9 |      - |     - |     - |     224 B |
|    RefWriter_ProtectUnprotectRoundtrip | Job-REJWKR | .NET Core 10.0 |  Throughput |               5 | 3.943 us | 0.0635 us | 0.0731 us | 3.909 us | 253,595.7 |      - |     - |     - |     160 B |
|    ByteArray_ProtectUnprotectRoundtrip | DefaultJob |        Default |     Default |              50 | 4.230 us | 0.0809 us | 0.0831 us | 4.190 us | 236,415.8 | 0.0076 |     - |     - |     456 B |
| PooledWriter_ProtectUnprotectRoundtrip | DefaultJob |        Default |     Default |              50 | 4.019 us | 0.0734 us | 0.0816 us | 3.991 us | 248,798.9 |      - |     - |     - |     224 B |
|    RefWriter_ProtectUnprotectRoundtrip | DefaultJob |        Default |     Default |              50 | 4.036 us | 0.0802 us | 0.1778 us | 3.971 us | 247,794.0 |      - |     - |     - |     160 B |
|    ByteArray_ProtectUnprotectRoundtrip | Job-REJWKR | .NET Core 10.0 |  Throughput |              50 | 4.244 us | 0.0839 us | 0.0744 us | 4.208 us | 235,623.3 |      - |     - |     - |     456 B |
| PooledWriter_ProtectUnprotectRoundtrip | Job-REJWKR | .NET Core 10.0 |  Throughput |              50 | 4.000 us | 0.0889 us | 0.2579 us | 4.005 us | 249,994.1 |      - |     - |     - |     224 B |
|    RefWriter_ProtectUnprotectRoundtrip | Job-REJWKR | .NET Core 10.0 |  Throughput |              50 | 3.679 us | 0.0692 us | 0.0740 us | 3.654 us | 271,839.1 |      - |     - |     - |     160 B |
|    ByteArray_ProtectUnprotectRoundtrip | DefaultJob |        Default |     Default |              80 | 3.862 us | 0.0741 us | 0.0728 us | 3.857 us | 258,957.3 | 0.0076 |     - |     - |     512 B |
| PooledWriter_ProtectUnprotectRoundtrip | DefaultJob |        Default |     Default |              80 | 3.725 us | 0.0743 us | 0.1242 us | 3.677 us | 268,484.2 |      - |     - |     - |     224 B |
|    RefWriter_ProtectUnprotectRoundtrip | DefaultJob |        Default |     Default |              80 | 3.727 us | 0.0745 us | 0.1539 us | 3.665 us | 268,320.4 |      - |     - |     - |     160 B |
|    ByteArray_ProtectUnprotectRoundtrip | Job-REJWKR | .NET Core 10.0 |  Throughput |              80 | 4.039 us | 0.0805 us | 0.2078 us | 3.939 us | 247,555.9 |      - |     - |     - |     512 B |
| PooledWriter_ProtectUnprotectRoundtrip | Job-REJWKR | .NET Core 10.0 |  Throughput |              80 | 3.809 us | 0.0751 us | 0.0835 us | 3.783 us | 262,504.4 |      - |     - |     - |     224 B |
|    RefWriter_ProtectUnprotectRoundtrip | Job-REJWKR | .NET Core 10.0 |  Throughput |              80 | 3.718 us | 0.0711 us | 0.0665 us | 3.717 us | 268,936.9 |      - |     - |     - |     160 B |
|    ByteArray_ProtectUnprotectRoundtrip | DefaultJob |        Default |     Default |             100 | 4.086 us | 0.0796 us | 0.2054 us | 4.011 us | 244,726.7 | 0.0076 |     - |     - |     552 B |
| PooledWriter_ProtectUnprotectRoundtrip | DefaultJob |        Default |     Default |             100 | 3.922 us | 0.0773 us | 0.1613 us | 3.877 us | 254,955.2 | 0.0038 |     - |     - |     224 B |
|    RefWriter_ProtectUnprotectRoundtrip | DefaultJob |        Default |     Default |             100 | 3.658 us | 0.0521 us | 0.0435 us | 3.658 us | 273,341.9 |      - |     - |     - |     160 B |
|    ByteArray_ProtectUnprotectRoundtrip | Job-REJWKR | .NET Core 10.0 |  Throughput |             100 | 4.088 us | 0.0805 us | 0.1018 us | 4.046 us | 244,641.5 |      - |     - |     - |     552 B |
| PooledWriter_ProtectUnprotectRoundtrip | Job-REJWKR | .NET Core 10.0 |  Throughput |             100 | 3.800 us | 0.0351 us | 0.0293 us | 3.805 us | 263,132.8 |      - |     - |     - |     224 B |
|    RefWriter_ProtectUnprotectRoundtrip | Job-REJWKR | .NET Core 10.0 |  Throughput |             100 | 3.797 us | 0.0735 us | 0.1031 us | 3.752 us | 263,344.2 |      - |     - |     - |     160 B |

### 

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
