// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Xml.Linq;
using Microsoft.AspNet.DataProtection.AuthenticatedEncryption;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.DataProtection.XmlEncryption
{
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
        EncryptedXmlInfo Encrypt([NotNull] XElement plaintextElement);
    }
}
