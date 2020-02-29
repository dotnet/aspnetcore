// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Server.IntegrationTesting
{
    public static class Tfm
    {
        public const string Net461 = "net461";
        public const string NetCoreApp20 = "netcoreapp2.0";
        public const string NetCoreApp21 = "netcoreapp2.1";
        public const string NetCoreApp22 = "netcoreapp2.2";
        public const string NetCoreApp30 = "netcoreapp3.0";
        public const string NetCoreApp31 = "netcoreapp3.1";
        public const string NetCoreApp50 = "netcoreapp5.0";

        public static bool Matches(string tfm1, string tfm2)
        {
            return string.Equals(tfm1, tfm2, StringComparison.OrdinalIgnoreCase);
        }
    }
}
