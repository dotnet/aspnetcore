// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography;
using Microsoft.AspNetCore.Cryptography.Cng;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption
{
    /// <summary>
    /// Options for configuring authenticated encryption algorithms.
    /// </summary>
    public sealed class AuthenticatedEncryptionOptions : IInternalAuthenticatedEncryptionOptions
    {
        /// <summary>
        /// The algorithm to use for symmetric encryption (confidentiality).
        /// </summary>
        /// <remarks>
        /// The default value is <see cref="EncryptionAlgorithm.AES_256_CBC"/>.
        /// </remarks>
        public EncryptionAlgorithm EncryptionAlgorithm { get; set; } = EncryptionAlgorithm.AES_256_CBC;

        /// <summary>
        /// The algorithm to use for message authentication (tamper-proofing).
        /// </summary>
        /// <remarks>
        /// The default value is <see cref="ValidationAlgorithm.HMACSHA256"/>.
        /// This property is ignored if <see cref="EncryptionAlgorithm"/> specifies a 'GCM' algorithm.
        /// </remarks>
        public ValidationAlgorithm ValidationAlgorithm { get; set; } = ValidationAlgorithm.HMACSHA256;

        /// <summary>
        /// Validates that this <see cref="AuthenticatedEncryptionOptions"/> is well-formed, i.e.,
        /// that the specified algorithms actually exist and that they can be instantiated properly.
        /// An exception will be thrown if validation fails.
        /// </summary>
        public void Validate()
        {
            // Run a sample payload through an encrypt -> decrypt operation to make sure data round-trips properly.
            var encryptor = CreateAuthenticatedEncryptorInstance(Secret.Random(512 / 8));
            try
            {
                encryptor.PerformSelfTest();
            }
            finally
            {
                (encryptor as IDisposable)?.Dispose();
            }
        }

        /*
         * HELPER ROUTINES
         */

        internal IAuthenticatedEncryptor CreateAuthenticatedEncryptorInstance(ISecret secret, IServiceProvider services = null)
        {
            return CreateImplementationOptions()
                .ToConfiguration(services)
                .CreateDescriptorFromSecret(secret)
                .CreateEncryptorInstance();
        }

        private IInternalAuthenticatedEncryptionOptions CreateImplementationOptions()
        {
            if (IsGcmAlgorithm(EncryptionAlgorithm))
            {
                // GCM requires CNG, and CNG is only supported on Windows.
                if (!OSVersionUtil.IsWindows())
                {
                    throw new PlatformNotSupportedException(Resources.Platform_WindowsRequiredForGcm);
                }
                return new CngGcmAuthenticatedEncryptionOptions()
                {
                    EncryptionAlgorithm = GetBCryptAlgorithmName(EncryptionAlgorithm),
                    EncryptionAlgorithmKeySize = GetAlgorithmKeySizeInBits(EncryptionAlgorithm)
                };
            }
            else
            {
                if (OSVersionUtil.IsWindows())
                {
                    // CNG preferred over managed implementations if running on Windows
                    return new CngCbcAuthenticatedEncryptionOptions()
                    {
                        EncryptionAlgorithm = GetBCryptAlgorithmName(EncryptionAlgorithm),
                        EncryptionAlgorithmKeySize = GetAlgorithmKeySizeInBits(EncryptionAlgorithm),
                        HashAlgorithm = GetBCryptAlgorithmName(ValidationAlgorithm)
                    };
                }
                else
                {
                    // Use managed implementations as a fallback
                    return new ManagedAuthenticatedEncryptionOptions()
                    {
                        EncryptionAlgorithmType = GetManagedTypeForAlgorithm(EncryptionAlgorithm),
                        EncryptionAlgorithmKeySize = GetAlgorithmKeySizeInBits(EncryptionAlgorithm),
                        ValidationAlgorithmType = GetManagedTypeForAlgorithm(ValidationAlgorithm)
                    };
                }
            }
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

        private static string GetBCryptAlgorithmName(EncryptionAlgorithm algorithm)
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

        private static string GetBCryptAlgorithmName(ValidationAlgorithm algorithm)
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

        private static Type GetManagedTypeForAlgorithm(EncryptionAlgorithm algorithm)
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

        private static Type GetManagedTypeForAlgorithm(ValidationAlgorithm algorithm)
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

        internal static bool IsGcmAlgorithm(EncryptionAlgorithm algorithm)
        {
            return (EncryptionAlgorithm.AES_128_GCM <= algorithm && algorithm <= EncryptionAlgorithm.AES_256_GCM);
        }

        IInternalAuthenticatedEncryptorConfiguration IInternalAuthenticatedEncryptionOptions.ToConfiguration(IServiceProvider services)
        {
            return new AuthenticatedEncryptorConfiguration(this, services);
        }
    }
}
