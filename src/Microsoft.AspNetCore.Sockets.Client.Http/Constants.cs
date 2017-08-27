// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net.Http.Headers;

namespace Microsoft.AspNetCore.Sockets.Client.Http
{
    public class Constants
    {
        private static readonly string UserAgent = "Microsoft.AspNetCore.Sockets.Client.Http/1.0.0-alpha";
        public static readonly ProductInfoHeaderValue UserAgentHeader = ProductInfoHeaderValue.Parse(UserAgent);
    }
}
