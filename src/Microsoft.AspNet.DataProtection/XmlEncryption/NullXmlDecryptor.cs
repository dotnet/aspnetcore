// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Xml.Linq;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.DataProtection.XmlEncryption
{
    /// <summary>
    /// An <see cref="IXmlDecryptor"/> that decrypts XML elements with a null decryptor.
    /// </summary>
    public sealed class NullXmlDecryptor : IXmlDecryptor
    {
        /// <summary>
        /// Decrypts the specified XML element.
        /// </summary>
        /// <param name="encryptedElement">An encrypted XML element.</param>
        /// <returns>The decrypted form of <paramref name="encryptedElement"/>.</returns>
        /// <remarks>
        public XElement Decrypt([NotNull] XElement encryptedElement)
        {
            // <unencryptedKey>
            //   <!-- This key is not encrypted. -->
            //   <plaintextElement />
            // </unencryptedKey>

            // Return a clone of the single child node.
            return new XElement(encryptedElement.Elements().Single());
        }
    }
}
