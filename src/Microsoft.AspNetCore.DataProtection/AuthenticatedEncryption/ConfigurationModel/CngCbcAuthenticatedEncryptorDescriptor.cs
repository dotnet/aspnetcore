// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel
{
    /// <summary>
    /// A descriptor which can create an authenticated encryption system based upon the
    /// configuration provided by an <see cref="CngCbcAuthenticatedEncryptionSettings"/> object.
    /// </summary>
    public sealed class CngCbcAuthenticatedEncryptorDescriptor : IAuthenticatedEncryptorDescriptor
    {
        private readonly ILogger _log;

        public CngCbcAuthenticatedEncryptorDescriptor(CngCbcAuthenticatedEncryptionSettings settings, ISecret masterKey)
            : this(settings, masterKey, services: null)
        {
        }

        public CngCbcAuthenticatedEncryptorDescriptor(CngCbcAuthenticatedEncryptionSettings settings, ISecret masterKey, IServiceProvider services)
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
            _log = services.GetLogger<CngCbcAuthenticatedEncryptorDescriptor>();
        }

        internal ISecret MasterKey { get; }

        internal CngCbcAuthenticatedEncryptionSettings Settings { get; }

        public IAuthenticatedEncryptor CreateEncryptorInstance()
        {
            return Settings.CreateAuthenticatedEncryptorInstance(MasterKey, _log);
        }

        public XmlSerializedDescriptorInfo ExportToXml()
        {
            // <descriptor>
            //   <!-- Windows CNG-CBC -->
            //   <encryption algorithm="..." keyLength="..." [provider="..."] />
            //   <hash algorithm="..." [provider="..."] />
            //   <masterKey>...</masterKey>
            // </descriptor>

            var encryptionElement = new XElement("encryption",
                new XAttribute("algorithm", Settings.EncryptionAlgorithm),
                new XAttribute("keyLength", Settings.EncryptionAlgorithmKeySize));
            if (Settings.EncryptionAlgorithmProvider != null)
            {
                encryptionElement.SetAttributeValue("provider", Settings.EncryptionAlgorithmProvider);
            }

            var hashElement = new XElement("hash",
                new XAttribute("algorithm", Settings.HashAlgorithm));
            if (Settings.HashAlgorithmProvider != null)
            {
                hashElement.SetAttributeValue("provider", Settings.HashAlgorithmProvider);
            }

            var rootElement = new XElement("descriptor",
                new XComment(" Algorithms provided by Windows CNG, using CBC-mode encryption with HMAC validation "),
                encryptionElement,
                hashElement,
                MasterKey.ToMasterKeyElement());

            return new XmlSerializedDescriptorInfo(rootElement, typeof(CngCbcAuthenticatedEncryptorDescriptorDeserializer));
        }
    }
}
