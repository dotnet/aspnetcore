// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Xml.Linq;

namespace Microsoft.AspNet.DataProtection.AuthenticatedEncryption.ConfigurationModel
{
    /// <summary>
    /// A class that can deserialize an <see cref="XElement"/> that represents the serialized version
    /// of an <see cref="CngGcmAuthenticatedEncryptorDescriptor"/>.
    /// </summary>
    public sealed class CngGcmAuthenticatedEncryptorDescriptorDeserializer : IAuthenticatedEncryptorDescriptorDeserializer
    {
        private readonly IServiceProvider _services;

        public CngGcmAuthenticatedEncryptorDescriptorDeserializer()
            : this(services: null)
        {
        }

        public CngGcmAuthenticatedEncryptorDescriptorDeserializer(IServiceProvider services)
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
            //   <!-- Windows CNG-GCM -->
            //   <encryption algorithm="..." keyLength="..." [provider="..."] />
            //   <masterKey>...</masterKey>
            // </descriptor>

            var options = new CngGcmAuthenticatedEncryptionOptions();

            var encryptionElement = element.Element("encryption");
            options.EncryptionAlgorithm = (string)encryptionElement.Attribute("algorithm");
            options.EncryptionAlgorithmKeySize = (int)encryptionElement.Attribute("keyLength");
            options.EncryptionAlgorithmProvider = (string)encryptionElement.Attribute("provider"); // could be null

            Secret masterKey = ((string)element.Element("masterKey")).ToSecret();

            return new CngGcmAuthenticatedEncryptorDescriptor(options, masterKey, _services);
        }
    }
}
