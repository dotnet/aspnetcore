// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.AspNetCore.Server.Kestrel.Https
{
    public class HttpsConnectionFilterOptions
    {
        public HttpsConnectionFilterOptions()
        {
            ClientCertificateMode = ClientCertificateMode.NoCertificate;
            SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls11;
        }

        public X509Certificate2 ServerCertificate { get; set; }
        public ClientCertificateMode ClientCertificateMode { get; set; }
        public Func<X509Certificate2, X509Chain, SslPolicyErrors, bool> ClientCertificateValidation { get; set; }
        public SslProtocols SslProtocols { get; set; }
        public bool CheckCertificateRevocation { get; set; }
    }
}
