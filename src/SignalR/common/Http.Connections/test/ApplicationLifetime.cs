// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.Http.Connections.Tests;

public class TestApplicationLifetime : IHostApplicationLifetime
{
    private readonly CancellationTokenSource _startedSource = new CancellationTokenSource();
    private readonly CancellationTokenSource _stoppingSource = new CancellationTokenSource();
    private readonly CancellationTokenSource _stoppedSource = new CancellationTokenSource();

    public CancellationToken ApplicationStarted => _startedSource.Token;

    public CancellationToken ApplicationStopping => _stoppingSource.Token;

    public CancellationToken ApplicationStopped => _stoppedSource.Token;

    public void StopApplication()
    {
        _stoppingSource.Cancel(throwOnFirstException: false);
    }

    public void Start()
    {
        _startedSource.Cancel(throwOnFirstException: false);
    }
}

public class EmptyApplicationLifetime : IHostApplicationLifetime
{
    public CancellationToken ApplicationStarted => CancellationToken.None;

    public CancellationToken ApplicationStopping => CancellationToken.None;

    public CancellationToken ApplicationStopped => CancellationToken.None;

    public void StopApplication()
    {
    }
}
