// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel
{
    /// <summary>
    /// A descriptor which can create an authenticated encryption system based upon the
    /// configuration provided by an <see cref="CngGcmAuthenticatedEncryptionSettings"/> object.
    /// </summary>
    public sealed class CngGcmAuthenticatedEncryptorDescriptor : IAuthenticatedEncryptorDescriptor
    {
        private readonly ILogger _log;

        public CngGcmAuthenticatedEncryptorDescriptor(CngGcmAuthenticatedEncryptionSettings settings, ISecret masterKey)
            : this(settings, masterKey, services: null)
        {
        }

        public CngGcmAuthenticatedEncryptorDescriptor(CngGcmAuthenticatedEncryptionSettings settings, ISecret masterKey, IServiceProvider services)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            if (masterKey == null)
            {
                throw new ArgumentNullException(nameof(masterKey));
            }

            Settings = settings;
            MasterKey = masterKey;
            _log = services.GetLogger<CngGcmAuthenticatedEncryptorDescriptor>();
        }

        internal ISecret MasterKey { get; }

        internal CngGcmAuthenticatedEncryptionSettings Settings { get; }

        public IAuthenticatedEncryptor CreateEncryptorInstance()
        {
            return Settings.CreateAuthenticatedEncryptorInstance(MasterKey, _log);
        }

        public XmlSerializedDescriptorInfo ExportToXml()
        {
            // <descriptor>
            //   <!-- Windows CNG-GCM -->
            //   <encryption algorithm="..." keyLength="..." [provider="..."] />
            //   <masterKey>...</masterKey>
            // </descriptor>

            var encryptionElement = new XElement("encryption",
                new XAttribute("algorithm", Settings.EncryptionAlgorithm),
                new XAttribute("keyLength", Settings.EncryptionAlgorithmKeySize));
            if (Settings.EncryptionAlgorithmProvider != null)
            {
                encryptionElement.SetAttributeValue("provider", Settings.EncryptionAlgorithmProvider);
            }

            var rootElement = new XElement("descriptor",
                new XComment(" Algorithms provided by Windows CNG, using Galois/Counter Mode encryption and validation "),
                encryptionElement,
                MasterKey.ToMasterKeyElement());

            return new XmlSerializedDescriptorInfo(rootElement, typeof(CngGcmAuthenticatedEncryptorDescriptorDeserializer));
        }
    }
}
