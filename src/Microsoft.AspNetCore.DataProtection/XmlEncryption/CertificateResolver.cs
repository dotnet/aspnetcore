// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Cryptography;
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
                store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);
                var matchingCerts = store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, validOnly: true);
                return (matchingCerts != null && matchingCerts.Count > 0)
                    ? matchingCerts[0]
                    : null;
            }
            catch (CryptographicException)
            {
                // Suppress first-chance exceptions when opening the store.
                // For example, LocalMachine\My is not supported on Linux yet and will throw on Open(),
                // but there isn't a good way to detect this without attempting to open the store.
                // See https://github.com/dotnet/corefx/issues/3690.
                return null;
            }
            finally
            {
                store.Close();
            }
        }
    }
}

