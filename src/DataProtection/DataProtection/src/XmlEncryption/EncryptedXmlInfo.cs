// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Xml.Linq;
using Microsoft.AspNetCore.Shared;

namespace Microsoft.AspNetCore.DataProtection.XmlEncryption;

/// <summary>
/// Wraps an <see cref="XElement"/> that contains a blob of encrypted XML
/// and information about the class which can be used to decrypt it.
/// </summary>
public sealed class EncryptedXmlInfo
{
    /// <summary>
    /// Creates an instance of an <see cref="EncryptedXmlInfo"/>.
    /// </summary>
    /// <param name="encryptedElement">A piece of encrypted XML.</param>
    /// <param name="decryptorType">The class whose <see cref="IXmlDecryptor.Decrypt(XElement)"/>
    /// method can be used to decrypt <paramref name="encryptedElement"/>.</param>
    public EncryptedXmlInfo(XElement encryptedElement, Type decryptorType)
    {
        ArgumentNullThrowHelper.ThrowIfNull(encryptedElement);
        ArgumentNullThrowHelper.ThrowIfNull(decryptorType);

        if (!typeof(IXmlDecryptor).IsAssignableFrom(decryptorType))
        {
            throw new ArgumentException(
                Resources.FormatTypeExtensions_BadCast(decryptorType.FullName, typeof(IXmlDecryptor).FullName),
                nameof(decryptorType));
        }

        EncryptedElement = encryptedElement;
        DecryptorType = decryptorType;
    }

    /// <summary>
    /// The class whose <see cref="IXmlDecryptor.Decrypt(XElement)"/> method can be used to
    /// decrypt the value stored in <see cref="EncryptedElement"/>.
    /// </summary>
    public Type DecryptorType { get; }

    /// <summary>
    /// A piece of encrypted XML.
    /// </summary>
    public XElement EncryptedElement { get; }
}
