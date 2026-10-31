// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.XmlEncryption;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Security.Cryptography.X509Certificates;
using System.Xml.Linq;

var cert = new X509Certificate2(Convert.FromBase64String(Constants.Key), Constants.Password);
var encryptedData = XElement.Parse(Constants.KeyRingXmlContents)
    .Element("descriptor")
    .Element("descriptor")
    .Element(XName.Get("encryptedSecret", "http://schemas.asp.net/2015/03/dataProtection"))
    .Element(XName.Get("EncryptedData", "http://www.w3.org/2001/04/xmlenc#"));

var services = new ServiceCollection();
services.AddOptions();
var dpBuilder = new DataProtectionBuilder(services);
dpBuilder.UnprotectKeysWithAnyCertificate(cert);
var decryptor = new EncryptedXmlDecryptor(services.BuildServiceProvider());

var e = decryptor.Decrypt(encryptedData);

if (e.Name != "masterKey")
{
    return -1;
}
if (e.Value != "HfIK4QgxlajUlAj2se0A90ZAtJmkI4zOLQrCwEl86WM77WlKbDQlXhnd/DYDZKHUW6t0pg0J054XFJeFZ4U6hg==")
{
    return -2;
}

return 100;

internal sealed class DataProtectionBuilder : IDataProtectionBuilder
{
    public DataProtectionBuilder(IServiceCollection services) => Services = services;
    public IServiceCollection Services { get; }
}
