// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.AspNetCore.Authentication.Certificate
{
    public static class CertificateValidator
    {
        /// <summary>
        /// Disables connection based client certificate validation so the middleware can handle it instead.
        /// </summary>
        /// <param name="certificate">The certificate used to authenticate the remote party.</param>
        /// <param name="chain">The chain of certificate authorities associated with the remote certificate.</param>
        /// <param name="errors">One or more errors associated with the remote certificate.</param>
        /// <returns>A Boolean value that determines whether the specified certificate is accepted for authentication.</returns>
        public static bool DisableChannelValidation(X509Certificate2 certificate, X509Chain chain, SslPolicyErrors errors)
        {
            return true;
        }
    }
}
