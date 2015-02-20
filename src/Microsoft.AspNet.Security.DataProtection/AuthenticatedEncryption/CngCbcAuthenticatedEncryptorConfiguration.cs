// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Xml.Linq;
using Microsoft.AspNet.Cryptography;
using Microsoft.AspNet.Security.DataProtection.XmlEncryption;

namespace Microsoft.AspNet.Security.DataProtection.AuthenticatedEncryption
{
    internal sealed class CngCbcAuthenticatedEncryptorConfiguration : IAuthenticatedEncryptorConfiguration
    {
        internal static readonly XNamespace XmlNamespace = XNamespace.Get("http://www.asp.net/2014/dataProtection/cng");
        internal static readonly XName CbcEncryptorElementName = XmlNamespace.GetName("cbcEncryptor");
        internal static readonly XName EncryptionElementName = XmlNamespace.GetName("encryption");
        internal static readonly XName SecretElementName = XmlNamespace.GetName("secret");
        internal static readonly XName ValidationElementName = XmlNamespace.GetName("validation");

        private readonly CngCbcAuthenticatedEncryptorConfigurationOptions _options;
        private readonly ISecret _secret;

        public CngCbcAuthenticatedEncryptorConfiguration(CngCbcAuthenticatedEncryptorConfigurationOptions options, ISecret secret)
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
            // <cbcEncryptor reader="{TYPE}">
            //   <encryption algorithm="{STRING}" provider="{STRING}" keyLength="{INT}" />
            //   <validation algorithm="{STRING}" provider="{STRING}" />
            //   <secret>...</secret>
            // </cbcEncryptor>

            return new XElement(CbcEncryptorElementName,
                new XAttribute("reader", typeof(CngCbcAuthenticatedEncryptorConfigurationXmlReader).AssemblyQualifiedName),
                new XElement(EncryptionElementName,
                    new XAttribute("algorithm", _options.EncryptionAlgorithm),
                    new XAttribute("provider", _options.EncryptionAlgorithmProvider ?? String.Empty),
                    new XAttribute("keyLength", _options.EncryptionAlgorithmKeySize)),
                new XElement(ValidationElementName,
                    new XAttribute("algorithm", _options.HashAlgorithm),
                    new XAttribute("provider", _options.HashAlgorithmProvider ?? String.Empty)),
                EncryptSecret(xmlEncryptor));
        }
    }
}
