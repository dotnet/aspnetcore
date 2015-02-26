// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.DataProtection.Managed;
using Microsoft.Framework.OptionsModel;

namespace Microsoft.AspNet.DataProtection.AuthenticatedEncryption
{
    public sealed class ManagedAuthenticatedEncryptorConfigurationFactory : IAuthenticatedEncryptorConfigurationFactory
    {
        private readonly ManagedAuthenticatedEncryptorConfigurationOptions _options;

        public ManagedAuthenticatedEncryptorConfigurationFactory([NotNull] IOptions<ManagedAuthenticatedEncryptorConfigurationOptions> optionsAccessor)
        {
            _options = optionsAccessor.Options.Clone();
        }

        public IAuthenticatedEncryptorConfiguration CreateNewConfiguration()
        {
            // generate a 512-bit secret randomly
            const int KDK_SIZE_IN_BYTES = 512 / 8;
            byte[] kdk = ManagedGenRandomImpl.Instance.GenRandom(KDK_SIZE_IN_BYTES);
            Secret secret;
            try
            {
                secret = new Secret(kdk);
            }
            finally
            {
                Array.Clear(kdk, 0, kdk.Length);
            }

            return new ManagedAuthenticatedEncryptorConfiguration(_options, secret);
        }
    }
}
