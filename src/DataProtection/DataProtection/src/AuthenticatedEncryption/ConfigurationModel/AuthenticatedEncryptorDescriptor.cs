// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Xml.Linq;
using Microsoft.AspNetCore.Shared;

namespace Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;

/// <summary>
/// A descriptor which can create an authenticated encryption system based upon the
/// configuration provided by an <see cref="AuthenticatedEncryptorConfiguration"/> object.
/// </summary>
public sealed class AuthenticatedEncryptorDescriptor : IAuthenticatedEncryptorDescriptor
{
    /// <summary>
    /// Initializes a new instance of <see cref="AuthenticatedEncryptorDescriptor"/>.
    /// </summary>
    /// <param name="configuration">The <see cref="AuthenticatedEncryptorDescriptor"/>.</param>
    /// <param name="masterKey">The master key.</param>
    public AuthenticatedEncryptorDescriptor(AuthenticatedEncryptorConfiguration configuration, ISecret masterKey)
    {
        ArgumentNullThrowHelper.ThrowIfNull(configuration);
        ArgumentNullThrowHelper.ThrowIfNull(masterKey);

        Configuration = configuration;
        MasterKey = masterKey;
    }

    internal ISecret MasterKey { get; }

    internal AuthenticatedEncryptorConfiguration Configuration { get; }

    /// <inheritdoc/>
    public XmlSerializedDescriptorInfo ExportToXml()
    {
        // <descriptor>
        //   <encryption algorithm="..." />
        //   <validation algorithm="..." /> <!-- only if not GCM -->
        //   <masterKey requiresEncryption="true">...</masterKey>
        // </descriptor>

        var encryptionElement = new XElement("encryption",
            new XAttribute("algorithm", Configuration.EncryptionAlgorithm));

        var validationElement = (AuthenticatedEncryptorFactory.IsGcmAlgorithm(Configuration.EncryptionAlgorithm))
            ? (object)new XComment(" AES-GCM includes a 128-bit authentication tag, no extra validation algorithm required. ")
            : (object)new XElement("validation",
                new XAttribute("algorithm", Configuration.ValidationAlgorithm));

        var outerElement = new XElement("descriptor",
            encryptionElement,
            validationElement,
            MasterKey.ToMasterKeyElement());

        return new XmlSerializedDescriptorInfo(outerElement, typeof(AuthenticatedEncryptorDescriptorDeserializer));
    }
}
