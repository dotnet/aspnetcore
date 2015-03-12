// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Xml.Linq;
using Microsoft.Framework.Internal;
using Microsoft.Framework.Logging;

namespace Microsoft.AspNet.DataProtection.AuthenticatedEncryption.ConfigurationModel
{
    /// <summary>
    /// A descriptor which can create an authenticated encryption system based upon the
    /// configuration provided by an <see cref="CngGcmAuthenticatedEncryptionOptions"/> object.
    /// </summary>
    public sealed class CngGcmAuthenticatedEncryptorDescriptor : IAuthenticatedEncryptorDescriptor
    {
        private readonly ILogger _log;

        public CngGcmAuthenticatedEncryptorDescriptor([NotNull] CngGcmAuthenticatedEncryptionOptions options, [NotNull] ISecret masterKey)
            : this(options, masterKey, services: null)
        {
        }

        public CngGcmAuthenticatedEncryptorDescriptor([NotNull] CngGcmAuthenticatedEncryptionOptions options, [NotNull] ISecret masterKey, IServiceProvider services)
        {
            Options = options;
            MasterKey = masterKey;
            _log = services.GetLogger<CngGcmAuthenticatedEncryptorDescriptor>();
        }

        internal ISecret MasterKey { get; }

        internal CngGcmAuthenticatedEncryptionOptions Options { get; }

        public IAuthenticatedEncryptor CreateEncryptorInstance()
        {
            return Options.CreateAuthenticatedEncryptorInstance(MasterKey, _log);
        }

        public XmlSerializedDescriptorInfo ExportToXml()
        {
            // <descriptor>
            //   <!-- Windows CNG-GCM -->
            //   <encryption algorithm="..." keyLength="..." [provider="..."] />
            //   <masterKey>...</masterKey>
            // </descriptor>

            var encryptionElement = new XElement("encryption",
                new XAttribute("algorithm", Options.EncryptionAlgorithm),
                new XAttribute("keyLength", Options.EncryptionAlgorithmKeySize));
            if (Options.EncryptionAlgorithmProvider != null)
            {
                encryptionElement.SetAttributeValue("provider", Options.EncryptionAlgorithmProvider);
            }

            var rootElement = new XElement("descriptor",
                new XComment(" Algorithms provided by Windows CNG, using Galois/Counter Mode encryption and validation "),
                encryptionElement,
                MasterKey.ToMasterKeyElement());

            return new XmlSerializedDescriptorInfo(rootElement, typeof(CngGcmAuthenticatedEncryptorDescriptorDeserializer));
        }
    }
}
