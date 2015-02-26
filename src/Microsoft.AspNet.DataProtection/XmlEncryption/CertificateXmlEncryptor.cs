// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Cryptography.X509Certificates;
using System.Xml.Linq;

namespace Microsoft.AspNet.DataProtection.XmlEncryption
{
    /// <summary>
    /// A class that performs XML encryption using an X.509 certificate.
    /// </summary>
    /// <remarks>
    /// This type currently requires Windows 8.1 (Windows Server 2012 R2) or higher.
    /// </remarks>
    public sealed class CertificateXmlEncryptor : IXmlEncryptor
    {
        private readonly DpapiNGXmlEncryptor _dpapiEncryptor;

        public CertificateXmlEncryptor([NotNull] X509Certificate2 cert)
        {
            byte[] certAsBytes = cert.Export(X509ContentType.Cert);
            string protectionDescriptor = "CERTIFICATE=CertBlob:" + Convert.ToBase64String(certAsBytes);
            _dpapiEncryptor = new DpapiNGXmlEncryptor(protectionDescriptor, DpapiNGProtectionDescriptorFlags.None);
        }

        /// <summary>
        /// Encrypts the specified XML element using an X.509 certificate.
        /// </summary>
        /// <param name="plaintextElement">The plaintext XML element to encrypt. This element is unchanged by the method.</param>
        /// <returns>The encrypted form of the XML element.</returns>
        public XElement Encrypt([NotNull] XElement plaintextElement)
        {
            return _dpapiEncryptor.Encrypt(plaintextElement);
        }
    }
}
