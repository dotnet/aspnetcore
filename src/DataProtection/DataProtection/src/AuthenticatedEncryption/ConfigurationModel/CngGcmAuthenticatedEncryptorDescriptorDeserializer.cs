// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.Versioning;
using System.Xml.Linq;

namespace Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;

/// <summary>
/// A class that can deserialize an <see cref="XElement"/> that represents the serialized version
/// of an <see cref="CngGcmAuthenticatedEncryptorDescriptor"/>.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class CngGcmAuthenticatedEncryptorDescriptorDeserializer : IAuthenticatedEncryptorDescriptorDeserializer
{
    /// <summary>
    /// Imports the <see cref="CngCbcAuthenticatedEncryptorDescriptor"/> from serialized XML.
    /// </summary>
    public IAuthenticatedEncryptorDescriptor ImportFromXml(XElement element)
    {
        if (element == null)
        {
            throw new ArgumentNullException(nameof(element));
        }

        // <descriptor>
        //   <!-- Windows CNG-GCM -->
        //   <encryption algorithm="..." keyLength="..." [provider="..."] />
        //   <masterKey>...</masterKey>
        // </descriptor>

        var configuration = new CngGcmAuthenticatedEncryptorConfiguration();

        var encryptionElement = element.Element("encryption")!;
        configuration.EncryptionAlgorithm = (string)encryptionElement.Attribute("algorithm")!;
        configuration.EncryptionAlgorithmKeySize = (int)encryptionElement.Attribute("keyLength")!;
        configuration.EncryptionAlgorithmProvider = (string?)encryptionElement.Attribute("provider"); // could be null

        Secret masterKey = ((string)element.Element("masterKey")!).ToSecret();

        return new CngGcmAuthenticatedEncryptorDescriptor(configuration, masterKey);
    }
}
