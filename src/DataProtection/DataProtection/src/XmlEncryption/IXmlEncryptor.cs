// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml.Linq;

namespace Microsoft.AspNetCore.DataProtection.XmlEncryption;

/// <summary>
/// The basic interface for encrypting XML elements.
/// </summary>
public interface IXmlEncryptor
{
    /// <summary>
    /// Encrypts the specified <see cref="XElement"/>.
    /// </summary>
    /// <param name="plaintextElement">The plaintext to encrypt.</param>
    /// <returns>
    /// An <see cref="EncryptedXmlInfo"/> that contains the encrypted value of
    /// <paramref name="plaintextElement"/> along with information about how to
    /// decrypt it.
    /// </returns>
    /// <remarks>
    /// Implementations of this method must not mutate the <see cref="XElement"/>
    /// instance provided by <paramref name="plaintextElement"/>.
    /// </remarks>
    EncryptedXmlInfo Encrypt(XElement plaintextElement);
}
