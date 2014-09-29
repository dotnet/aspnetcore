// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Xml.Linq;

namespace Microsoft.AspNet.Security.DataProtection.XmlEncryption
{
    /// <summary>
    /// The basic interface for encrypting an XML element.
    /// </summary>
    public interface IXmlEncryptor
    {
        /// <summary>
        /// Encrypts the specified XML element.
        /// </summary>
        /// <param name="plaintextElement">The plaintext XML element to encrypt. This element is unchanged by the method.</param>
        /// <returns>The encrypted form of the XML element.</returns>
        XElement Encrypt(XElement plaintextElement);
    }
}
