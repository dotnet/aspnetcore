// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Xml.Linq;
using Microsoft.AspNetCore.Cryptography;
using Microsoft.AspNetCore.DataProtection.Cng;
using Microsoft.AspNetCore.Shared;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.DataProtection.XmlEncryption;

/// <summary>
/// An <see cref="IXmlDecryptor"/> that decrypts XML elements that were encrypted with <see cref="DpapiXmlEncryptor"/>.
/// </summary>
public sealed class DpapiXmlDecryptor : IXmlDecryptor
{
    private readonly ILogger _logger;

    /// <summary>
    /// Creates a new instance of a <see cref="DpapiXmlDecryptor"/>.
    /// </summary>
    public DpapiXmlDecryptor()
        : this(services: null)
    {
    }

    /// <summary>
    /// Creates a new instance of a <see cref="DpapiXmlDecryptor"/>.
    /// </summary>
    /// <param name="services">An optional <see cref="IServiceProvider"/> to provide ancillary services.</param>
    public DpapiXmlDecryptor(IServiceProvider? services)
    {
        CryptoUtil.AssertPlatformIsWindows();

        _logger = services.GetLogger<DpapiXmlDecryptor>();
    }

    /// <summary>
    /// Decrypts the specified XML element.
    /// </summary>
    /// <param name="encryptedElement">An encrypted XML element.</param>
    /// <returns>The decrypted form of <paramref name="encryptedElement"/>.</returns>
    public XElement Decrypt(XElement encryptedElement)
    {
        ArgumentNullThrowHelper.ThrowIfNull(encryptedElement);

        _logger.DecryptingSecretElementUsingWindowsDPAPI();

        try
        {
            // <encryptedKey>
            //   <!-- This key is encrypted with {provider}. -->
            //   <value>{base64}</value>
            // </encryptedKey>

            var protectedSecret = Convert.FromBase64String((string)encryptedElement.Element("value")!);
            using (var secret = DpapiSecretSerializerHelper.UnprotectWithDpapi(protectedSecret))
            {
                return secret.ToXElement();
            }
        }
        catch (Exception ex)
        {
            // It's OK for us to log the error, as we control the exception, and it doesn't contain
            // sensitive information.
            _logger.ExceptionOccurredTryingToDecryptElement(ex);
            throw;
        }
    }
}
