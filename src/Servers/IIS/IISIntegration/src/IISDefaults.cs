// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Server.IISIntegration
{
    /// <summary>
    /// Defaults to configure IIS Out-Of-Process with.
    /// </summary>
    public class IISDefaults
    {
        /// <summary>
        /// Default authentication scheme, which is "Windows".
        /// </summary>
        public const string AuthenticationScheme = "Windows";
        /// <summary>
        /// Default negotiate string, which is "Negotiate".
        /// </summary>
        public const string Negotiate = "Negotiate";
        /// <summary>
        /// Default ntlm string, which is "NTLM".
        /// </summary>
        public const string Ntlm = "NTLM";
    }
}
