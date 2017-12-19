// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Http.Internal
{
    internal static class Constants
    {
        internal const string Http = "http";
        internal const string Https = "https";
        internal const string UnixPipeHostPrefix = "unix:/";

        internal static class BuilderProperties
        {
            internal static string ServerFeatures = "server.Features";
            internal static string ApplicationServices = "application.Services";
        }
    }
}
