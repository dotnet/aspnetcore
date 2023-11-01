// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml.Linq;
using Microsoft.AspNetCore.DataProtection.Test.Shared;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.DataProtection.XmlEncryption;

public class DpapiXmlEncryptionTests
{
    [ConditionalTheory]
    [ConditionalRunTestOnlyOnWindows]
    [InlineData(true)]
    [InlineData(false)]
    public void Encrypt_CurrentUserOrLocalMachine_Decrypt_RoundTrips(bool protectToLocalMachine)
    {
        // Arrange
        var originalXml = XElement.Parse(@"<mySecret value='265ee4ea-ade2-43b1-b706-09b259e58b6b' />");
        var encryptor = new DpapiXmlEncryptor(protectToLocalMachine, NullLoggerFactory.Instance);
        var decryptor = new DpapiXmlDecryptor();

        // Act & assert - run through encryptor and make sure we get back an obfuscated element
        var encryptedXmlInfo = encryptor.Encrypt(originalXml);
        Assert.Equal(typeof(DpapiXmlDecryptor), encryptedXmlInfo.DecryptorType);
        Assert.DoesNotContain("265ee4ea-ade2-43b1-b706-09b259e58b6b", encryptedXmlInfo.EncryptedElement.ToString(), StringComparison.OrdinalIgnoreCase);

        // Act & assert - run through decryptor and make sure we get back the original value
        var roundTrippedElement = decryptor.Decrypt(encryptedXmlInfo.EncryptedElement);
        XmlAssert.Equal(originalXml, roundTrippedElement);
    }
}
