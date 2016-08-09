// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Xml.Linq;

namespace Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel
{
    /// <summary>
    /// A descriptor which can create an authenticated encryption system based upon the
    /// configuration provided by an <see cref="AuthenticatedEncryptorConfiguration"/> object.
    /// </summary>
    public sealed class AuthenticatedEncryptorDescriptor : IAuthenticatedEncryptorDescriptor
    {
        public AuthenticatedEncryptorDescriptor(AuthenticatedEncryptorConfiguration configuration, ISecret masterKey)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            if (masterKey == null)
            {
                throw new ArgumentNullException(nameof(masterKey));
            }

            Configuration = configuration;
            MasterKey = masterKey;
        }

        internal ISecret MasterKey { get; }

        internal AuthenticatedEncryptorConfiguration Configuration { get; }

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
}
