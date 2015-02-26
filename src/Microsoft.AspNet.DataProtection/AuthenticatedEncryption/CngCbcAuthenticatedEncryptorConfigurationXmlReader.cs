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
    internal sealed class CngCbcAuthenticatedEncryptorConfigurationXmlReader : IAuthenticatedEncryptorConfigurationXmlReader
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ITypeActivator _typeActivator;

        public CngCbcAuthenticatedEncryptorConfigurationXmlReader(
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
            //   <validation algorithm="{STRING}" provider="{STRING}" />
            //   <secret>...</secret>
            // </cbcEncryptor>

            CryptoUtil.Assert(element.Name == CngCbcAuthenticatedEncryptorConfiguration.CbcEncryptorElementName,
                @"TODO: Bad element.");

            var options = new CngCbcAuthenticatedEncryptorConfigurationOptions();

            // read <encryption> element
            var encryptionElement = element.Element(CngCbcAuthenticatedEncryptorConfiguration.EncryptionElementName);
            options.EncryptionAlgorithm = (string)encryptionElement.Attribute("algorithm");
            options.EncryptionAlgorithmProvider = (string)encryptionElement.Attribute("provider");
            options.EncryptionAlgorithmKeySize = (int)encryptionElement.Attribute("keyLength");

            // read <validation> element
            var validationElement = element.Element(CngCbcAuthenticatedEncryptorConfiguration.ValidationElementName);
            options.HashAlgorithm = (string)validationElement.Attribute("algorithm");
            options.HashAlgorithmProvider = (string)validationElement.Attribute("provider");

            // read the child of the <secret> element, then decrypt it
            var encryptedSecretElement = element.Element(CngCbcAuthenticatedEncryptorConfiguration.SecretElementName).Elements().Single();
            var secretElementDecryptorTypeName = (string)encryptedSecretElement.Attribute("decryptor");
            var secretElementDecryptorType = Type.GetType(secretElementDecryptorTypeName, throwOnError: true);
            var secretElementDecryptor = (IXmlDecryptor)_typeActivator.CreateInstance(_serviceProvider, secretElementDecryptorType);
            var decryptedSecretElement = secretElementDecryptor.Decrypt(encryptedSecretElement);
            CryptoUtil.Assert(decryptedSecretElement.Name == CngCbcAuthenticatedEncryptorConfiguration.SecretElementName,
                @"TODO: Bad element.");

            byte[] decryptedSecretBytes = Convert.FromBase64String((string)decryptedSecretElement);
            try
            {
                var secret = new Secret(decryptedSecretBytes);
                return new CngCbcAuthenticatedEncryptorConfiguration(options, secret);
            }
            finally
            {
                Array.Clear(decryptedSecretBytes, 0, decryptedSecretBytes.Length);
            }
        }
    }
}
