// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Antiforgery.Benchmarks.Benchmarks;
   
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
