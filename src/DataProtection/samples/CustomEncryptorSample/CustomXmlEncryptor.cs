// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml.Linq;
using Microsoft.AspNetCore.DataProtection.XmlEncryption;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CustomEncryptorSample;

public class CustomXmlEncryptor : IXmlEncryptor
{
    private readonly ILogger _logger;

    public CustomXmlEncryptor(IServiceProvider services)
    {
        _logger = services.GetRequiredService<ILoggerFactory>().CreateLogger<CustomXmlEncryptor>();
    }

    public EncryptedXmlInfo Encrypt(XElement plaintextElement)
    {
        ArgumentNullException.ThrowIfNull(plaintextElement);

        _logger.LogInformation("Not encrypting key");

        var newElement = new XElement("unencryptedKey",
            new XComment(" This key is not encrypted. "),
            new XElement(plaintextElement));
        var encryptedTextElement = new EncryptedXmlInfo(newElement, typeof(CustomXmlDecryptor));

        return encryptedTextElement;
    }
}
