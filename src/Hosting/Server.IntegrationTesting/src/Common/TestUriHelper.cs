// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;

namespace Microsoft.AspNetCore.Server.IntegrationTesting.Common
{
    public static class TestUriHelper
    {
        public static Uri BuildTestUri(ServerType serverType)
        {
            return BuildTestUri(serverType, hint: null);
        }

        public static Uri BuildTestUri(ServerType serverType, string hint)
        {
            // Assume status messages are enabled for Kestrel and disabled for all other servers.
            var statusMessagesEnabled = (serverType == ServerType.Kestrel);

            return BuildTestUri(serverType, Uri.UriSchemeHttp, hint, statusMessagesEnabled);
        }

        internal static Uri BuildTestUri(ServerType serverType, string scheme, string hint, bool statusMessagesEnabled)
        {
            if (string.IsNullOrEmpty(hint))
            {
                if (serverType == ServerType.Kestrel && statusMessagesEnabled)
                {
                    // Most functional tests use this codepath and should directly bind to dynamic port "0" and scrape
                    // the assigned port from the status message, which should be 100% reliable since the port is bound
                    // once and never released.  Binding to dynamic port "0" on "localhost" (both IPv4 and IPv6) is not
                    // supported, so the port is only bound on "127.0.0.1" (IPv4).  If a test explicitly requires IPv6,
                    // it should provide a hint URL with "localhost" (IPv4 and IPv6) or "[::1]" (IPv6-only).
                    return new UriBuilder(scheme, "127.0.0.1", 0).Uri;
                }
                else if (serverType == ServerType.HttpSys)
                {
                    Debug.Assert(scheme == "http", "Https not supported");
                    return new UriBuilder(scheme, "localhost", 0).Uri;
                }
                else
                {
                    // If the server type is not Kestrel, or status messages are disabled, there is no status message
                    // from which to scrape the assigned port, so the less reliable GetNextPort() must be used.  The
                    // port is bound on "localhost" (both IPv4 and IPv6), since this is supported when using a specific
                    // (non-zero) port.
                    return new UriBuilder(scheme, "localhost", TestPortHelper.GetNextPort()).Uri;
                }
            }
            else
            {
                var uriHint = new Uri(hint);
                if (uriHint.Port == 0)
                {
                    // Only a few tests use this codepath, so it's fine to use the less reliable GetNextPort() for simplicity.
                    // The tests using this codepath will be reviewed to see if they can be changed to directly bind to dynamic
                    // port "0" on "127.0.0.1" and scrape the assigned port from the status message (the default codepath).
                    return new UriBuilder(uriHint) { Port = TestPortHelper.GetNextPort() }.Uri;
                }
                else
                {
                    // If the hint contains a specific port, return it unchanged.
                    return uriHint;
                }
            }
        }
    }
}
