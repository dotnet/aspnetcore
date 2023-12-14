// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.DataProtection.XmlEncryption;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Security.Cryptography.X509Certificates;
using System.Xml.Linq;

var cert = new X509Certificate2(Convert.FromBase64String(Constants.Key), Constants.Password);

var encryptor = new CertificateXmlEncryptor(cert, NullLoggerFactory.Instance);

var e = XElement.Parse("""
    <root>
      <child Value="hi" />
    </root>
    """);

var result = encryptor.Encrypt(e);

if (result.DecryptorType.Name != "EncryptedXmlDecryptor")
{
    return -1;
}
if (result.EncryptedElement.Name.LocalName != "EncryptedData")
{
    return -2;
}

return 100;
