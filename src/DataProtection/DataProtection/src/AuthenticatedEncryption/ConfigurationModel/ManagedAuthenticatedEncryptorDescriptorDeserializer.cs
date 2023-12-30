// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Xml.Linq;
using Microsoft.AspNetCore.Shared;

namespace Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;

/// <summary>
/// A class that can deserialize an <see cref="XElement"/> that represents the serialized version
/// of an <see cref="ManagedAuthenticatedEncryptorDescriptor"/>.
/// </summary>
public sealed class ManagedAuthenticatedEncryptorDescriptorDeserializer : IAuthenticatedEncryptorDescriptorDeserializer
{
    /// <summary>
    /// Imports the <see cref="ManagedAuthenticatedEncryptorDescriptor"/> from serialized XML.
    /// </summary>
    public IAuthenticatedEncryptorDescriptor ImportFromXml(XElement element)
    {
        ArgumentNullThrowHelper.ThrowIfNull(element);

        // <descriptor>
        //   <!-- managed implementations -->
        //   <encryption algorithm="..." keyLength="..." />
        //   <validation algorithm="..." />
        //   <masterKey>...</masterKey>
        // </descriptor>

        var configuration = new ManagedAuthenticatedEncryptorConfiguration();

        var encryptionElement = element.Element("encryption")!;
        configuration.EncryptionAlgorithmType = ManagedAlgorithmHelpers.FriendlyNameToType((string)encryptionElement.Attribute("algorithm")!);
        configuration.EncryptionAlgorithmKeySize = (int)encryptionElement.Attribute("keyLength")!;

        var validationElement = element.Element("validation")!;
        configuration.ValidationAlgorithmType = ManagedAlgorithmHelpers.FriendlyNameToType((string)validationElement.Attribute("algorithm")!);

        Secret masterKey = ((string)element.Element("masterKey")!).ToSecret();

        return new ManagedAuthenticatedEncryptorDescriptor(configuration, masterKey);
    }
}
