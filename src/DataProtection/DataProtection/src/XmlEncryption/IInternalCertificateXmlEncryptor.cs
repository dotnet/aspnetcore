// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography.Xml;
using System.Xml;

namespace Microsoft.AspNetCore.DataProtection.XmlEncryption;

/// <summary>
/// Internal implementation details of <see cref="CertificateXmlEncryptor"/> for unit testing.
/// </summary>
internal interface IInternalCertificateXmlEncryptor
{
    EncryptedData PerformEncryption(EncryptedXml encryptedXml, XmlElement elementToEncrypt);
}
