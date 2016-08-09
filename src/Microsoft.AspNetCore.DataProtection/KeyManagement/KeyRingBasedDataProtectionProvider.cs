// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.DataProtection.KeyManagement.Internal;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.DataProtection.KeyManagement
{
    internal unsafe sealed class KeyRingBasedDataProtectionProvider : IDataProtectionProvider
    {
        private readonly IKeyRingProvider _keyRingProvider;
        private readonly ILogger _logger;

        public KeyRingBasedDataProtectionProvider(IKeyRingProvider keyRingProvider, ILoggerFactory loggerFactory)
        {
            _keyRingProvider = keyRingProvider;
            _logger = loggerFactory.CreateLogger<KeyRingBasedDataProtector>(); // note: for protector (not provider!) type
        }

        public IDataProtector CreateProtector(string purpose)
        {
            if (purpose == null)
            {
                throw new ArgumentNullException(nameof(purpose));
            }

            return new KeyRingBasedDataProtector(
                logger: _logger,
                keyRingProvider: _keyRingProvider,
                originalPurposes: null,
                newPurpose: purpose);
        }
    }
}
