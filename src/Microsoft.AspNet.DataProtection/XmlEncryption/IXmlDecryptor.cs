// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Xml.Linq;

namespace Microsoft.AspNet.DataProtection.XmlEncryption
{
    /// <summary>
    /// The basic interface for decrypting an XML element.
    /// </summary>
    public interface IXmlDecryptor
    {
        /// <summary>
        /// Decrypts the specified XML element.
        /// </summary>
        /// <param name="encryptedElement">The encrypted XML element to decrypt. This element is unchanged by the method.</param>
        /// <returns>The decrypted form of the XML element.</returns>
        XElement Decrypt(XElement encryptedElement);
    }
}
