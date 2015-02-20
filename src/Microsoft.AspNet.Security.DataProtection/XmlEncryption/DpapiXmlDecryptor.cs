// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Xml.Linq;
using Microsoft.AspNet.Cryptography;
using Microsoft.AspNet.Security.DataProtection.Cng;

namespace Microsoft.AspNet.Security.DataProtection.XmlEncryption
{
    /// <summary>
    /// A class that can decrypt XML elements which were encrypted using Windows DPAPI.
    /// </summary>
    internal unsafe sealed class DpapiXmlDecryptor : IXmlDecryptor
    {
        /// <summary>
        /// Decrypts the specified XML element using Windows DPAPI.
        /// </summary>
        /// <param name="encryptedElement">The encrypted XML element to decrypt. This element is unchanged by the method.</param>
        /// <returns>The decrypted form of the XML element.</returns>
        public XElement Decrypt([NotNull] XElement encryptedElement)
        {
            CryptoUtil.Assert(encryptedElement.Name == DpapiXmlEncryptor.DpapiEncryptedSecretElementName,
                "TODO: Incorrect element.");

            int version = (int)encryptedElement.Attribute("version");
            CryptoUtil.Assert(version == 1, "TODO: Bad version.");

            byte[] dpapiProtectedBytes = Convert.FromBase64String(encryptedElement.Value);
            using (var secret = DpapiSecretSerializerHelper.UnprotectWithDpapi(dpapiProtectedBytes))
            {
                byte[] plaintextXmlBytes = new byte[secret.Length];
                try
                {
                    secret.WriteSecretIntoBuffer(new ArraySegment<byte>(plaintextXmlBytes));
                    using (var memoryStream = new MemoryStream(plaintextXmlBytes, writable: false))
                    {
                        return XElement.Load(memoryStream);
                    }
                }
                finally
                {
                    Array.Clear(plaintextXmlBytes, 0, plaintextXmlBytes.Length);
                }
            }
        }
    }
}
