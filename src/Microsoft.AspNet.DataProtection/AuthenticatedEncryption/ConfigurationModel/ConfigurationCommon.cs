// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.DataProtection.AuthenticatedEncryption.ConfigurationModel
{
    internal static class ConfigurationCommon
    {
        /// <summary>
        /// Creates an <see cref="IAuthenticatedEncryptorDescriptor"/> from this <see cref="IInternalAuthenticatedEncryptorConfiguration"/>
        /// using a random 512-bit master key generated from a secure PRNG.
        /// </summary>
        public static IAuthenticatedEncryptorDescriptor CreateNewDescriptorCore(this IInternalAuthenticatedEncryptorConfiguration configuration)
        {
            const int KDK_SIZE_IN_BYTES = 512 / 8;
            return configuration.CreateDescriptorFromSecret(Secret.Random(KDK_SIZE_IN_BYTES));
        }
    }
}
