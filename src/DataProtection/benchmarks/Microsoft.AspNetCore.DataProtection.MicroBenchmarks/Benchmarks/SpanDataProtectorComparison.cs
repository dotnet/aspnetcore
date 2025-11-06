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
|    ByteArray_ProtectUnprotectRoundtrip | DefaultJob |        Default |     Default |               5 | 3.643 us | 0.0397 us | 0.0372 us | 3.637 us | 274,473.3 |      - |     - |     - |     360 B |
| PooledWriter_ProtectUnprotectRoundtrip | DefaultJob |        Default |     Default |               5 | 3.490 us | 0.0265 us | 0.0207 us | 3.490 us | 286,505.4 | 0.0038 |     - |     - |     224 B |
|    RefWriter_ProtectUnprotectRoundtrip | DefaultJob |        Default |     Default |               5 | 3.958 us | 0.1572 us | 0.4585 us | 3.791 us | 252,681.2 |      - |     - |     - |     160 B |
|    ByteArray_ProtectUnprotectRoundtrip | Job-UEQIYD | .NET Core 10.0 |  Throughput |               5 | 3.896 us | 0.0708 us | 0.1163 us | 3.847 us | 256,706.2 |      - |     - |     - |     360 B |
| PooledWriter_ProtectUnprotectRoundtrip | Job-UEQIYD | .NET Core 10.0 |  Throughput |               5 | 3.715 us | 0.0242 us | 0.0189 us | 3.716 us | 269,177.8 |      - |     - |     - |     224 B |
|    RefWriter_ProtectUnprotectRoundtrip | Job-UEQIYD | .NET Core 10.0 |  Throughput |               5 | 3.735 us | 0.0741 us | 0.1153 us | 3.709 us | 267,729.7 |      - |     - |     - |     160 B |
|    ByteArray_ProtectUnprotectRoundtrip | DefaultJob |        Default |     Default |              50 | 4.020 us | 0.0790 us | 0.0700 us | 3.998 us | 248,760.2 | 0.0076 |     - |     - |     456 B |
| PooledWriter_ProtectUnprotectRoundtrip | DefaultJob |        Default |     Default |              50 | 3.750 us | 0.0507 us | 0.0423 us | 3.761 us | 266,700.7 |      - |     - |     - |     224 B |
|    RefWriter_ProtectUnprotectRoundtrip | DefaultJob |        Default |     Default |              50 | 3.856 us | 0.0737 us | 0.1231 us | 3.875 us | 259,344.1 |      - |     - |     - |     160 B |
|    ByteArray_ProtectUnprotectRoundtrip | Job-UEQIYD | .NET Core 10.0 |  Throughput |              50 | 4.347 us | 0.1277 us | 0.3764 us | 4.207 us | 230,042.8 |      - |     - |     - |     456 B |
| PooledWriter_ProtectUnprotectRoundtrip | Job-UEQIYD | .NET Core 10.0 |  Throughput |              50 | 3.938 us | 0.0785 us | 0.1454 us | 3.903 us | 253,935.7 |      - |     - |     - |     224 B |
|    RefWriter_ProtectUnprotectRoundtrip | Job-UEQIYD | .NET Core 10.0 |  Throughput |              50 | 3.898 us | 0.0780 us | 0.2286 us | 3.828 us | 256,567.4 |      - |     - |     - |     160 B |
|    ByteArray_ProtectUnprotectRoundtrip | DefaultJob |        Default |     Default |             100 | 4.088 us | 0.0816 us | 0.2329 us | 4.051 us | 244,610.9 | 0.0076 |     - |     - |     552 B |
| PooledWriter_ProtectUnprotectRoundtrip | DefaultJob |        Default |     Default |             100 | 3.895 us | 0.0779 us | 0.0765 us | 3.877 us | 256,752.7 | 0.0038 |     - |     - |     224 B |
|    RefWriter_ProtectUnprotectRoundtrip | DefaultJob |        Default |     Default |             100 | 4.041 us | 0.0843 us | 0.2377 us | 3.981 us | 247,485.8 |      - |     - |     - |     160 B |
|    ByteArray_ProtectUnprotectRoundtrip | Job-UEQIYD | .NET Core 10.0 |  Throughput |             100 | 4.352 us | 0.0835 us | 0.2001 us | 4.280 us | 229,762.9 |      - |     - |     - |     552 B |
| PooledWriter_ProtectUnprotectRoundtrip | Job-UEQIYD | .NET Core 10.0 |  Throughput |             100 | 3.960 us | 0.0768 us | 0.1051 us | 3.961 us | 252,506.3 |      - |     - |     - |     224 B |
|    RefWriter_ProtectUnprotectRoundtrip | Job-UEQIYD | .NET Core 10.0 |  Throughput |             100 | 3.980 us | 0.0788 us | 0.1227 us | 3.939 us | 251,236.0 |      - |     - |     - |     160 B |

 */

[SimpleJob, MemoryDiagnoser]
public class SpanDataProtectorComparison
{
    private IDataProtector _dataProtector = null!;
    private ISpanDataProtector _spanDataProtector = null!;

    private byte[] _plaintext5 = null!;
    private byte[] _plaintext50 = null!;
    private byte[] _plaintext100 = null!;

    [Params(5, 50, 100)]
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

        _plaintext5 = new byte[5];
        random.NextBytes(_plaintext5);

        _plaintext50 = new byte[50];
        random.NextBytes(_plaintext50);

        _plaintext100 = new byte[100];
        random.NextBytes(_plaintext100);
    }

    private byte[] GetPlaintext()
    {
        return PlaintextLength switch
        {
            5 => _plaintext5,
            50 => _plaintext50,
            100 => _plaintext100,
            _ => throw new ArgumentException("Invalid plaintext length")
        };
    }

    [Benchmark]
    public int ByteArray_ProtectUnprotectRoundtrip()
    {
        var plaintext = GetPlaintext();

        // Traditional approach with allocations
        var protectedData = _dataProtector.Protect(plaintext);
        var unprotectedData = _dataProtector.Unprotect(protectedData);
        return protectedData.Length + unprotectedData.Length;
    }

    [Benchmark]
    public int PooledWriter_ProtectUnprotectRoundtrip()
    {
        var plaintext = GetPlaintext();

        var protectBuffer = new PooledArrayBufferWriter<byte>(initialCapacity: 255);
        var unprotectBuffer = new PooledArrayBufferWriter<byte>(initialCapacity: PlaintextLength);
        try
        {
            _spanDataProtector.Protect(plaintext, ref protectBuffer);
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
    public int RefWriter_ProtectUnprotectRoundtrip()
    {
        var plaintext = GetPlaintext();

        var protectBuffer = new RefPooledArrayBufferWriter(initialCapacity: 255);
        var unprotectBuffer = new RefPooledArrayBufferWriter(initialCapacity: PlaintextLength);
        try
        {
            _spanDataProtector.Protect(plaintext, ref protectBuffer);
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
