// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.Versioning;
using System.Xml.Linq;
using Microsoft.AspNetCore.Shared;

namespace Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;

/// <summary>
/// A descriptor which can create an authenticated encryption system based upon the
/// configuration provided by an <see cref="CngGcmAuthenticatedEncryptorConfiguration"/> object.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class CngGcmAuthenticatedEncryptorDescriptor : IAuthenticatedEncryptorDescriptor
{
    /// <summary>
    /// Initializes a new instance of <see cref="CngGcmAuthenticatedEncryptorDescriptor"/>.
    /// </summary>
    /// <param name="configuration">The <see cref="CngCbcAuthenticatedEncryptorConfiguration"/>.</param>
    /// <param name="masterKey">The master key.</param>
    public CngGcmAuthenticatedEncryptorDescriptor(CngGcmAuthenticatedEncryptorConfiguration configuration, ISecret masterKey)
    {
        ArgumentNullThrowHelper.ThrowIfNull(configuration);
        ArgumentNullThrowHelper.ThrowIfNull(masterKey);

        Configuration = configuration;
        MasterKey = masterKey;
    }

    internal ISecret MasterKey { get; }

    internal CngGcmAuthenticatedEncryptorConfiguration Configuration { get; }

    /// <inheritdoc />
    public XmlSerializedDescriptorInfo ExportToXml()
    {
        // <descriptor>
        //   <!-- Windows CNG-GCM -->
        //   <encryption algorithm="..." keyLength="..." [provider="..."] />
        //   <masterKey>...</masterKey>
        // </descriptor>

        var encryptionElement = new XElement("encryption",
            new XAttribute("algorithm", Configuration.EncryptionAlgorithm),
            new XAttribute("keyLength", Configuration.EncryptionAlgorithmKeySize));
        if (Configuration.EncryptionAlgorithmProvider != null)
        {
            encryptionElement.SetAttributeValue("provider", Configuration.EncryptionAlgorithmProvider);
        }

        var rootElement = new XElement("descriptor",
            new XComment(" Algorithms provided by Windows CNG, using Galois/Counter Mode encryption and validation "),
            encryptionElement,
            MasterKey.ToMasterKeyElement());

        return new XmlSerializedDescriptorInfo(rootElement, typeof(CngGcmAuthenticatedEncryptorDescriptorDeserializer));
    }
}
