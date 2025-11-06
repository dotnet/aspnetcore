// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.DataProtection.MicroBenchmarks.Benchmarks;

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
    public int ProtectUnprotectRoundtrip()
    {
        var plaintext = GetPlaintext();

        // Traditional approach with allocations
        var protectedData = _dataProtector.Protect(plaintext);
        var unprotectedData = _dataProtector.Unprotect(protectedData);
        return protectedData.Length + unprotectedData.Length;
    }

    [Benchmark]
    public int TryProtectTryUnprotectRoundtrip()
    {
        var plaintext = GetPlaintext();

        var protectBuffer = new RefPooledArrayBufferWriter(initialCapacity: 255);
        _spanDataProtector.Protect(plaintext, protectBuffer);
        var protectedSpan = protectBuffer.WrittenSpan;

        var unprotectBuffer = new RefPooledArrayBufferWriter(initialCapacity: PlaintextLength);
        _spanDataProtector.Unprotect(protectedSpan, unprotectBuffer);
        var unProtectedSpan = unprotectBuffer.WrittenSpan;

        return protectedSpan.Length + unProtectedSpan.Length;
    }
}
