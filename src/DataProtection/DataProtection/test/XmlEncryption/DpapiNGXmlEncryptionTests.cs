// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Xml.Linq;
using Microsoft.AspNetCore.DataProtection.Test.Shared;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Microsoft.AspNetCore.DataProtection.XmlEncryption
{
    public class DpapiNGXmlEncryptionTests
    {
        [ConditionalFact]
        [ConditionalRunTestOnlyOnWindows8OrLater]
        public void Encrypt_Decrypt_RoundTrips()
        {
            // Arrange
            var originalXml = XElement.Parse(@"<mySecret value='265ee4ea-ade2-43b1-b706-09b259e58b6b' />");
            var encryptor = new DpapiNGXmlEncryptor("LOCAL=user", DpapiNGProtectionDescriptorFlags.None, NullLoggerFactory.Instance);
            var decryptor = new DpapiNGXmlDecryptor();

            // Act & assert - run through encryptor and make sure we get back an obfuscated element
            var encryptedXmlInfo = encryptor.Encrypt(originalXml);
            Assert.Equal(typeof(DpapiNGXmlDecryptor), encryptedXmlInfo.DecryptorType);
            Assert.DoesNotContain("265ee4ea-ade2-43b1-b706-09b259e58b6b", encryptedXmlInfo.EncryptedElement.ToString(), StringComparison.OrdinalIgnoreCase);

            // Act & assert - run through decryptor and make sure we get back the original value
            var roundTrippedElement = decryptor.Decrypt(encryptedXmlInfo.EncryptedElement);
            XmlAssert.Equal(originalXml, roundTrippedElement);
        }
    }
}
