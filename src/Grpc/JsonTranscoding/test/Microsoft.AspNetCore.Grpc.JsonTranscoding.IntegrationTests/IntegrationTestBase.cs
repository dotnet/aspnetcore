// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using IntegrationTestsWebsite;
using Microsoft.AspNetCore.Grpc.JsonTranscoding.IntegrationTests.Infrastructure;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Grpc.JsonTranscoding.IntegrationTests;

public class IntegrationTestBase : IClassFixture<GrpcTestFixture<Startup>>, IDisposable
{
    private HttpClient? _channel;
    private IDisposable? _testContext;

    protected GrpcTestFixture<Startup> Fixture { get; set; }

    protected ILoggerFactory LoggerFactory => Fixture.LoggerFactory;

    protected HttpClient Channel => _channel ??= CreateChannel();

    protected HttpClient CreateChannel()
    {
        return new HttpClient(Fixture.Handler);
    }

    public IntegrationTestBase(GrpcTestFixture<Startup> fixture, ITestOutputHelper outputHelper)
    {
        Fixture = fixture;
        _testContext = Fixture.GetTestContext(outputHelper);
    }

    public void Dispose()
    {
        _testContext?.Dispose();
        _channel = null;
    }
}

