// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.DataProtection.KeyManagement.Internal;
using Microsoft.AspNetCore.Shared;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.DataProtection.KeyManagement;

internal sealed unsafe class KeyRingBasedDataProtectionProvider : IDataProtectionProvider
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
        ArgumentNullThrowHelper.ThrowIfNull(purpose);

        return new KeyRingBasedDataProtector(
            logger: _logger,
            keyRingProvider: _keyRingProvider,
            originalPurposes: null,
            newPurpose: purpose);
    }
}
