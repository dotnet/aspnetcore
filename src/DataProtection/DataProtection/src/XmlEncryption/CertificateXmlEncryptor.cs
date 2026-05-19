// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Xml;
using System.Xml.Linq;
using Microsoft.AspNetCore.Cryptography;
using Microsoft.AspNetCore.Shared;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.DataProtection.XmlEncryption;

/// <summary>
/// An <see cref="IXmlEncryptor"/> that can perform XML encryption by using an X.509 certificate.
/// </summary>
public sealed class CertificateXmlEncryptor : IInternalCertificateXmlEncryptor, IXmlEncryptor
{
    private readonly Func<X509Certificate2> _certFactory;
    private readonly IInternalCertificateXmlEncryptor _encryptor;
    private readonly ILogger _logger;

    /// <summary>
    /// Creates a <see cref="CertificateXmlEncryptor"/> given a certificate's thumbprint, an
    /// <see cref="ICertificateResolver"/> that can be used to resolve the certificate, and
    /// an <see cref="IServiceProvider"/>.
    /// </summary>
    public CertificateXmlEncryptor(string thumbprint, ICertificateResolver certificateResolver, ILoggerFactory loggerFactory)
        : this(loggerFactory, encryptor: null)
    {
        ArgumentNullThrowHelper.ThrowIfNull(thumbprint);
        ArgumentNullThrowHelper.ThrowIfNull(certificateResolver);

        _certFactory = CreateCertFactory(thumbprint, certificateResolver);
    }

    /// <summary>
    /// Creates a <see cref="CertificateXmlEncryptor"/> given an <see cref="X509Certificate2"/> instance
    /// and an <see cref="IServiceProvider"/>.
    /// </summary>
    public CertificateXmlEncryptor(X509Certificate2 certificate, ILoggerFactory loggerFactory)
        : this(loggerFactory, encryptor: null)
    {
        ArgumentNullThrowHelper.ThrowIfNull(certificate);

        _certFactory = () => certificate;
    }

    internal CertificateXmlEncryptor(ILoggerFactory loggerFactory, IInternalCertificateXmlEncryptor? encryptor)
    {
        _encryptor = encryptor ?? this;
        _logger = loggerFactory.CreateLogger<CertificateXmlEncryptor>();
        _certFactory = default!; // Set by calling ctors
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
    public EncryptedXmlInfo Encrypt(XElement plaintextElement)
    {
        ArgumentNullThrowHelper.ThrowIfNull(plaintextElement);

        // <EncryptedData Type="http://www.w3.org/2001/04/xmlenc#Element" xmlns="http://www.w3.org/2001/04/xmlenc#">
        //   ...
        // </EncryptedData>

        var encryptedElement = EncryptElement(plaintextElement);
        return new EncryptedXmlInfo(encryptedElement, typeof(EncryptedXmlDecryptor));
    }

    [UnconditionalSuppressMessage("AOT", "IL2026:RequiresUnreferencedCode",
        Justification = "This usage of EncryptedXml to encrypt an XElement using a X509Certificate2 does not use reflection.")]
    [UnconditionalSuppressMessage("AOT", "IL3050:RequiresDynamicCode",
        Justification = "This usage of EncryptedXml to encrypt an XElement using a X509Certificate2 does not use XSLTs.")]
    private XElement EncryptElement(XElement plaintextElement)
    {
        // EncryptedXml works with XmlDocument, not XLinq. When we perform the conversion
        // we'll wrap the incoming element in a dummy <root /> element since encrypted XML
        // doesn't handle encrypting the root element all that well.
        var xmlDocument = new XmlDocument();
        xmlDocument.Load(new XElement("root", plaintextElement).CreateReader());
        var elementToEncrypt = (XmlElement)xmlDocument.DocumentElement!.FirstChild!;

        // Perform the encryption and update the document in-place.
        var encryptedXml = new EncryptedXml(xmlDocument);
        var encryptedData = _encryptor.PerformEncryption(encryptedXml, elementToEncrypt);
        EncryptedXml.ReplaceElement(elementToEncrypt, encryptedData, content: false);

        // Strip the <root /> element back off and convert the XmlDocument to an XElement.
        return XElement.Load(xmlDocument.DocumentElement.FirstChild!.CreateNavigator()!.ReadSubtree());
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
                _logger.ExceptionWhileTryingToResolveCertificateWithThumbprint(thumbprint, ex);

                throw;
            }
        };
    }

    EncryptedData IInternalCertificateXmlEncryptor.PerformEncryption(EncryptedXml encryptedXml, XmlElement elementToEncrypt)
    {
        var cert = _certFactory()
            ?? CryptoUtil.Fail<X509Certificate2>("Cert factory returned null.");

        _logger.EncryptingToX509CertificateWithThumbprint(cert.Thumbprint);

        try
        {
            return encryptedXml.Encrypt(elementToEncrypt, cert);
        }
        catch (Exception ex)
        {
            _logger.AnErrorOccurredWhileEncryptingToX509CertificateWithThumbprint(cert.Thumbprint, ex);
            throw;
        }
    }
}
