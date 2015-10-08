// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Xml.Linq;

namespace Microsoft.AspNet.DataProtection.AuthenticatedEncryption.ConfigurationModel
{
    /// <summary>
    /// A class that can deserialize an <see cref="XElement"/> that represents the serialized version
    /// of an <see cref="AuthenticatedEncryptorDescriptor"/>.
    /// </summary>
    public sealed class AuthenticatedEncryptorDescriptorDeserializer : IAuthenticatedEncryptorDescriptorDeserializer
    {
        private readonly IServiceProvider _services;

        public AuthenticatedEncryptorDescriptorDeserializer()
            : this(services: null)
        {
        }

        public AuthenticatedEncryptorDescriptorDeserializer(IServiceProvider services)
        {
            _services = services;
        }

        /// <summary>
        /// Imports the <see cref="AuthenticatedEncryptorDescriptor"/> from serialized XML.
        /// </summary>
        public IAuthenticatedEncryptorDescriptor ImportFromXml(XElement element)
        {
            if (element == null)
            {
                throw new ArgumentNullException(nameof(element));
            }

            // <descriptor>
            //   <encryption algorithm="..." />
            //   <validation algorithm="..." /> <!-- only if not GCM -->
            //   <masterKey requiresEncryption="true">...</masterKey>
            // </descriptor>

            var options = new AuthenticatedEncryptionOptions();

            var encryptionElement = element.Element("encryption");
            options.EncryptionAlgorithm = (EncryptionAlgorithm)Enum.Parse(typeof(EncryptionAlgorithm), (string)encryptionElement.Attribute("algorithm"));

            // only read <validation> if not GCM
            if (!AuthenticatedEncryptionOptions.IsGcmAlgorithm(options.EncryptionAlgorithm))
            {
                var validationElement = element.Element("validation");
                options.ValidationAlgorithm = (ValidationAlgorithm)Enum.Parse(typeof(ValidationAlgorithm), (string)validationElement.Attribute("algorithm"));
            }

            Secret masterKey = ((string)element.Elements("masterKey").Single()).ToSecret();
            return new AuthenticatedEncryptorDescriptor(options, masterKey, _services);
        }
    }
}
