// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Framework.Internal;
using Microsoft.Framework.Logging;

namespace Microsoft.AspNet.DataProtection.KeyManagement
{
    internal unsafe sealed class KeyRingBasedDataProtectionProvider : IDataProtectionProvider
    {
        private readonly IKeyRingProvider _keyRingProvider;
        private readonly ILogger _logger;

        public KeyRingBasedDataProtectionProvider(IKeyRingProvider keyRingProvider, IServiceProvider services)
        {
            _keyRingProvider = keyRingProvider;
            _logger = services.GetLogger<KeyRingBasedDataProtector>(); // note: for protector (not provider!) type, could be null
        }

        public IDataProtector CreateProtector([NotNull] string purpose)
        {
            return new KeyRingBasedDataProtector(
                logger: _logger,
                keyRingProvider: _keyRingProvider,
                originalPurposes: null,
                newPurpose: purpose);
        }
    }
}
