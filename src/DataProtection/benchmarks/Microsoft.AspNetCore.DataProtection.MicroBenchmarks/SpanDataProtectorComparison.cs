// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.DataProtection.MicroBenchmarks;

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
    public void ProtectUnprotectRoundtrip()
    {
        var plaintext = GetPlaintext();
        
        // Traditional approach with allocations
        var protectedData = _dataProtector.Protect(plaintext);
        var unprotectedData = _dataProtector.Unprotect(protectedData);
    }

    [Benchmark]
    public void TryProtectTryUnprotectRoundtrip()
    {
        var plaintext = GetPlaintext();
        
        // Span-based approach with minimal allocations
        var protectedSize = _spanDataProtector.GetProtectedSize(plaintext.Length);
        var protectedBuffer = ArrayPool<byte>.Shared.Rent(protectedSize);
        
        try
        {
            var protectSuccess = _spanDataProtector.TryProtect(plaintext, protectedBuffer, out var protectedBytesWritten);
            if (!protectSuccess)
            {
                throw new InvalidOperationException("TryProtect failed");
            }

            var unprotectedSize = _spanDataProtector.GetUnprotectedSize(protectedBytesWritten);
            var unprotectedBuffer = ArrayPool<byte>.Shared.Rent(unprotectedSize);
            
            try
            {
                var unprotectSuccess = _spanDataProtector.TryUnprotect(
                    protectedBuffer.AsSpan(0, protectedBytesWritten),
                    unprotectedBuffer, 
                    out var unprotectedBytesWritten);
                    
                if (!unprotectSuccess)
                {
                    throw new InvalidOperationException("TryUnprotect failed");
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(unprotectedBuffer);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(protectedBuffer);
        }
    }
}
