// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Authentication.Certificate
{
    /// <summary>
    /// Context used when a failure occurs.
    /// </summary>
    public class CertificateAuthenticationFailedContext : ResultContext<CertificateAuthenticationOptions>
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="scheme"></param>
        /// <param name="options"></param>
        public CertificateAuthenticationFailedContext(
            HttpContext context,
            AuthenticationScheme scheme,
            CertificateAuthenticationOptions options)
            : base(context, scheme, options)
        {
        }

        /// <summary>
        /// The exception.
        /// </summary>
        public Exception Exception { get; set; }
    }
}
