// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Framework.Runtime;

namespace Microsoft.AspNet.HttpFeature
{
    [AssemblyNeutral]
    public interface IHttpClientCertificateFeature
    {
        /// <summary>
        /// Synchronously retrieves the client certificate, if any.
        /// </summary>
        X509Certificate ClientCertificate { get; set; }

        /// <summary>
        /// Asynchronously retrieves the client certificate, if any.
        /// </summary>
        /// <returns></returns>
        Task<X509Certificate> GetClientCertificateAsync(CancellationToken cancellationToken);
    }
}
