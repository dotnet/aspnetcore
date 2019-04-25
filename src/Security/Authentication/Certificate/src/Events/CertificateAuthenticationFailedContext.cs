// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Authentication.Certificate
{
    public class CertificateAuthenticationFailedContext : ResultContext<CertificateAuthenticationOptions>
    {
        public CertificateAuthenticationFailedContext(
            HttpContext context,
            AuthenticationScheme scheme,
            CertificateAuthenticationOptions options)
            : base(context, scheme, options)
        {
        }

        public Exception Exception { get; set; }
    }
}
