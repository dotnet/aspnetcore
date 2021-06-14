// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography;
using Microsoft.AspNetCore.Cryptography.Cng;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.Managed;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption
{
    /// <summary>
    /// An <see cref="IAuthenticatedEncryptorFactory"/> to create an <see cref="IAuthenticatedEncryptor"/>
    /// based on the <see cref="AuthenticatedEncryptorConfiguration"/>.
    /// </summary>
    public sealed class AuthenticatedEncryptorFactory : IAuthenticatedEncryptorFactory
    {
        private readonly ILoggerFactory _loggerFactory;

        /// <summary>
        /// Initializes a new instance of <see cref="AuthenticatedEncryptorFactory"/>.
        /// </summary>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
        public AuthenticatedEncryptorFactory(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
        }

        /// <inheritdoc />
        public IAuthenticatedEncryptor? CreateEncryptorInstance(IKey key)
        {
            if (key.Descriptor is not AuthenticatedEncryptorDescriptor descriptor)
            {
                return null;
            }

            return CreateAuthenticatedEncryptorInstance(descriptor.MasterKey, descriptor.Configuration);
        }

        [return: NotNullIfNotNull("authenticatedConfiguration")]
        internal IAuthenticatedEncryptor? CreateAuthenticatedEncryptorInstance(
            ISecret secret,
            AuthenticatedEncryptorConfiguration? authenticatedConfiguration)
        {
            if (authenticatedConfiguration == null)
            {
                return null;
            }

            if (IsGcmAlgorithm(authenticatedConfiguration.EncryptionAlgorithm))
            {
#if NETCOREAPP
                return new AesGcmAuthenticatedEncryptor(secret, GetAlgorithmKeySizeInBits(authenticatedConfiguration.EncryptionAlgorithm) / 8);
#else
                // GCM requires CNG, and CNG is only supported on Windows.
                if (!OSVersionUtil.IsWindows())
                {
                    throw new PlatformNotSupportedException(Resources.Platform_WindowsRequiredForGcm);
                }

                Debug.Assert(RuntimeInformation.IsOSPlatform(OSPlatform.Windows));

                var configuration = new CngGcmAuthenticatedEncryptorConfiguration()
                {
                    EncryptionAlgorithm = GetBCryptAlgorithmNameFromEncryptionAlgorithm(authenticatedConfiguration.EncryptionAlgorithm),
                    EncryptionAlgorithmKeySize = GetAlgorithmKeySizeInBits(authenticatedConfiguration.EncryptionAlgorithm)
                };

                return new CngGcmAuthenticatedEncryptorFactory(_loggerFactory).CreateAuthenticatedEncryptorInstance(secret, configuration);
#endif
            }
            else
            {
                if (OSVersionUtil.IsWindows())
                {
                    Debug.Assert(RuntimeInformation.IsOSPlatform(OSPlatform.Windows));
                    // CNG preferred over managed implementations if running on Windows
                    var configuration = new CngCbcAuthenticatedEncryptorConfiguration()
                    {
                        EncryptionAlgorithm = GetBCryptAlgorithmNameFromEncryptionAlgorithm(authenticatedConfiguration.EncryptionAlgorithm),
                        EncryptionAlgorithmKeySize = GetAlgorithmKeySizeInBits(authenticatedConfiguration.EncryptionAlgorithm),
                        HashAlgorithm = GetBCryptAlgorithmNameFromValidationAlgorithm(authenticatedConfiguration.ValidationAlgorithm)
                    };

                    return new CngCbcAuthenticatedEncryptorFactory(_loggerFactory).CreateAuthenticatedEncryptorInstance(secret, configuration);
                }
                else
                {
                    // Use managed implementations as a fallback
                    var configuration = new ManagedAuthenticatedEncryptorConfiguration()
                    {
                        EncryptionAlgorithmType = GetManagedTypeFromEncryptionAlgorithm(authenticatedConfiguration.EncryptionAlgorithm),
                        EncryptionAlgorithmKeySize = GetAlgorithmKeySizeInBits(authenticatedConfiguration.EncryptionAlgorithm),
                        ValidationAlgorithmType = GetManagedTypeFromValidationAlgorithm(authenticatedConfiguration.ValidationAlgorithm)
                    };

                    return new ManagedAuthenticatedEncryptorFactory(_loggerFactory).CreateAuthenticatedEncryptorInstance(secret, configuration);
                }
            }
        }

        internal static bool IsGcmAlgorithm(EncryptionAlgorithm algorithm)
        {
            return (EncryptionAlgorithm.AES_128_GCM <= algorithm && algorithm <= EncryptionAlgorithm.AES_256_GCM);
        }

        private static int GetAlgorithmKeySizeInBits(EncryptionAlgorithm algorithm)
        {
            switch (algorithm)
            {
                case EncryptionAlgorithm.AES_128_CBC:
                case EncryptionAlgorithm.AES_128_GCM:
                    return 128;

                case EncryptionAlgorithm.AES_192_CBC:
                case EncryptionAlgorithm.AES_192_GCM:
                    return 192;

                case EncryptionAlgorithm.AES_256_CBC:
                case EncryptionAlgorithm.AES_256_GCM:
                    return 256;

                default:
                    throw new ArgumentOutOfRangeException(nameof(algorithm));
            }
        }

        private static string GetBCryptAlgorithmNameFromEncryptionAlgorithm(EncryptionAlgorithm algorithm)
        {
            switch (algorithm)
            {
                case EncryptionAlgorithm.AES_128_CBC:
                case EncryptionAlgorithm.AES_192_CBC:
                case EncryptionAlgorithm.AES_256_CBC:
                case EncryptionAlgorithm.AES_128_GCM:
                case EncryptionAlgorithm.AES_192_GCM:
                case EncryptionAlgorithm.AES_256_GCM:
                    return Constants.BCRYPT_AES_ALGORITHM;

                default:
                    throw new ArgumentOutOfRangeException(nameof(algorithm));
            }
        }

        private static string GetBCryptAlgorithmNameFromValidationAlgorithm(ValidationAlgorithm algorithm)
        {
            switch (algorithm)
            {
                case ValidationAlgorithm.HMACSHA256:
                    return Constants.BCRYPT_SHA256_ALGORITHM;

                case ValidationAlgorithm.HMACSHA512:
                    return Constants.BCRYPT_SHA512_ALGORITHM;

                default:
                    throw new ArgumentOutOfRangeException(nameof(algorithm));
            }
        }

        private static Type GetManagedTypeFromEncryptionAlgorithm(EncryptionAlgorithm algorithm)
        {
            switch (algorithm)
            {
                case EncryptionAlgorithm.AES_128_CBC:
                case EncryptionAlgorithm.AES_192_CBC:
                case EncryptionAlgorithm.AES_256_CBC:
                case EncryptionAlgorithm.AES_128_GCM:
                case EncryptionAlgorithm.AES_192_GCM:
                case EncryptionAlgorithm.AES_256_GCM:
                    return typeof(Aes);

                default:
                    throw new ArgumentOutOfRangeException(nameof(algorithm));
            }
        }

        private static Type GetManagedTypeFromValidationAlgorithm(ValidationAlgorithm algorithm)
        {
            switch (algorithm)
            {
                case ValidationAlgorithm.HMACSHA256:
                    return typeof(HMACSHA256);

                case ValidationAlgorithm.HMACSHA512:
                    return typeof(HMACSHA512);

                default:
                    throw new ArgumentOutOfRangeException(nameof(algorithm));
            }
        }
    }
}
