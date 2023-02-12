// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Security.Cryptography;
using System.Xml.Linq;
using Microsoft.AspNetCore.Shared;

namespace Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;

/// <summary>
/// A descriptor which can create an authenticated encryption system based upon the
/// configuration provided by an <see cref="ManagedAuthenticatedEncryptorConfiguration"/> object.
/// </summary>
public sealed class ManagedAuthenticatedEncryptorDescriptor : IAuthenticatedEncryptorDescriptor
{
    /// <summary>
    /// Initializes a new instance of <see cref="ManagedAuthenticatedEncryptorDescriptor"/>.
    /// </summary>
    /// <param name="configuration">The <see cref="ManagedAuthenticatedEncryptorConfiguration"/>.</param>
    /// <param name="masterKey">The master key.</param>
    public ManagedAuthenticatedEncryptorDescriptor(ManagedAuthenticatedEncryptorConfiguration configuration, ISecret masterKey)
    {
        ArgumentNullThrowHelper.ThrowIfNull(configuration);
        ArgumentNullThrowHelper.ThrowIfNull(masterKey);

        Configuration = configuration;
        MasterKey = masterKey;
    }

    internal ISecret MasterKey { get; }

    internal ManagedAuthenticatedEncryptorConfiguration Configuration { get; }

    /// <inheritdoc />
    public XmlSerializedDescriptorInfo ExportToXml()
    {
        // <descriptor>
        //   <!-- managed implementations -->
        //   <encryption algorithm="..." keyLength="..." />
        //   <validation algorithm="..." />
        //   <masterKey>...</masterKey>
        // </descriptor>

        var encryptionElement = new XElement("encryption",
            new XAttribute("algorithm", ManagedAlgorithmHelpers.TypeToFriendlyName(Configuration.EncryptionAlgorithmType)),
            new XAttribute("keyLength", Configuration.EncryptionAlgorithmKeySize));

        var validationElement = new XElement("validation",
            new XAttribute("algorithm", ManagedAlgorithmHelpers.TypeToFriendlyName(Configuration.ValidationAlgorithmType)));

        var rootElement = new XElement("descriptor",
            new XComment(" Algorithms provided by specified SymmetricAlgorithm and KeyedHashAlgorithm "),
            encryptionElement,
            validationElement,
            MasterKey.ToMasterKeyElement());

        return new XmlSerializedDescriptorInfo(rootElement, typeof(ManagedAuthenticatedEncryptorDescriptorDeserializer));
    }
}
