// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Xml.Linq;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.DataProtection.AuthenticatedEncryption.ConfigurationModel
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
        public IAuthenticatedEncryptorDescriptor ImportFromXml([NotNull] XElement element)
        {
            // <descriptor>
            //   <!-- Windows CNG-CBC -->
            //   <encryption algorithm="..." keyLength="..." [provider="..."] />
            //   <hash algorithm="..." [provider="..."] />
            //   <masterKey>...</masterKey>
            // </descriptor>
            
            var options = new CngCbcAuthenticatedEncryptionOptions();

            var encryptionElement = element.Element("encryption");
            options.EncryptionAlgorithm = (string)encryptionElement.Attribute("algorithm");
            options.EncryptionAlgorithmKeySize = (int)encryptionElement.Attribute("keyLength");
            options.EncryptionAlgorithmProvider = (string)encryptionElement.Attribute("provider"); // could be null

            var hashElement = element.Element("hash");
            options.HashAlgorithm = (string)hashElement.Attribute("algorithm");
            options.HashAlgorithmProvider = (string)hashElement.Attribute("provider"); // could be null

            Secret masterKey = ((string)element.Element("masterKey")).ToSecret();

            return new CngCbcAuthenticatedEncryptorDescriptor(options, masterKey);
        }
    }
}
