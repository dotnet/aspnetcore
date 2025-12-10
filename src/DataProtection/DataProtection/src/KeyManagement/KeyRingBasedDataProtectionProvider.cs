// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption;
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

        var currentKeyRing = _keyRingProvider.GetCurrentKeyRing();
        var encryptor = currentKeyRing.DefaultAuthenticatedEncryptor;

#if NET
        if (encryptor is ISpanAuthenticatedEncryptor)
        {
            // allows caller to check if dataProtector supports Span APIs
            // and use more performant APIs
            return new KeyRingBasedSpanDataProtector(
                logger: _logger,
                keyRingProvider: _keyRingProvider,
                originalPurposes: null,
                newPurpose: purpose);
        }
#endif

        return new KeyRingBasedDataProtector(
            logger: _logger,
            keyRingProvider: _keyRingProvider,
            originalPurposes: null,
            newPurpose: purpose);
    }
}
