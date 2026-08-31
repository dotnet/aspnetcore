// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.DirectTls;

/// <summary>
/// Test double for <see cref="IHostApplicationLifetime"/>. Records how many times <see cref="StopApplication"/>
/// was called and cancels <see cref="ApplicationStopping"/> when it is, so tests can assert that a fatal pump
/// error escalated to a host shutdown request.
/// </summary>
internal sealed class TestHostApplicationLifetime : IHostApplicationLifetime
{
    private readonly CancellationTokenSource _stopping = new();

    public int StopApplicationCallCount { get; private set; }

    public CancellationToken ApplicationStarted => CancellationToken.None;

    public CancellationToken ApplicationStopping => _stopping.Token;

    public CancellationToken ApplicationStopped => CancellationToken.None;

    public void StopApplication()
    {
        StopApplicationCallCount++;
        _stopping.Cancel();
    }
}
