// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Xml.Linq;

namespace Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel
{
    /// <summary>
    /// A descriptor which can create an authenticated encryption system based upon the
    /// configuration provided by an <see cref="CngCbcAuthenticatedEncryptorConfiguration"/> object.
    /// </summary>
    public sealed class CngCbcAuthenticatedEncryptorDescriptor : IAuthenticatedEncryptorDescriptor
    {
        public CngCbcAuthenticatedEncryptorDescriptor(CngCbcAuthenticatedEncryptorConfiguration configuration, ISecret masterKey)
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

        internal CngCbcAuthenticatedEncryptorConfiguration Configuration { get; }

        public XmlSerializedDescriptorInfo ExportToXml()
        {
            // <descriptor>
            //   <!-- Windows CNG-CBC -->
            //   <encryption algorithm="..." keyLength="..." [provider="..."] />
            //   <hash algorithm="..." [provider="..."] />
            //   <masterKey>...</masterKey>
            // </descriptor>

            var encryptionElement = new XElement("encryption",
                new XAttribute("algorithm", Configuration.EncryptionAlgorithm),
                new XAttribute("keyLength", Configuration.EncryptionAlgorithmKeySize));
            if (Configuration.EncryptionAlgorithmProvider != null)
            {
                encryptionElement.SetAttributeValue("provider", Configuration.EncryptionAlgorithmProvider);
            }

            var hashElement = new XElement("hash",
                new XAttribute("algorithm", Configuration.HashAlgorithm));
            if (Configuration.HashAlgorithmProvider != null)
            {
                hashElement.SetAttributeValue("provider", Configuration.HashAlgorithmProvider);
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
