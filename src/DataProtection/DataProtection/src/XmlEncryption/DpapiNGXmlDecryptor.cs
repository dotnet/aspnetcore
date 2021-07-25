// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Xml.Linq;
using Microsoft.AspNetCore.Cryptography;
using Microsoft.AspNetCore.DataProtection.Cng;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.DataProtection.XmlEncryption
{
    /// <summary>
    /// An <see cref="IXmlDecryptor"/> that decrypts XML elements that were encrypted with <see cref="DpapiNGXmlEncryptor"/>.
    /// </summary>
    /// <remarks>
    /// This API is only supported on Windows 8 / Windows Server 2012 and higher.
    /// </remarks>
    public sealed partial class DpapiNGXmlDecryptor : IXmlDecryptor
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
        public DpapiNGXmlDecryptor(IServiceProvider? services)
        {
            CryptoUtil.AssertPlatformIsWindows8OrLater();

            _logger = services?.GetLogger<DpapiNGXmlDecryptor>() ?? NullLogger.Instance;
        }

        /// <summary>
        /// Decrypts the specified XML element.
        /// </summary>
        /// <param name="encryptedElement">An encrypted XML element.</param>
        /// <returns>The decrypted form of <paramref name="encryptedElement"/>.</returns>
        public XElement Decrypt(XElement encryptedElement)
        {
            if (encryptedElement == null)
            {
                throw new ArgumentNullException(nameof(encryptedElement));
            }

            try
            {
                // <encryptedKey>
                //   <!-- This key is encrypted with {provider}. -->
                //   <!-- rule string -->
                //   <value>{base64}</value>
                // </encryptedKey>

                var protectedSecret = Convert.FromBase64String((string)encryptedElement.Element("value")!);
                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    string? protectionDescriptorRule;
                    try
                    {
                        protectionDescriptorRule = DpapiSecretSerializerHelper.GetRuleFromDpapiNGProtectedPayload(protectedSecret);
                    }
                    catch
                    {
                        // swallow all errors - it's just a log
                        protectionDescriptorRule = null;
                    }

                    Log.DecryptingSecretElementUsingWindowsDPAPING(_logger, protectionDescriptorRule);
                }

                using (var secret = DpapiSecretSerializerHelper.UnprotectWithDpapiNG(protectedSecret))
                {
                    return secret.ToXElement();
                }
            }
            catch (Exception ex)
            {
                // It's OK for us to log the error, as we control the exception, and it doesn't contain
                // sensitive information.
                Log.ExceptionOccurredTryingToDecryptElement(_logger, ex);
                throw;
            }
        }

        private partial class Log
        {
            [LoggerMessage(42, LogLevel.Debug, "Decrypting secret element using Windows DPAPI-NG with protection descriptor rule '{DescriptorRule}'.", EventName = "DecryptingSecretElementUsingWindowsDPAPING", SkipEnabledCheck = true)]
            public static partial void DecryptingSecretElementUsingWindowsDPAPING(ILogger logger, string? descriptorRule);

            [LoggerMessage(43, LogLevel.Error, "An exception occurred while trying to decrypt the element.", EventName = "ExceptionOccurredTryingToDecryptElement")]
            public static partial void ExceptionOccurredTryingToDecryptElement(ILogger logger, Exception exception);
        }
    }
}
