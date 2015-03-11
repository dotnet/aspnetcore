// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if !DNXCORE50 // [[ISSUE60]] Remove this #ifdef when Core CLR gets support for EncryptedXml

using System;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Xml;
using System.Xml.Linq;
using Microsoft.AspNet.Cryptography;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Internal;
using Microsoft.Framework.Logging;

namespace Microsoft.AspNet.DataProtection.XmlEncryption
{
    /// <summary>
    /// An <see cref="IXmlEncryptor"/> that can perform XML encryption by using an X.509 certificate.
    /// </summary>
    public sealed class CertificateXmlEncryptor : IInternalCertificateXmlEncryptor, IXmlEncryptor
    {
        private readonly Func<X509Certificate2> _certFactory;
        private readonly IInternalCertificateXmlEncryptor _encryptor;
        private readonly ILogger _logger;

        /// <summary>
        /// Creates a <see cref="CertificateXmlEncryptor"/> given a certificate's thumbprint and an
        /// <see cref="ICertificateResolver"/> that can be used to resolve the certificate.
        /// </summary>
        /// <param name="thumbprint">The thumbprint (as a hex string) of the certificate with which to
        /// encrypt the key material. The certificate must be locatable by <paramref name="certificateResolver"/>.</param>
        /// <param name="certificateResolver">A resolver which can locate <see cref="X509Certificate2"/> objects.</param>
        public CertificateXmlEncryptor([NotNull] string thumbprint, [NotNull] ICertificateResolver certificateResolver)
            : this(thumbprint, certificateResolver, services: null)
        {
        }

        /// <summary>
        /// Creates a <see cref="CertificateXmlEncryptor"/> given a certificate's thumbprint, an
        /// <see cref="ICertificateResolver"/> that can be used to resolve the certificate, and
        /// an <see cref="IServiceProvider"/>.
        /// </summary>
        /// <param name="thumbprint">The thumbprint (as a hex string) of the certificate with which to
        /// encrypt the key material. The certificate must be locatable by <paramref name="certificateResolver"/>.</param>
        /// <param name="certificateResolver">A resolver which can locate <see cref="X509Certificate2"/> objects.</param>
        /// <param name="services">An optional <see cref="IServiceProvider"/> to provide ancillary services.</param>
        public CertificateXmlEncryptor([NotNull] string thumbprint, [NotNull] ICertificateResolver certificateResolver, IServiceProvider services)
            : this(services)
        {
            _certFactory = CreateCertFactory(thumbprint, certificateResolver);
        }

        /// <summary>
        /// Creates a <see cref="CertificateXmlEncryptor"/> given an <see cref="X509Certificate2"/> instance.
        /// </summary>
        /// <param name="certificate">The <see cref="X509Certificate2"/> with which to encrypt the key material.</param>
        public CertificateXmlEncryptor([NotNull] X509Certificate2 certificate)
            : this(certificate, services: null)
        {
        }

        /// <summary>
        /// Creates a <see cref="CertificateXmlEncryptor"/> given an <see cref="X509Certificate2"/> instance
        /// and an <see cref="IServiceProvider"/>.
        /// </summary>
        /// <param name="certificate">The <see cref="X509Certificate2"/> with which to encrypt the key material.</param>
        /// <param name="services">An optional <see cref="IServiceProvider"/> to provide ancillary services.</param>
        public CertificateXmlEncryptor([NotNull] X509Certificate2 certificate, IServiceProvider services)
            : this(services)
        {
            _certFactory = () => certificate;
        }

        internal CertificateXmlEncryptor(IServiceProvider services)
        {
            _encryptor = services?.GetService<IInternalCertificateXmlEncryptor>() ?? this;
            _logger = services.GetLogger<CertificateXmlEncryptor>();
        }

        /// <summary>
        /// Encrypts the specified <see cref="XElement"/> with an X.509 certificate.
        /// </summary>
        /// <param name="plaintextElement">The plaintext to encrypt.</param>
        /// <returns>
        /// An <see cref="EncryptedXmlInfo"/> that contains the encrypted value of
        /// <paramref name="plaintextElement"/> along with information about how to
        /// decrypt it.
        /// </returns>
        public EncryptedXmlInfo Encrypt([NotNull] XElement plaintextElement)
        {
            // <EncryptedData Type="http://www.w3.org/2001/04/xmlenc#Element" xmlns="http://www.w3.org/2001/04/xmlenc#">
            //   ...
            // </EncryptedData>

            XElement encryptedElement = EncryptElement(plaintextElement);
            return new EncryptedXmlInfo(encryptedElement, typeof(EncryptedXmlDecryptor));
        }

        private XElement EncryptElement(XElement plaintextElement)
        {
            // EncryptedXml works with XmlDocument, not XLinq. When we perform the conversion
            // we'll wrap the incoming element in a dummy <root /> element since encrypted XML
            // doesn't handle encrypting the root element all that well.
            var xmlDocument = new XmlDocument();
            xmlDocument.Load(new XElement("root", plaintextElement).CreateReader());
            var elementToEncrypt = (XmlElement)xmlDocument.DocumentElement.FirstChild;

            // Perform the encryption and update the document in-place.
            var encryptedXml = new EncryptedXml(xmlDocument);
            var encryptedData = _encryptor.PerformEncryption(encryptedXml, elementToEncrypt);
            EncryptedXml.ReplaceElement(elementToEncrypt, encryptedData, content: false);

            // Strip the <root /> element back off and convert the XmlDocument to an XElement.
            return XElement.Load(xmlDocument.DocumentElement.FirstChild.CreateNavigator().ReadSubtree());
        }

        private Func<X509Certificate2> CreateCertFactory(string thumbprint, ICertificateResolver resolver)
        {
            return () =>
            {
                try
                {
                    var cert = resolver.ResolveCertificate(thumbprint);
                    if (cert == null)
                    {
                        throw Error.CertificateXmlEncryptor_CertificateNotFound(thumbprint);
                    }
                    return cert;
                }
                catch (Exception ex)
                {
                    if (_logger.IsErrorLevelEnabled())
                    {
                        _logger.LogError(ex, "An exception occurred while trying to resolve certificate with thumbprint '{0}'.", thumbprint);
                    }
                    throw;
                }
            };
        }

        EncryptedData IInternalCertificateXmlEncryptor.PerformEncryption(EncryptedXml encryptedXml, XmlElement elementToEncrypt)
        {
            var cert = _certFactory()
                ?? CryptoUtil.Fail<X509Certificate2>("Cert factory returned null.");

            if (_logger.IsVerboseLevelEnabled())
            {
                _logger.LogVerbose("Encrypting to X.509 certificate with thumbprint '{0}'.", cert.Thumbprint);
            }

            try
            {
                return encryptedXml.Encrypt(elementToEncrypt, cert);
            }
            catch (Exception ex)
            {
                if (_logger.IsErrorLevelEnabled())
                {
                    _logger.LogError(ex, "An error occurred while encrypting to X.509 certificate with thumbprint '{0}'.", cert.Thumbprint);
                }
                throw;
            }
        }
    }
}

#endif
