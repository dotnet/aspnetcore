// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Hosting
{
    /// <summary>
    /// Commonly used environment names.
    /// </summary>
    [System.Obsolete("This type is obsolete and will be removed in a future version. The recommended alternative is Microsoft.Extensions.Hosting.Environments.", error: false)]
    public static class EnvironmentName
    {
        public static readonly string Development = "Development";
        public static readonly string Staging = "Staging";
        public static readonly string Production = "Production";
    }
}
