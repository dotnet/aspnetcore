// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Http.Core
{
    public class HttpClientCertificateFeature : IHttpClientCertificateFeature
    {
        public HttpClientCertificateFeature()
        {
        }

        public X509Certificate ClientCertificate { get; set; }

        public Task<X509Certificate> GetClientCertificateAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(ClientCertificate);
        }
    }
}