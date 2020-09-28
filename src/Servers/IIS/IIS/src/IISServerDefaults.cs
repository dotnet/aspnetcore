// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Server.IIS
{
    /// <summary>
    /// Defaults to configure IIS In-Process with.
    /// </summary>
    public class IISServerDefaults
    {
        /// <summary>
        /// Default authentication scheme, which is "Windows".
        /// </summary>
        public const string AuthenticationScheme = "Windows";
    }
}
