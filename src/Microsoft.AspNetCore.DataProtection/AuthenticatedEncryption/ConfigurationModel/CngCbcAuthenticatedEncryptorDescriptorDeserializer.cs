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
        private readonly IServiceProvider _services;

        public CngCbcAuthenticatedEncryptorDescriptorDeserializer()
            : this(services: null)
        {
        }

        public CngCbcAuthenticatedEncryptorDescriptorDeserializer(IServiceProvider services)
        {
            _services = services;
        }

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

            var settings = new CngCbcAuthenticatedEncryptionSettings();

            var encryptionElement = element.Element("encryption");
            settings.EncryptionAlgorithm = (string)encryptionElement.Attribute("algorithm");
            settings.EncryptionAlgorithmKeySize = (int)encryptionElement.Attribute("keyLength");
            settings.EncryptionAlgorithmProvider = (string)encryptionElement.Attribute("provider"); // could be null

            var hashElement = element.Element("hash");
            settings.HashAlgorithm = (string)hashElement.Attribute("algorithm");
            settings.HashAlgorithmProvider = (string)hashElement.Attribute("provider"); // could be null

            Secret masterKey = ((string)element.Element("masterKey")).ToSecret();

            return new CngCbcAuthenticatedEncryptorDescriptor(settings, masterKey, _services);
        }
    }
}
