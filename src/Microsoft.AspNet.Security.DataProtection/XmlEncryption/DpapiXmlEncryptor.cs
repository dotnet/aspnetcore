// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Xml.Linq;
using Microsoft.AspNet.Security.DataProtection.Cng;
using Microsoft.AspNet.Security.DataProtection.KeyManagement;

namespace Microsoft.AspNet.Security.DataProtection.XmlEncryption
{
    /// <summary>
    /// A class that can encrypt XML elements using Windows DPAPI.
    /// </summary>
    public sealed class DpapiXmlEncryptor : IXmlEncryptor
    {
        internal static readonly XName DpapiEncryptedSecretElementName = XmlKeyManager.KeyManagementXmlNamespace.GetName("dpapiEncryptedSecret");

        private readonly bool _protectToLocalMachine;

        public DpapiXmlEncryptor(bool protectToLocalMachine)
        {
            _protectToLocalMachine = protectToLocalMachine;
        }

        /// <summary>
        /// Encrypts the specified XML element using Windows DPAPI.
        /// </summary>
        /// <param name="plaintextElement">The plaintext XML element to encrypt. This element is unchanged by the method.</param>
        /// <returns>The encrypted form of the XML element.</returns>
        public XElement Encrypt([NotNull] XElement plaintextElement)
        {
            // First, convert the XML element to a byte[] so that it can be encrypted.
            ProtectedMemoryBlob secret;
            using (var memoryStream = new MemoryStream())
            {
                plaintextElement.Save(memoryStream);

#if !ASPNETCORE50
                // If we're on full desktop CLR, utilize the underlying buffer directly as an optimization.
                byte[] underlyingBuffer = memoryStream.GetBuffer();
                secret = new ProtectedMemoryBlob(new ArraySegment<byte>(underlyingBuffer, 0, checked((int)memoryStream.Length)));
                Array.Clear(underlyingBuffer, 0, underlyingBuffer.Length);
#else
                // Otherwise, need to make a copy of the buffer.
                byte[] clonedBuffer = memoryStream.ToArray();
                secret = new ProtectedMemoryBlob(clonedBuffer);
                Array.Clear(clonedBuffer, 0, clonedBuffer.Length);
#endif
            }

            // <secret decryptor="{TYPE}">
            //   ... base64 data ...
            // </secret>
            byte[] encryptedBytes = DpapiSecretSerializerHelper.ProtectWithDpapi(secret, protectToLocalMachine: _protectToLocalMachine);
            return new XElement(DpapiEncryptedSecretElementName,
                new XAttribute("decryptor", typeof(DpapiXmlDecryptor).AssemblyQualifiedName),
                new XAttribute("version", 1),
                Convert.ToBase64String(encryptedBytes));
        }
    }
}
