// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.AspNetCore.DataProtection.XmlEncryption
{
    /// <summary>
    /// Specifies settings for how to decrypt XML keys.
    /// </summary>
    internal class XmlKeyDecryptionOptions
    {
        private readonly Dictionary<string, X509Certificate2> _certs = new Dictionary<string, X509Certificate2>(StringComparer.Ordinal);

        /// <summary>
        /// A mapping of key thumbprint to the X509Certificate2
        /// </summary>
        public IReadOnlyDictionary<string, X509Certificate2> KeyDecryptionCertificates => _certs;

        public void AddKeyDecryptionCertificate(X509Certificate2 certificate)
        {
            _certs[certificate.Thumbprint] = certificate;
        }
    }
}
