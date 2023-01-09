// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using System.Runtime.Versioning;
using System.Security.Principal;
using System.Xml.Linq;
using Microsoft.AspNetCore.Cryptography;
using Microsoft.AspNetCore.Cryptography.SafeHandles;
using Microsoft.AspNetCore.DataProtection.Cng;
using Microsoft.AspNetCore.Shared;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.DataProtection.XmlEncryption;

/// <summary>
/// A class that can encrypt XML elements using Windows DPAPI:NG.
/// </summary>
/// <remarks>
/// This API is only supported on Windows 8 / Windows Server 2012 and higher.
/// </remarks>
[SupportedOSPlatform("windows")]
public sealed class DpapiNGXmlEncryptor : IXmlEncryptor
{
    private readonly ILogger _logger;
    private readonly NCryptDescriptorHandle _protectionDescriptorHandle;

    /// <summary>
    /// Creates a new instance of a <see cref="DpapiNGXmlEncryptor"/>.
    /// </summary>
    /// <param name="protectionDescriptorRule">The rule string from which to create the protection descriptor.</param>
    /// <param name="flags">Flags controlling the creation of the protection descriptor.</param>
    /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
    public DpapiNGXmlEncryptor(string protectionDescriptorRule, DpapiNGProtectionDescriptorFlags flags, ILoggerFactory loggerFactory)
    {
        ArgumentNullThrowHelper.ThrowIfNull(protectionDescriptorRule);

        CryptoUtil.AssertPlatformIsWindows8OrLater();

        var ntstatus = UnsafeNativeMethods.NCryptCreateProtectionDescriptor(protectionDescriptorRule, (uint)flags, out _protectionDescriptorHandle);
        UnsafeNativeMethods.ThrowExceptionForNCryptStatus(ntstatus);
        CryptoUtil.AssertSafeHandleIsValid(_protectionDescriptorHandle);

        _logger = loggerFactory.CreateLogger<DpapiNGXmlEncryptor>();
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

        var protectionDescriptorRuleString = _protectionDescriptorHandle.GetProtectionDescriptorRuleString();
        _logger.EncryptingToWindowsDPAPINGUsingProtectionDescriptorRule(protectionDescriptorRuleString);

        // Convert the XML element to a binary secret so that it can be run through DPAPI
        byte[] cngDpapiEncryptedData;
        try
        {
            using (var plaintextElementAsSecret = plaintextElement.ToSecret())
            {
                cngDpapiEncryptedData = DpapiSecretSerializerHelper.ProtectWithDpapiNG(plaintextElementAsSecret, _protectionDescriptorHandle);
            }
        }
        catch (Exception ex)
        {
            _logger.ErrorOccurredWhileEncryptingToWindowsDPAPING(ex);
            throw;
        }

        // <encryptedKey>
        //   <!-- This key is encrypted with {provider}. -->
        //   <!-- rule string -->
        //   <value>{base64}</value>
        // </encryptedKey>

        var element = new XElement("encryptedKey",
            new XComment(" This key is encrypted with Windows DPAPI-NG. "),
            new XComment(" Rule: " + protectionDescriptorRuleString + " "),
            new XElement("value",
                Convert.ToBase64String(cngDpapiEncryptedData)));

        return new EncryptedXmlInfo(element, typeof(DpapiNGXmlDecryptor));
    }

    /// <summary>
    /// Creates a rule string tied to the current Windows user and which is transferrable
    /// across machines (backed up in AD).
    /// </summary>
    internal static string GetDefaultProtectionDescriptorString()
    {
        CryptoUtil.AssertPlatformIsWindows8OrLater();

        // Creates a SID=... protection descriptor string for the current user.
        // Reminder: DPAPI:NG provides only encryption, not authentication.
        using (var currentIdentity = WindowsIdentity.GetCurrent())
        {
            // use the SID to create an SDDL string
            return string.Format(CultureInfo.InvariantCulture, "SID={0}", currentIdentity?.User?.Value);
        }
    }
}
