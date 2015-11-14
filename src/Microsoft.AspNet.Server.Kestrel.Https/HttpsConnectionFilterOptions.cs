// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Security.Cryptography.X509Certificates;

namespace Microsoft.AspNet.Server.Kestrel.Https
{
    public class HttpsConnectionFilterOptions
    {
        public HttpsConnectionFilterOptions()
        {
            ClientCertificateMode = ClientCertificateMode.NoCertificate;
        }

        public X509Certificate2 ServerCertificate { get; set; }
        public ClientCertificateMode ClientCertificateMode { get; set; }
        public ClientCertificateValidationCallback ClientCertificateValidation { get; set; }
    }
}
