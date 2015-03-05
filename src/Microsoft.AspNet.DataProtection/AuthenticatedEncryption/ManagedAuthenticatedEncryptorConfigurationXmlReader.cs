// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Xml.Linq;
using Microsoft.AspNet.Cryptography;
using Microsoft.AspNet.DataProtection.XmlEncryption;
using Microsoft.Framework.DependencyInjection;

namespace Microsoft.AspNet.DataProtection.AuthenticatedEncryption
{
    internal sealed class ManagedAuthenticatedEncryptorConfigurationXmlReader : IAuthenticatedEncryptorConfigurationXmlReader
    {
        private readonly IServiceProvider _serviceProvider;

        public ManagedAuthenticatedEncryptorConfigurationXmlReader(
            [NotNull] IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IAuthenticatedEncryptorConfiguration FromXml([NotNull] XElement element)
        {
            // <managedEncryptor reader="{TYPE}">
            //   <encryption type="{STRING}" keyLength="{INT}" />
            //   <validation type="{STRING}" />
            //   <secret>...</secret>
            // </managedEncryptor>

            CryptoUtil.Assert(element.Name == ManagedAuthenticatedEncryptorConfiguration.EncryptionElementName,
                @"TODO: Bad element.");

            var options = new ManagedAuthenticatedEncryptorConfigurationOptions();

            // read <encryption> element
            var encryptionElement = element.Element(ManagedAuthenticatedEncryptorConfiguration.EncryptionElementName);
            options.EncryptionAlgorithmType = Type.GetType((string)encryptionElement.Attribute("type"), throwOnError: true);
            options.EncryptionAlgorithmKeySize = (int)encryptionElement.Attribute("keyLength");

            // read <validation> element
            var validationElement = element.Element(ManagedAuthenticatedEncryptorConfiguration.ValidationElementName);
            options.ValidationAlgorithmType = Type.GetType((string)validationElement.Attribute("type"), throwOnError: true);

            // read the child of the <secret> element, then decrypt it
            var encryptedSecretElement = element.Element(ManagedAuthenticatedEncryptorConfiguration.SecretElementName).Elements().Single();
            var secretElementDecryptorTypeName = (string)encryptedSecretElement.Attribute("decryptor");
            var secretElementDecryptorType = Type.GetType(secretElementDecryptorTypeName, throwOnError: true);
            var secretElementDecryptor = (IXmlDecryptor)ActivatorUtilities.CreateInstance(_serviceProvider, secretElementDecryptorType);
            var decryptedSecretElement = secretElementDecryptor.Decrypt(encryptedSecretElement);
            CryptoUtil.Assert(decryptedSecretElement.Name == ManagedAuthenticatedEncryptorConfiguration.SecretElementName,
                @"TODO: Bad element.");

            byte[] decryptedSecretBytes = Convert.FromBase64String((string)decryptedSecretElement);
            try
            {
                var secret = new Secret(decryptedSecretBytes);
                return new ManagedAuthenticatedEncryptorConfiguration(options, secret);
            }
            finally
            {
                Array.Clear(decryptedSecretBytes, 0, decryptedSecretBytes.Length);
            }
        }
    }
}
