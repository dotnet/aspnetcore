// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Xml.Linq;

namespace Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel
{
    /// <summary>
    /// A class that can deserialize an <see cref="XElement"/> that represents the serialized version
    /// of an <see cref="CngCbcAuthenticatedEncryptorDescriptor"/>.
    /// </summary>
    public sealed class CngCbcAuthenticatedEncryptorDescriptorDeserializer : IAuthenticatedEncryptorDescriptorDeserializer
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
            //   <!-- Windows CNG-CBC -->
            //   <encryption algorithm="..." keyLength="..." [provider="..."] />
            //   <hash algorithm="..." [provider="..."] />
            //   <masterKey>...</masterKey>
            // </descriptor>

            var configuration = new CngCbcAuthenticatedEncryptorConfiguration();

            var encryptionElement = element.Element("encryption");
            configuration.EncryptionAlgorithm = (string)encryptionElement.Attribute("algorithm");
            configuration.EncryptionAlgorithmKeySize = (int)encryptionElement.Attribute("keyLength");
            configuration.EncryptionAlgorithmProvider = (string)encryptionElement.Attribute("provider"); // could be null

            var hashElement = element.Element("hash");
            configuration.HashAlgorithm = (string)hashElement.Attribute("algorithm");
            configuration.HashAlgorithmProvider = (string)hashElement.Attribute("provider"); // could be null

            Secret masterKey = ((string)element.Element("masterKey")).ToSecret();

            return new CngCbcAuthenticatedEncryptorDescriptor(configuration, masterKey);
        }
    }
}
