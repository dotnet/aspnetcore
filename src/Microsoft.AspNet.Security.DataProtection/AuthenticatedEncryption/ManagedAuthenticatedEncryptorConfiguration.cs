// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Xml.Linq;
using Microsoft.AspNet.Cryptography;
using Microsoft.AspNet.Security.DataProtection.XmlEncryption;

namespace Microsoft.AspNet.Security.DataProtection.AuthenticatedEncryption
{
    internal sealed class ManagedAuthenticatedEncryptorConfiguration : IAuthenticatedEncryptorConfiguration
    {
        internal static readonly XNamespace XmlNamespace = XNamespace.Get("http://www.asp.net/2014/dataProtection/managed");
        internal static readonly XName ManagedEncryptorElementName = XmlNamespace.GetName("managedEncryptor");
        internal static readonly XName EncryptionElementName = XmlNamespace.GetName("encryption");
        internal static readonly XName SecretElementName = XmlNamespace.GetName("secret");
        internal static readonly XName ValidationElementName = XmlNamespace.GetName("validation");

        private readonly ManagedAuthenticatedEncryptorConfigurationOptions _options;
        private readonly ISecret _secret;

        public ManagedAuthenticatedEncryptorConfiguration(ManagedAuthenticatedEncryptorConfigurationOptions options, ISecret secret)
        {
            _options = options;
            _secret = secret;
        }

        public IAuthenticatedEncryptor CreateEncryptorInstance()
        {
            return _options.CreateAuthenticatedEncryptor(_secret);
        }

        private XElement EncryptSecret(IXmlEncryptor encryptor)
        {
            // First, create the inner <secret> element.
            XElement secretElement;
            byte[] plaintextSecret = new byte[_secret.Length];
            try
            {
                _secret.WriteSecretIntoBuffer(new ArraySegment<byte>(plaintextSecret));
                secretElement = new XElement(SecretElementName, Convert.ToBase64String(plaintextSecret));
            }
            finally
            {
                Array.Clear(plaintextSecret, 0, plaintextSecret.Length);
            }

            // Then encrypt it and wrap it in another <secret> element.
            var encryptedSecretElement = encryptor.Encrypt(secretElement);
            CryptoUtil.Assert(!String.IsNullOrEmpty((string)encryptedSecretElement.Attribute("decryptor")),
                @"TODO: <secret> encryption was invalid.");

            return new XElement(SecretElementName, encryptedSecretElement);
        }

        public XElement ToXml([NotNull] IXmlEncryptor xmlEncryptor)
        {
            // <managedEncryptor reader="{TYPE}">
            //   <encryption type="{TYPE}" keyLength="{INT}" />
            //   <validation type="{TYPE}" />
            //   <secret>...</secret>
            // </managedEncryptor>

            return new XElement(ManagedEncryptorElementName,
                new XAttribute("reader", typeof(ManagedAuthenticatedEncryptorConfigurationXmlReader).AssemblyQualifiedName),
                new XElement(EncryptionElementName,
                    new XAttribute("type", _options.EncryptionAlgorithmType),
                    new XAttribute("keyLength", _options.EncryptionAlgorithmKeySize)),
                new XElement(ValidationElementName,
                    new XAttribute("type", _options.ValidationAlgorithmType)),
                EncryptSecret(xmlEncryptor));
        }
    }
}
