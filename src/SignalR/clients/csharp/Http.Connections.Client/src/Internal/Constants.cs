// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics;
using System.Linq;
using System.Net.Http.Headers;
using System.Reflection;

namespace Microsoft.AspNetCore.Http.Connections.Client.Internal
{
    internal static class Constants
    {
        public static readonly ProductInfoHeaderValue UserAgentHeader;

        static Constants()
        {
            var userAgent = "Microsoft.AspNetCore.Http.Connections.Client";

            var assemblyVersion = typeof(Constants)
                .Assembly
                .GetCustomAttributes<AssemblyInformationalVersionAttribute>()
                .FirstOrDefault();

            Debug.Assert(assemblyVersion != null);

            // assembly version attribute should always be present
            // but in case it isn't then don't include version in user-agent
            if (assemblyVersion != null)
            {
                userAgent += "/" + assemblyVersion.InformationalVersion;
            }

            UserAgentHeader = ProductInfoHeaderValue.Parse(userAgent);
        }
    }
}
