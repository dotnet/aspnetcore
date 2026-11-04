// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Xml.Linq;
using Microsoft.AspNetCore.Shared;

namespace Microsoft.AspNetCore.DataProtection.XmlEncryption;

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
    public XElement Decrypt(XElement encryptedElement)
    {
        ArgumentNullThrowHelper.ThrowIfNull(encryptedElement);

        // <unencryptedKey>
        //   <!-- This key is not encrypted. -->
        //   <plaintextElement />
        // </unencryptedKey>

        // Return a clone of the single child node.
        return new XElement(encryptedElement.Elements().Single());
    }
}
