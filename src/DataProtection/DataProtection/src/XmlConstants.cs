// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Xml.Linq;

namespace Microsoft.AspNetCore.DataProtection
{
    /// <summary>
    /// Contains XLinq constants.
    /// </summary>
    internal static class XmlConstants
    {
        /// <summary>
        /// The root namespace used for all DataProtection-specific XML elements and attributes.
        /// </summary>
        private static readonly XNamespace RootNamespace = XNamespace.Get("http://schemas.asp.net/2015/03/dataProtection");

        /// <summary>
        /// Represents the type of decryptor that can be used when reading 'encryptedSecret' elements.
        /// </summary>
        internal static readonly XName DecryptorTypeAttributeName = "decryptorType";

        /// <summary>
        /// Elements with this attribute will be read with the specified deserializer type.
        /// </summary>
        internal static readonly XName DeserializerTypeAttributeName = "deserializerType";

        /// <summary>
        /// Elements with this name will be automatically decrypted when read by the XML key manager.
        /// </summary>
        internal static readonly XName EncryptedSecretElementName = RootNamespace.GetName("encryptedSecret");

        /// <summary>
        /// Elements where this attribute has a value of 'true' should be encrypted before storage.
        /// </summary>
        internal static readonly XName RequiresEncryptionAttributeName = RootNamespace.GetName("requiresEncryption");
    }
}
