// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Xml.Linq;
using Microsoft.AspNet.Cryptography;

namespace Microsoft.AspNet.DataProtection.XmlEncryption
{
    /// <summary>
    /// A class that can decrypt XML elements which were encrypted using a null encryptor.
    /// </summary>
    internal unsafe sealed class NullXmlDecryptor : IXmlDecryptor
    {
        public XElement Decrypt([NotNull] XElement encryptedElement)
        {
            CryptoUtil.Assert(encryptedElement.Name == NullXmlEncryptor.NullEncryptedSecretElementName,
                "TODO: Incorrect element.");

            return encryptedElement.Elements().Single();
        }
    }
}
