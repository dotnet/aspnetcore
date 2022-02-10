// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.AspNetCore.Server.Kestrel.Core;

namespace Microsoft.AspNetCore.Hosting;

/// <summary>
/// Extensions for integrating with systemd
/// </summary>
public static class KestrelServerOptionsSystemdExtensions
{
    // SD_LISTEN_FDS_START https://www.freedesktop.org/software/systemd/man/sd_listen_fds.html
    private const int SdListenFdsStart = 3;
    private const string ListenPidEnvVar = "LISTEN_PID";
    private const string ListenFdsEnvVar = "LISTEN_FDS";

    /// <summary>
    /// Open file descriptors (starting from SD_LISTEN_FDS_START) initialized by systemd socket-based activation logic if available.
    /// </summary>
    /// <returns>
    /// The <see cref="KestrelServerOptions"/>.
    /// </returns>
    public static KestrelServerOptions UseSystemd(this KestrelServerOptions options)
    {
        return options.UseSystemd(_ => { });
    }

    /// <summary>
    /// Open file descriptors (starting from SD_LISTEN_FDS_START) initialized by systemd socket-based activation logic if available.
    /// Specify callback to configure endpoint-specific settings.
    /// </summary>
    /// <returns>
    /// The <see cref="KestrelServerOptions"/>.
    /// </returns>
    public static KestrelServerOptions UseSystemd(this KestrelServerOptions options, Action<ListenOptions> configure)
    {
        if (string.Equals(Environment.ProcessId.ToString(CultureInfo.InvariantCulture), Environment.GetEnvironmentVariable(ListenPidEnvVar), StringComparison.Ordinal))
        {
            // This matches sd_listen_fds behavior that requires %LISTEN_FDS% to be present and in range [1;INT_MAX-SD_LISTEN_FDS_START]
            if (int.TryParse(Environment.GetEnvironmentVariable(ListenFdsEnvVar), NumberStyles.None, NumberFormatInfo.InvariantInfo, out var listenFds)
                && listenFds > 0
                && listenFds <= int.MaxValue - SdListenFdsStart)
            {
                for (var handle = SdListenFdsStart; handle < SdListenFdsStart + listenFds; ++handle)
                {
                    options.ListenHandle((ulong)handle, configure);
                }
            }
        }

        return options;
    }
}
