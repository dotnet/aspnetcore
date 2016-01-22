// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Xml.Linq;

namespace Microsoft.AspNetCore.DataProtection.XmlEncryption
{
    /// <summary>
    /// The basic interface for decrypting an XML element.
    /// </summary>
    public interface IXmlDecryptor
    {
        /// <summary>
        /// Decrypts the specified XML element.
        /// </summary>
        /// <param name="encryptedElement">An encrypted XML element.</param>
        /// <returns>The decrypted form of <paramref name="encryptedElement"/>.</returns>
        /// <remarks>
        /// Implementations of this method must not mutate the <see cref="XElement"/>
        /// instance provided by <paramref name="encryptedElement"/>.
        /// </remarks>
        XElement Decrypt(XElement encryptedElement);
    }
}
