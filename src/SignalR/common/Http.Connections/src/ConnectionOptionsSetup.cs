// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;

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
