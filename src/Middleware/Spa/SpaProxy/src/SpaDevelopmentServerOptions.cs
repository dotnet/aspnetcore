// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.SpaProxy;

internal sealed class SpaDevelopmentServerOptions
{
    public string ServerUrl { get; set; } = "";

    public string? RedirectUrl { get; set; }

    internal string GetRedirectUrl()
    {
        if (!String.IsNullOrEmpty(RedirectUrl))
        {
            return RedirectUrl;
        }

        return ServerUrl;
    }

    public string LaunchCommand { get; set; } = "";

    public int MaxTimeoutInSeconds { get; set; }

    public TimeSpan MaxTimeout => TimeSpan.FromSeconds(MaxTimeoutInSeconds);

    public string WorkingDirectory { get; set; } = "";

    public bool KeepRunning { get; set; }
}
