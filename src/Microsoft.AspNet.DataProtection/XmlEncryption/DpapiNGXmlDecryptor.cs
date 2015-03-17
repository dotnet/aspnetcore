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
    /// An <see cref="IXmlDecryptor"/> that decrypts XML elements that were encrypted with <see cref="DpapiNGXmlEncryptor"/>.
    /// </summary>
    /// <remarks>
    /// This API is only supported on Windows 8 / Windows Server 2012 and higher.
    /// </remarks>
    public sealed class DpapiNGXmlDecryptor : IXmlDecryptor
    {
        private readonly ILogger _logger;

        /// <summary>
        /// Creates a new instance of a <see cref="DpapiNGXmlDecryptor"/>.
        /// </summary>
        public DpapiNGXmlDecryptor()
            : this(services: null)
        {
        }

        /// <summary>
        /// Creates a new instance of a <see cref="DpapiNGXmlDecryptor"/>.
        /// </summary>
        /// <param name="services">An optional <see cref="IServiceProvider"/> to provide ancillary services.</param>
        public DpapiNGXmlDecryptor(IServiceProvider services)
        {
            CryptoUtil.AssertPlatformIsWindows8OrLater();

            _logger = services.GetLogger<DpapiNGXmlDecryptor>();
        }

        /// <summary>
        /// Decrypts the specified XML element.
        /// </summary>
        /// <param name="encryptedElement">An encrypted XML element.</param>
        /// <returns>The decrypted form of <paramref name="encryptedElement"/>.</returns>
        /// <remarks>
        public XElement Decrypt([NotNull] XElement encryptedElement)
        {
            try
            {
                // <encryptedKey>
                //   <!-- This key is encrypted with {provider}. -->
                //   <!-- rule string -->
                //   <value>{base64}</value>
                // </encryptedKey>

                byte[] protectedSecret = Convert.FromBase64String((string)encryptedElement.Element("value"));
                if (_logger.IsVerboseLevelEnabled())
                {
                    string protectionDescriptorRule;
                    try
                    {
                        protectionDescriptorRule = DpapiSecretSerializerHelper.GetRuleFromDpapiNGProtectedPayload(protectedSecret);
                    }
                    catch
                    {
                        // swallow all errors - it's just a log
                        protectionDescriptorRule = null;
                    }
                    _logger.LogVerboseF($"Decrypting secret element using Windows DPAPI-NG with protection descriptor rule '{protectionDescriptorRule}'.");
                }

                using (Secret secret = DpapiSecretSerializerHelper.UnprotectWithDpapiNG(protectedSecret))
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
