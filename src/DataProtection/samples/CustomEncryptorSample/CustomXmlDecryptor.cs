// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml.Linq;
using Microsoft.AspNetCore.DataProtection.XmlEncryption;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CustomEncryptorSample;

public class CustomXmlDecryptor : IXmlDecryptor
{
    private readonly ILogger _logger;

    public CustomXmlDecryptor(IServiceProvider services)
    {
        _logger = services.GetRequiredService<ILoggerFactory>().CreateLogger<CustomXmlDecryptor>();
    }

    public XElement Decrypt(XElement encryptedElement)
    {
        ArgumentNullException.ThrowIfNull(encryptedElement);

        return new XElement(encryptedElement.Elements().Single());
    }
}
