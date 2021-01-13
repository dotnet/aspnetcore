// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Cors
{
    internal static partial class Resources
    {
        internal static System.Globalization.CultureInfo Culture { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        internal static string InsecureConfiguration { get { throw null; } }
        internal static string PreflightMaxAgeOutOfRange { get { throw null; } }
        internal static System.Resources.ResourceManager ResourceManager { get { throw null; } }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]internal static string GetResourceString(string resourceKey, string defaultValue = null) { throw null; }
    }
}

namespace Microsoft.AspNetCore.Cors.Infrastructure
{
    public partial class CorsPolicyBuilder
    {
        internal static string GetNormalizedOrigin(string origin) { throw null; }
    }
    internal static partial class CorsPolicyExtensions
    {
        public static bool IsOriginAnAllowedSubdomain(this Microsoft.AspNetCore.Cors.Infrastructure.CorsPolicy policy, string origin) { throw null; }
    }
    internal static partial class UriHelpers
    {
        public static bool IsSubdomainOf(System.Uri subdomain, System.Uri domain) { throw null; }
    }
}
