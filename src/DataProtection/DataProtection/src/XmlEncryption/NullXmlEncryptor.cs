// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.DataProtection.XmlEncryption
{
    /// <summary>
    /// An <see cref="IXmlEncryptor"/> that encrypts XML elements with a null encryptor.
    /// </summary>
    public sealed class NullXmlEncryptor : IXmlEncryptor
    {
        private readonly ILogger _logger;

        /// <summary>
        /// Creates a new instance of <see cref="NullXmlEncryptor"/>.
        /// </summary>
        public NullXmlEncryptor()
            : this(services: null)
        {
        }

        /// <summary>
        /// Creates a new instance of <see cref="NullXmlEncryptor"/>.
        /// </summary>
        /// <param name="services">An optional <see cref="IServiceProvider"/> to provide ancillary services.</param>
        public NullXmlEncryptor(IServiceProvider services)
        {
            _logger = services.GetLogger<NullXmlEncryptor>();
        }

        /// <summary>
        /// Encrypts the specified <see cref="XElement"/> with a null encryptor, i.e.,
        /// by returning the original value of <paramref name="plaintextElement"/> unencrypted.
        /// </summary>
        /// <param name="plaintextElement">The plaintext to echo back.</param>
        /// <returns>
        /// An <see cref="EncryptedXmlInfo"/> that contains the null-encrypted value of
        /// <paramref name="plaintextElement"/> along with information about how to
        /// decrypt it.
        /// </returns>
        public EncryptedXmlInfo Encrypt(XElement plaintextElement)
        {
            if (plaintextElement == null)
            {
                throw new ArgumentNullException(nameof(plaintextElement));
            }

            _logger?.EncryptingUsingNullEncryptor();

            // <unencryptedKey>
            //   <!-- This key is not encrypted. -->
            //   <plaintextElement />
            // </unencryptedKey>

            var newElement = new XElement("unencryptedKey",
                new XComment(" This key is not encrypted. "),
                new XElement(plaintextElement) /* copy ctor */);

            return new EncryptedXmlInfo(newElement, typeof(NullXmlDecryptor));
        }
    }
}
