// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Grpc.JsonTranscoding.IntegrationTests.Infrastructure;

public delegate void LogMessage(LogLevel logLevel, string categoryName, EventId eventId, string message, Exception? exception);

internal class GrpcTestContext<TStartup> : IDisposable where TStartup : class
{
    private readonly Stopwatch _stopwatch;
    private readonly GrpcTestFixture<TStartup> _fixture;
    private readonly ITestOutputHelper _outputHelper;

    public GrpcTestContext(GrpcTestFixture<TStartup> fixture, ITestOutputHelper outputHelper)
    {
        _stopwatch = Stopwatch.StartNew();
        _fixture = fixture;
        _outputHelper = outputHelper;
        _fixture.LoggedMessage += WriteMessage;
    }

    private void WriteMessage(LogLevel logLevel, string category, EventId eventId, string message, Exception? exception)
    {
        _outputHelper.WriteLine($"{_stopwatch.Elapsed.TotalSeconds:N3}s {category} - {logLevel}: {message}");
    }

    public void Dispose()
    {
        _fixture.LoggedMessage -= WriteMessage;
    }
}
