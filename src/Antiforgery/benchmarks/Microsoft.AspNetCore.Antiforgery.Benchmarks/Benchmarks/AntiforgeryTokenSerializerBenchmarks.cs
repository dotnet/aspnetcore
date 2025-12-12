// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Antiforgery.Benchmarks.Benchmarks;

/*
    main branch:
    |      Method |     Mean |     Error |    StdDev |      Op/s |  Gen 0 | Gen 1 | Gen 2 | Allocated |
    |------------ |---------:|----------:|----------:|----------:|-------:|------:|------:|----------:|
    |   Serialize | 2.221 us | 0.0245 us | 0.0229 us | 450,239.5 | 0.0076 |     - |     - |     872 B |
    | Deserialize | 2.436 us | 0.0463 us | 0.1036 us | 410,492.5 | 0.0076 |     - |     - |     632 B |

    This PR:
    |      Method |     Mean |     Error |    StdDev |      Op/s |  Gen 0 | Gen 1 | Gen 2 | Allocated |
    |------------ |---------:|----------:|----------:| ----------:|-------:|------:|------:|----------:|
    |   Serialize | 1.932 us | 0.0110 us | 0.0098 us | 517,688.9 | 0.0038 |     - |     - |     544 B |
    | Deserialize | 1.927 us | 0.0107 us | 0.0100 us | 519,058.2 | 0.0038 |     - |     - |     344 B |
 */
    
[AspNetCoreBenchmark]
public class AntiforgeryTokenSerializerBenchmarks
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    private IAntiforgeryTokenSerializer _tokenSerializer;

    private AntiforgeryToken _token;
    private string _serializedToken;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

    [GlobalSetup]
    public void Setup()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddAntiforgery();
        var serviceProvider = serviceCollection.BuildServiceProvider();
        _tokenSerializer = serviceProvider.GetRequiredService<IAntiforgeryTokenSerializer>();

        _token = new AntiforgeryToken()
        {
            IsCookieToken = false,
            Username = "user@test.com",
            ClaimUid = new BinaryBlob(AntiforgeryToken.ClaimUidBitLength),
            AdditionalData = "additional-data-here"
        };

        _serializedToken = _tokenSerializer.Serialize(_token);
    }

    [Benchmark]
    public string Serialize()
    {
        return _tokenSerializer.Serialize(_token);
    }

    [Benchmark]
    public object Deserialize()
    {
        return _tokenSerializer.Deserialize(_serializedToken);
    }
}
