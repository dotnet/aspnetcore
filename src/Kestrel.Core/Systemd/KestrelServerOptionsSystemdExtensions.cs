// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Globalization;
using Microsoft.AspNetCore.Server.Kestrel.Core;

namespace Microsoft.AspNetCore.Hosting
{
    public static class KestrelServerOptionsSystemdExtensions
    {
        // SD_LISTEN_FDS_START https://www.freedesktop.org/software/systemd/man/sd_listen_fds.html
        private const ulong SdListenFdsStart = 3;
        private const string ListenPidEnvVar = "LISTEN_PID";

        /// <summary>
        /// Open file descriptor (SD_LISTEN_FDS_START) initialized by systemd socket-based activation logic if available.
        /// </summary>
        /// <returns>
        /// The <see cref="KestrelServerOptions"/>.
        /// </returns>
        public static KestrelServerOptions UseSystemd(this KestrelServerOptions options)
        {
            return options.UseSystemd(_ => { });
        }

        /// <summary>
        /// Open file descriptor (SD_LISTEN_FDS_START) initialized by systemd socket-based activation logic if available.
        /// Specify callback to configure endpoint-specific settings.
        /// </summary>
        /// <returns>
        /// The <see cref="KestrelServerOptions"/>.
        /// </returns>
        public static KestrelServerOptions UseSystemd(this KestrelServerOptions options, Action<ListenOptions> configure)
        {
            if (string.Equals(Process.GetCurrentProcess().Id.ToString(CultureInfo.InvariantCulture), Environment.GetEnvironmentVariable(ListenPidEnvVar), StringComparison.Ordinal))
            {
                options.ListenHandle(SdListenFdsStart, configure);
            }

            return options;
        }
    }
}
