// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.Security.DataProtection.KeyManagement
{
    internal unsafe sealed class KeyRingBasedDataProtectionProvider : IDataProtectionProvider
    {
        private readonly IKeyRingProvider _keyringProvider;

        public KeyRingBasedDataProtectionProvider(IKeyRingProvider keyringProvider)
        {
            _keyringProvider = keyringProvider;
        }

        public IDataProtector CreateProtector([NotNull] string purpose)
        {
            return new KeyRingBasedDataProtector(_keyringProvider, new[] { purpose });
        }
    }
}
