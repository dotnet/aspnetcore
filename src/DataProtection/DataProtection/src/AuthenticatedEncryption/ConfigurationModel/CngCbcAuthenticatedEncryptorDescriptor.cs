// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.Versioning;
using System.Xml.Linq;
using Microsoft.AspNetCore.Shared;

namespace Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;

/// <summary>
/// A descriptor which can create an authenticated encryption system based upon the
/// configuration provided by an <see cref="CngCbcAuthenticatedEncryptorConfiguration"/> object.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class CngCbcAuthenticatedEncryptorDescriptor : IAuthenticatedEncryptorDescriptor
{
    /// <summary>
    /// Initializes a new instance of <see cref="CngCbcAuthenticatedEncryptorDescriptor"/>.
    /// </summary>
    /// <param name="configuration">The <see cref="CngCbcAuthenticatedEncryptorConfiguration"/>.</param>
    /// <param name="masterKey">The master key.</param>
    public CngCbcAuthenticatedEncryptorDescriptor(CngCbcAuthenticatedEncryptorConfiguration configuration, ISecret masterKey)
    {
        ArgumentNullThrowHelper.ThrowIfNull(configuration);
        ArgumentNullThrowHelper.ThrowIfNull(masterKey);

        Configuration = configuration;
        MasterKey = masterKey;
    }

    internal ISecret MasterKey { get; }

    internal CngCbcAuthenticatedEncryptorConfiguration Configuration { get; }

    /// <inheritdoc />
    public XmlSerializedDescriptorInfo ExportToXml()
    {
        // <descriptor>
        //   <!-- Windows CNG-CBC -->
        //   <encryption algorithm="..." keyLength="..." [provider="..."] />
        //   <hash algorithm="..." [provider="..."] />
        //   <masterKey>...</masterKey>
        // </descriptor>

        var encryptionElement = new XElement("encryption",
            new XAttribute("algorithm", Configuration.EncryptionAlgorithm),
            new XAttribute("keyLength", Configuration.EncryptionAlgorithmKeySize));
        if (Configuration.EncryptionAlgorithmProvider != null)
        {
            encryptionElement.SetAttributeValue("provider", Configuration.EncryptionAlgorithmProvider);
        }

        var hashElement = new XElement("hash",
            new XAttribute("algorithm", Configuration.HashAlgorithm));
        if (Configuration.HashAlgorithmProvider != null)
        {
            hashElement.SetAttributeValue("provider", Configuration.HashAlgorithmProvider);
        }

        var rootElement = new XElement("descriptor",
            new XComment(" Algorithms provided by Windows CNG, using CBC-mode encryption with HMAC validation "),
            encryptionElement,
            hashElement,
            MasterKey.ToMasterKeyElement());

        return new XmlSerializedDescriptorInfo(rootElement, typeof(CngCbcAuthenticatedEncryptorDescriptorDeserializer));
    }
}
