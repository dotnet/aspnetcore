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

#if NET
        // Always return the span-capable protector on .NET. It inspects the resolved encryptor at each Protect/Unprotect call
        // and falls back to the byte[] path when the encryptor does not implement ISpanAuthenticatedEncryptor.
        //
        // We could determine whether the encryptor implements ISpanAuthenticatedEncryptor here and return the appropriate protector type,
        // but that forces a keyring resolve (which may hit the configured key store, e.g. database or blob storage) during startup, which is not expected. See dotnet/aspnetcore#67447.
        return new KeyRingBasedSpanDataProtector(
            logger: _logger,
            keyRingProvider: _keyRingProvider,
            originalPurposes: null,
            newPurpose: purpose);
#else
        return new KeyRingBasedDataProtector(
            logger: _logger,
            keyRingProvider: _keyRingProvider,
            originalPurposes: null,
            newPurpose: purpose);
#endif
    }
}
