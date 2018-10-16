// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Xml.Linq;
using Microsoft.AspNetCore.DataProtection.XmlEncryption;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CustomEncryptorSample
{
    public class CustomXmlDecryptor : IXmlDecryptor
    {
        private readonly ILogger _logger;

        public CustomXmlDecryptor(IServiceProvider services)
        {
            _logger = services.GetRequiredService<ILoggerFactory>().CreateLogger<CustomXmlDecryptor>();
        }

        public XElement Decrypt(XElement encryptedElement)
        {
            if (encryptedElement == null)
            {
                throw new ArgumentNullException(nameof(encryptedElement));
            }

            return new XElement(encryptedElement.Elements().Single());
        }
    }
}
