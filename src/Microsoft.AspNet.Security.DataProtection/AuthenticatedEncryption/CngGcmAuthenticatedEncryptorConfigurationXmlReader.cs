// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Xml.Linq;
using Microsoft.AspNet.Cryptography;
using Microsoft.AspNet.Security.DataProtection.XmlEncryption;
using Microsoft.Framework.DependencyInjection;

namespace Microsoft.AspNet.Security.DataProtection.AuthenticatedEncryption
{
    internal sealed class CngGcmAuthenticatedEncryptorConfigurationXmlReader : IAuthenticatedEncryptorConfigurationXmlReader
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ITypeActivator _typeActivator;

        public CngGcmAuthenticatedEncryptorConfigurationXmlReader(
            [NotNull] IServiceProvider serviceProvider,
            [NotNull] ITypeActivator typeActivator)
        {
            _serviceProvider = serviceProvider;
            _typeActivator = typeActivator;
        }

        public IAuthenticatedEncryptorConfiguration FromXml([NotNull] XElement element)
        {
            // <cbcEncryptor reader="{TYPE}">
            //   <encryption algorithm="{STRING}" provider="{STRING}" keyLength="{INT}" />
            //   <secret>...</secret>
            // </cbcEncryptor>

            CryptoUtil.Assert(element.Name == CngGcmAuthenticatedEncryptorConfiguration.GcmEncryptorElementName,
                @"TODO: Bad element.");

            var options = new CngGcmAuthenticatedEncryptorConfigurationOptions();

            // read <encryption> element
            var encryptionElement = element.Element(CngGcmAuthenticatedEncryptorConfiguration.EncryptionElementName);
            options.EncryptionAlgorithm = (string)encryptionElement.Attribute("algorithm");
            options.EncryptionAlgorithmProvider = (string)encryptionElement.Attribute("provider");
            options.EncryptionAlgorithmKeySize = (int)encryptionElement.Attribute("keyLength");

            // read the child of the <secret> element, then decrypt it
            var encryptedSecretElement = element.Element(CngGcmAuthenticatedEncryptorConfiguration.SecretElementName).Elements().Single();
            var secretElementDecryptorTypeName = (string)encryptedSecretElement.Attribute("decryptor");
            var secretElementDecryptorType = Type.GetType(secretElementDecryptorTypeName, throwOnError: true);
            var secretElementDecryptor = (IXmlDecryptor)_typeActivator.CreateInstance(_serviceProvider, secretElementDecryptorType);
            var decryptedSecretElement = secretElementDecryptor.Decrypt(encryptedSecretElement);
            CryptoUtil.Assert(decryptedSecretElement.Name == CngGcmAuthenticatedEncryptorConfiguration.SecretElementName,
                @"TODO: Bad element.");

            byte[] decryptedSecretBytes = Convert.FromBase64String((string)decryptedSecretElement);
            try
            {
                var secret = new Secret(decryptedSecretBytes);
                return new CngGcmAuthenticatedEncryptorConfiguration(options, secret);
            }
            finally
            {
                Array.Clear(decryptedSecretBytes, 0, decryptedSecretBytes.Length);
            }
        }
    }
}
