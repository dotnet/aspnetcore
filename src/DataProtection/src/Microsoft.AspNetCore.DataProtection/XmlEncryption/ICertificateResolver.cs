// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Security.Cryptography.X509Certificates;

namespace Microsoft.AspNetCore.DataProtection.XmlEncryption
{
    /// <summary>
    /// Provides services for locating <see cref="X509Certificate2"/> instances.
    /// </summary>
    public interface ICertificateResolver
    {
        /// <summary>
        /// Locates an <see cref="X509Certificate2"/> given its thumbprint.
        /// </summary>
        /// <param name="thumbprint">The thumbprint (as a hex string) of the certificate to resolve.</param>
        /// <returns>The resolved <see cref="X509Certificate2"/>, or null if the certificate cannot be found.</returns>
        X509Certificate2 ResolveCertificate(string thumbprint);
    }
}
