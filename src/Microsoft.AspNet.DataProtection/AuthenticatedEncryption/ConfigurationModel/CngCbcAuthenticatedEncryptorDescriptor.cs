// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Xml.Linq;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.DataProtection.AuthenticatedEncryption.ConfigurationModel
{
    /// <summary>
    /// A descriptor which can create an authenticated encryption system based upon the
    /// configuration provided by an <see cref="CngCbcAuthenticatedEncryptionOptions"/> object.
    /// </summary>
    public sealed class CngCbcAuthenticatedEncryptorDescriptor : IAuthenticatedEncryptorDescriptor
    {
        public CngCbcAuthenticatedEncryptorDescriptor([NotNull] CngCbcAuthenticatedEncryptionOptions options, [NotNull] ISecret masterKey)
        {
            Options = options;
            MasterKey = masterKey;
        }

        internal ISecret MasterKey { get; }

        internal CngCbcAuthenticatedEncryptionOptions Options { get; }

        public IAuthenticatedEncryptor CreateEncryptorInstance()
        {
            return Options.CreateAuthenticatedEncryptorInstance(MasterKey);
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
                new XAttribute("algorithm", Options.EncryptionAlgorithm),
                new XAttribute("keyLength", Options.EncryptionAlgorithmKeySize));
            if (Options.EncryptionAlgorithmProvider != null)
            {
                encryptionElement.SetAttributeValue("provider", Options.EncryptionAlgorithmProvider);
            }

            var hashElement = new XElement("hash",
                new XAttribute("algorithm", Options.HashAlgorithm));
            if (Options.HashAlgorithmProvider != null)
            {
                hashElement.SetAttributeValue("provider", Options.HashAlgorithmProvider);
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
