// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Xml.Linq;

namespace Microsoft.AspNet.DataProtection.AuthenticatedEncryption.ConfigurationModel
{
    /// <summary>
    /// A descriptor which can create an authenticated encryption system based upon the
    /// configuration provided by an <see cref="AuthenticatedEncryptionOptions"/> object.
    /// </summary>
    public sealed class AuthenticatedEncryptorDescriptor : IAuthenticatedEncryptorDescriptor
    {
        private readonly IServiceProvider _services;

        public AuthenticatedEncryptorDescriptor(AuthenticatedEncryptionOptions options, ISecret masterKey)
            : this(options, masterKey, services: null)
        {
        }

        public AuthenticatedEncryptorDescriptor(AuthenticatedEncryptionOptions options, ISecret masterKey, IServiceProvider services)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (masterKey == null)
            {
                throw new ArgumentNullException(nameof(masterKey));
            }

            Options = options;
            MasterKey = masterKey;
            _services = services;
        }

        internal ISecret MasterKey { get; }

        internal AuthenticatedEncryptionOptions Options { get; }

        public IAuthenticatedEncryptor CreateEncryptorInstance()
        {
            return Options.CreateAuthenticatedEncryptorInstance(MasterKey, _services);
        }

        public XmlSerializedDescriptorInfo ExportToXml()
        {
            // <descriptor>
            //   <encryption algorithm="..." />
            //   <validation algorithm="..." /> <!-- only if not GCM -->
            //   <masterKey requiresEncryption="true">...</masterKey>
            // </descriptor>

            var encryptionElement = new XElement("encryption",
                new XAttribute("algorithm", Options.EncryptionAlgorithm));

            var validationElement = (AuthenticatedEncryptionOptions.IsGcmAlgorithm(Options.EncryptionAlgorithm))
                ? (object)new XComment(" AES-GCM includes a 128-bit authentication tag, no extra validation algorithm required. ")
                : (object)new XElement("validation",
                    new XAttribute("algorithm", Options.ValidationAlgorithm));

            var outerElement = new XElement("descriptor",
                encryptionElement,
                validationElement,
                MasterKey.ToMasterKeyElement());

            return new XmlSerializedDescriptorInfo(outerElement, typeof(AuthenticatedEncryptorDescriptorDeserializer));
        }
    }
}
