// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Authentication.Certificate
{
    /// <summary>
    /// Context used when certificates are being validated.
    /// </summary>
    public class CertificateValidatedContext : ResultContext<CertificateAuthenticationOptions>
    {
        /// <summary>
        /// Creates a new instance of <see cref="CertificateValidatedContext"/>.
        /// </summary>
        /// <param name="context">The HttpContext the validate context applies too.</param>
        /// <param name="scheme">The scheme used when the Certificate Authentication handler was registered.</param>
        /// <param name="options">The <see cref="CertificateAuthenticationOptions"/>.</param>
        public CertificateValidatedContext(
            HttpContext context,
            AuthenticationScheme scheme,
            CertificateAuthenticationOptions options)
            : base(context, scheme, options)
        {
        }

        /// <summary>
        /// The certificate to validate.
        /// </summary>
        public X509Certificate2 ClientCertificate { get; set; }
    }
}
