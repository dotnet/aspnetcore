// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.DataProtection.XmlEncryption
{
    /// <summary>
    /// An <see cref="IXmlDecryptor"/> that decrypts XML elements by using the <see cref="EncryptedXml"/> class.
    /// </summary>
    public sealed class EncryptedXmlDecryptor : IInternalEncryptedXmlDecryptor, IXmlDecryptor
    {
        private readonly IInternalEncryptedXmlDecryptor _decryptor;
        private readonly XmlKeyDecryptionOptions _options;

        /// <summary>
        /// Creates a new instance of an <see cref="EncryptedXmlDecryptor"/>.
        /// </summary>
        public EncryptedXmlDecryptor()
            : this(services: null)
        {
        }

        /// <summary>
        /// Creates a new instance of an <see cref="EncryptedXmlDecryptor"/>.
        /// </summary>
        /// <param name="services">An optional <see cref="IServiceProvider"/> to provide ancillary services.</param>
        public EncryptedXmlDecryptor(IServiceProvider services)
        {
            _decryptor = services?.GetService<IInternalEncryptedXmlDecryptor>() ?? this;
            _options = services?.GetService<IOptions<XmlKeyDecryptionOptions>>()?.Value;
        }

        /// <summary>
        /// Decrypts the specified XML element.
        /// </summary>
        /// <param name="encryptedElement">An encrypted XML element.</param>
        /// <returns>The decrypted form of <paramref name="encryptedElement"/>.</returns>
        public XElement Decrypt(XElement encryptedElement)
        {
            if (encryptedElement == null)
            {
                throw new ArgumentNullException(nameof(encryptedElement));
            }

            // <EncryptedData Type="http://www.w3.org/2001/04/xmlenc#Element" xmlns="http://www.w3.org/2001/04/xmlenc#">
            //   ...
            // </EncryptedData>

            // EncryptedXml works with XmlDocument, not XLinq. When we perform the conversion
            // we'll wrap the incoming element in a dummy <root /> element since encrypted XML
            // doesn't handle encrypting the root element all that well.
            var xmlDocument = new XmlDocument();
            xmlDocument.Load(new XElement("root", encryptedElement).CreateReader());
            var elementToDecrypt = (XmlElement)xmlDocument.DocumentElement.FirstChild;

            // Perform the decryption and update the document in-place.
            var encryptedXml = new EncryptedXmlWithCertificateKeys(_options, xmlDocument);
            _decryptor.PerformPreDecryptionSetup(encryptedXml);

            encryptedXml.DecryptDocument();

            // Strip the <root /> element back off and convert the XmlDocument to an XElement.
            return XElement.Load(xmlDocument.DocumentElement.FirstChild.CreateNavigator().ReadSubtree());
        }

        void IInternalEncryptedXmlDecryptor.PerformPreDecryptionSetup(EncryptedXml encryptedXml)
        {
            // no-op
        }

        /// <summary>
        /// Can decrypt the XML key data from an <see cref="X509Certificate2"/> that is not in stored in <see cref="X509Store"/>.
        /// </summary>
        private class EncryptedXmlWithCertificateKeys : EncryptedXml
        {
            private readonly XmlKeyDecryptionOptions _options;

            public EncryptedXmlWithCertificateKeys(XmlKeyDecryptionOptions options, XmlDocument document)
                : base(document)
            {
                _options = options;
            }

            public override byte[] DecryptEncryptedKey(EncryptedKey encryptedKey)
            {
                if (_options != null && _options.KeyDecryptionCertificateCount > 0)
                {
                    var keyInfoEnum = encryptedKey.KeyInfo?.GetEnumerator();
                    if (keyInfoEnum == null)
                    {
                        return null;
                    }

                    while (keyInfoEnum.MoveNext())
                    {
                        if (!(keyInfoEnum.Current is KeyInfoX509Data kiX509Data))
                        {
                            continue;
                        }

                        byte[] key = GetKeyFromCert(encryptedKey, kiX509Data);
                        if (key != null)
                        {
                            return key;
                        }
                    }
                }

                return base.DecryptEncryptedKey(encryptedKey);
            }

            private byte[] GetKeyFromCert(EncryptedKey encryptedKey, KeyInfoX509Data keyInfo)
            {
                var certEnum = keyInfo.Certificates?.GetEnumerator();
                if (certEnum == null)
                {
                    return null;
                }

                while (certEnum.MoveNext())
                {
                    if (!(certEnum.Current is X509Certificate2 certInfo))
                    {
                        continue;
                    }

                    if (!_options.TryGetKeyDecryptionCertificates(certInfo, out var keyDecryptionCerts))
                    {
                        continue;
                    }

                    foreach (var keyDecryptionCert in keyDecryptionCerts)
                    {
                        if (!keyDecryptionCert.HasPrivateKey)
                        {
                            continue;
                        }

                        using (RSA privateKey = keyDecryptionCert.GetRSAPrivateKey())
                        {
                            if (privateKey != null)
                            {
                                var useOAEP = encryptedKey.EncryptionMethod?.KeyAlgorithm == XmlEncRSAOAEPUrl;
                                return DecryptKey(encryptedKey.CipherData.CipherValue, privateKey, useOAEP);
                            }
                        }
                    }
                }

                return null;
            }
        }
    }
}
