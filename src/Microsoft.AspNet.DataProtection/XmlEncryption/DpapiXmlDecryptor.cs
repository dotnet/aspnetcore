// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Xml.Linq;
using Microsoft.AspNet.Cryptography;
using Microsoft.AspNet.DataProtection.Cng;
using Microsoft.Framework.Internal;
using Microsoft.Framework.Logging;

namespace Microsoft.AspNet.DataProtection.XmlEncryption
{
    /// <summary>
    /// An <see cref="IXmlDecryptor"/> that decrypts XML elements that were encrypted with <see cref="DpapiXmlEncryptor"/>.
    /// </summary>
    public sealed class DpapiXmlDecryptor : IXmlDecryptor
    {
        private readonly ILogger _logger;

        /// <summary>
        /// Creates a new instance of a <see cref="DpapiXmlDecryptor"/>.
        /// </summary>
        public DpapiXmlDecryptor()
            : this(services: null)
        {
        }

        /// <summary>
        /// Creates a new instance of a <see cref="DpapiXmlDecryptor"/>.
        /// </summary>
        /// <param name="services">An optional <see cref="IServiceProvider"/> to provide ancillary services.</param>
        public DpapiXmlDecryptor(IServiceProvider services)
        {
            CryptoUtil.AssertPlatformIsWindows();

            _logger = services.GetLogger<DpapiXmlDecryptor>();
        }

        /// <summary>
        /// Decrypts the specified XML element.
        /// </summary>
        /// <param name="encryptedElement">An encrypted XML element.</param>
        /// <returns>The decrypted form of <paramref name="encryptedElement"/>.</returns>
        /// <remarks>
        public XElement Decrypt([NotNull] XElement encryptedElement)
        {
            if (_logger.IsVerboseLevelEnabled())
            {
                _logger.LogVerbose("Decrypting secret element using Windows DPAPI.");
            }

            try
            {
                // <encryptedKey>
                //   <!-- This key is encrypted with {provider}. -->
                //   <value>{base64}</value>
                // </encryptedKey>

                byte[] protectedSecret = Convert.FromBase64String((string)encryptedElement.Element("value"));
                using (Secret secret = DpapiSecretSerializerHelper.UnprotectWithDpapi(protectedSecret))
                {
                    return secret.ToXElement();
                }
            }
            catch (Exception ex)
            {
                // It's OK for us to log the error, as we control the exception, and it doesn't contain
                // sensitive information.
                if (_logger.IsErrorLevelEnabled())
                {
                    _logger.LogError(ex, "An exception occurred while trying to decrypt the element.");
                }
                throw;
            }
        }
    }
}
