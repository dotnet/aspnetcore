// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if !DOTNET5_4 // [[ISSUE60]] Remove this #ifdef when Core CLR gets support for EncryptedXml

using System;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.AspNetCore.DataProtection.XmlEncryption
{
    /// <summary>
    /// A default implementation of <see cref="ICertificateResolver"/> that looks in the current user
    /// and local machine certificate stores.
    /// </summary>
    public class CertificateResolver : ICertificateResolver
    {
        /// <summary>
        /// Locates an <see cref="X509Certificate2"/> given its thumbprint.
        /// </summary>
        /// <param name="thumbprint">The thumbprint (as a hex string) of the certificate to resolve.</param>
        /// <returns>The resolved <see cref="X509Certificate2"/>, or null if the certificate cannot be found.</returns>
        public virtual X509Certificate2 ResolveCertificate(string thumbprint)
        {
            if (thumbprint == null)
            {
                throw new ArgumentNullException(nameof(thumbprint));
            }

            if (String.IsNullOrEmpty(thumbprint))
            {
                throw Error.Common_ArgumentCannotBeNullOrEmpty(nameof(thumbprint));
            }

            return GetCertificateFromStore(StoreLocation.CurrentUser, thumbprint)
                ?? GetCertificateFromStore(StoreLocation.LocalMachine, thumbprint);
        }

        private static X509Certificate2 GetCertificateFromStore(StoreLocation location, string thumbprint)
        {
            var store = new X509Store(location);
            try
            {
                store.Open(OpenFlags.ReadOnly);
                var matchingCerts = store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, validOnly: true);
                return (matchingCerts != null && matchingCerts.Count > 0) ? matchingCerts[0] : null;
            }
            finally
            {
                store.Close();
            }
        }
    }
}

#endif
