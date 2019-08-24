// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Authentication.Certificate
{
    /// <summary>
    /// Default values related to certificate authentication middleware
    /// </summary>
    public static class CertificateAuthenticationDefaults
    {
        /// <summary>
        /// The default value used for CertificateAuthenticationOptions.AuthenticationScheme
        /// </summary>
        public const string AuthenticationScheme = "Certificate";
    }
}
