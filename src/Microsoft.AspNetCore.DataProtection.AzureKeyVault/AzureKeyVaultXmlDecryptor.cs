// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.AspNetCore.DataProtection.XmlEncryption;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.DataProtection.AzureKeyVault
{
    internal class AzureKeyVaultXmlDecryptor: IXmlDecryptor
    {
        private readonly IKeyVaultWrappingClient _client;

        public AzureKeyVaultXmlDecryptor(IServiceProvider serviceProvider)
        {
            _client = serviceProvider.GetService<IKeyVaultWrappingClient>();
        }

        public XElement Decrypt(XElement encryptedElement)
        {
            return DecryptAsync(encryptedElement).GetAwaiter().GetResult();
        }

        private async Task<XElement> DecryptAsync(XElement encryptedElement)
        {
            var kid = (string)encryptedElement.Element("kid");
            var symmetricKey = Convert.FromBase64String((string)encryptedElement.Element("key"));
            var symmetricIV = Convert.FromBase64String((string)encryptedElement.Element("iv"));

            var encryptedValue = Convert.FromBase64String((string)encryptedElement.Element("value"));

            var result = await _client.UnwrapKeyAsync(kid, AzureKeyVaultXmlEncryptor.DefaultKeyEncryption, symmetricKey);

            byte[] decryptedValue;
            using (var symmetricAlgorithm = AzureKeyVaultXmlEncryptor.DefaultSymmetricAlgorithmFactory())
            {
                using (var decryptor = symmetricAlgorithm.CreateDecryptor(result.Result, symmetricIV))
                {
                    decryptedValue = decryptor.TransformFinalBlock(encryptedValue, 0, encryptedValue.Length);
                }
            }

            using (var memoryStream = new MemoryStream(decryptedValue))
            {
                return XElement.Load(memoryStream);
            }
        }
    }
}