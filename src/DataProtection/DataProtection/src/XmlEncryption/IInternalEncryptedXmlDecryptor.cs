// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography.Xml;

namespace Microsoft.AspNetCore.DataProtection.XmlEncryption;

/// <summary>
/// Internal implementation details of <see cref="EncryptedXmlDecryptor"/> for unit testing.
/// </summary>
internal interface IInternalEncryptedXmlDecryptor
{
    void PerformPreDecryptionSetup(EncryptedXml encryptedXml);
}
