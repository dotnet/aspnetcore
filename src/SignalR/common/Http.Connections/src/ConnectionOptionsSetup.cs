// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Http.Connections
{
    public class ConnectionOptionsSetup : IConfigureOptions<ConnectionOptions>
    {
        public static TimeSpan DefaultDisconectTimeout = TimeSpan.FromSeconds(15);

        public void Configure(ConnectionOptions options)
        {
            if (options.DisconnectTimeout == null)
            {
                options.DisconnectTimeout = DefaultDisconectTimeout;
            }
        }
    }
}
