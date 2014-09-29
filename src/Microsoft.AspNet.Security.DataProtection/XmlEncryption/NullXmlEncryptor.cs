// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Xml.Linq;
using Microsoft.AspNet.Security.DataProtection.KeyManagement;

namespace Microsoft.AspNet.Security.DataProtection.XmlEncryption
{
    /// <summary>
    /// A class that performs null XML encryption (just returns the plaintext).
    /// </summary>
    public sealed class NullXmlEncryptor : IXmlEncryptor
    {
        internal static readonly XName NullEncryptedSecretElementName = XmlKeyManager.KeyManagementXmlNamespace.GetName("nullEncryptedSecret");

        /// <summary>
        /// Encrypts the specified XML element using a null encryptor.
        /// </summary>
        /// <param name="plaintextElement">The plaintext XML element to encrypt. This element is unchanged by the method.</param>
        /// <returns>The null-encrypted form of the XML element.</returns>
        public XElement Encrypt([NotNull] XElement plaintextElement)
        {
            // <nullEncryptedSecret decryptor="{TYPE}">
            //   <plaintextElement />
            // </nullEncryptedSecret>
            return new XElement(NullEncryptedSecretElementName,
                new XAttribute("decryptor", typeof(NullXmlDecryptor).AssemblyQualifiedName),
                plaintextElement);
        }
    }
}
