// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Cryptography;
using System.Xml.Linq;

namespace Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel
{
    /// <summary>
    /// A class that can deserialize an <see cref="XElement"/> that represents the serialized version
    /// of an <see cref="ManagedAuthenticatedEncryptorDescriptor"/>.
    /// </summary>
    public sealed class ManagedAuthenticatedEncryptorDescriptorDeserializer : IAuthenticatedEncryptorDescriptorDeserializer
    {
        private readonly IServiceProvider _services;

        public ManagedAuthenticatedEncryptorDescriptorDeserializer()
            : this(services: null)
        {
        }

        public ManagedAuthenticatedEncryptorDescriptorDeserializer(IServiceProvider services)
        {
            _services = services;
        }

        /// <summary>
        /// Imports the <see cref="ManagedAuthenticatedEncryptorDescriptor"/> from serialized XML.
        /// </summary>
        public IAuthenticatedEncryptorDescriptor ImportFromXml(XElement element)
        {
            if (element == null)
            {
                throw new ArgumentNullException(nameof(element));
            }

            // <descriptor>
            //   <!-- managed implementations -->
            //   <encryption algorithm="..." keyLength="..." />
            //   <validation algorithm="..." />
            //   <masterKey>...</masterKey>
            // </descriptor>

            var settings = new ManagedAuthenticatedEncryptionSettings();

            var encryptionElement = element.Element("encryption");
            settings.EncryptionAlgorithmType = FriendlyNameToType((string)encryptionElement.Attribute("algorithm"));
            settings.EncryptionAlgorithmKeySize = (int)encryptionElement.Attribute("keyLength");

            var validationElement = element.Element("validation");
            settings.ValidationAlgorithmType = FriendlyNameToType((string)validationElement.Attribute("algorithm"));

            Secret masterKey = ((string)element.Element("masterKey")).ToSecret();

            return new ManagedAuthenticatedEncryptorDescriptor(settings, masterKey, _services);
        }

        // Any changes to this method should also be be reflected
        // in ManagedAuthenticatedEncryptorDescriptor.TypeToFriendlyName.
        private static Type FriendlyNameToType(string typeName)
        {
            if (typeName == nameof(Aes))
            {
                return typeof(Aes);
            }
            else if (typeName == nameof(HMACSHA1))
            {
                return typeof(HMACSHA1);
            }
            else if (typeName == nameof(HMACSHA256))
            {
                return typeof(HMACSHA256);
            }
            else if (typeName == nameof(HMACSHA384))
            {
                return typeof(HMACSHA384);
            }
            else if (typeName == nameof(HMACSHA512))
            {
                return typeof(HMACSHA512);
            }
            else
            {
                return Type.GetType(typeName, throwOnError: true);
            }
        }
    }
}
