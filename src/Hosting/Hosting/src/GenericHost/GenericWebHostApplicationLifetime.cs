// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.Hosting;
#pragma warning disable CS0618 // Type or member is obsolete
internal sealed class GenericWebHostApplicationLifetime : IApplicationLifetime
#pragma warning restore CS0618 // Type or member is obsolete
{
    private readonly IHostApplicationLifetime _applicationLifetime;
    public GenericWebHostApplicationLifetime(IHostApplicationLifetime applicationLifetime)
    {
        _applicationLifetime = applicationLifetime;
    }

    public CancellationToken ApplicationStarted => _applicationLifetime.ApplicationStarted;

    public CancellationToken ApplicationStopping => _applicationLifetime.ApplicationStopping;

    public CancellationToken ApplicationStopped => _applicationLifetime.ApplicationStopped;

    public void StopApplication() => _applicationLifetime.StopApplication();
}
