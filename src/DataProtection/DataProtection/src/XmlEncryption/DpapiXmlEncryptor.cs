// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.Versioning;
using System.Security.Principal;
using System.Xml.Linq;
using Microsoft.AspNetCore.Cryptography;
using Microsoft.AspNetCore.DataProtection.Cng;
using Microsoft.AspNetCore.Shared;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.DataProtection.XmlEncryption;

/// <summary>
/// An <see cref="IXmlEncryptor"/> that encrypts XML by using Windows DPAPI.
/// </summary>
/// <remarks>
/// This API is only supported on Windows platforms.
/// </remarks>
[SupportedOSPlatform("windows")]
public sealed class DpapiXmlEncryptor : IXmlEncryptor
{
    private readonly ILogger _logger;
    private readonly bool _protectToLocalMachine;

    /// <summary>
    /// Creates a <see cref="DpapiXmlEncryptor"/> given a protection scope and an <see cref="IServiceProvider"/>.
    /// </summary>
    /// <param name="protectToLocalMachine">'true' if the data should be decipherable by anybody on the local machine,
    /// 'false' if the data should only be decipherable by the current Windows user account.</param>
    /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
    public DpapiXmlEncryptor(bool protectToLocalMachine, ILoggerFactory loggerFactory)
    {
        CryptoUtil.AssertPlatformIsWindows();

        _protectToLocalMachine = protectToLocalMachine;
        _logger = loggerFactory.CreateLogger<DpapiXmlEncryptor>();
    }

    /// <summary>
    /// Encrypts the specified <see cref="XElement"/>.
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
        if (_protectToLocalMachine)
        {
            _logger.EncryptingToWindowsDPAPIForLocalMachineAccount();
        }
        else
        {
            _logger.EncryptingToWindowsDPAPIForCurrentUserAccount(WindowsIdentity.GetCurrent().Name);
        }

        // Convert the XML element to a binary secret so that it can be run through DPAPI
        byte[] dpapiEncryptedData;
        try
        {
            using (var plaintextElementAsSecret = plaintextElement.ToSecret())
            {
                dpapiEncryptedData = DpapiSecretSerializerHelper.ProtectWithDpapi(plaintextElementAsSecret, protectToLocalMachine: _protectToLocalMachine);
            }
        }
        catch (Exception ex)
        {
            _logger.ErrorOccurredWhileEncryptingToWindowsDPAPI(ex);
            throw;
        }

        // <encryptedKey>
        //   <!-- This key is encrypted with {provider}. -->
        //   <value>{base64}</value>
        // </encryptedKey>

        var element = new XElement("encryptedKey",
            new XComment(" This key is encrypted with Windows DPAPI. "),
            new XElement("value",
                Convert.ToBase64String(dpapiEncryptedData)));

        return new EncryptedXmlInfo(element, typeof(DpapiXmlDecryptor));
    }
}
