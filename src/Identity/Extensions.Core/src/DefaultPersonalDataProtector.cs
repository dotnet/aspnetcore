// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

namespace Microsoft.AspNetCore.Identity;

/// <summary>
/// Default implementation of <see cref="IPersonalDataProtector"/> that uses <see cref="ILookupProtectorKeyRing"/>
/// and <see cref="ILookupProtector"/> to protect data with a payload format of {keyId}:{protectedData}
/// </summary>
public class DefaultPersonalDataProtector : IPersonalDataProtector
{
    private readonly ILookupProtectorKeyRing _keyRing;
    private readonly ILookupProtector _encryptor;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="keyRing"></param>
    /// <param name="protector"></param>
    public DefaultPersonalDataProtector(ILookupProtectorKeyRing keyRing, ILookupProtector protector)
    {
        _keyRing = keyRing;
        _encryptor = protector;
    }

    /// <summary>
    /// Unprotect the data.
    /// </summary>
    /// <param name="data">The data to unprotect.</param>
    /// <returns>The unprotected data.</returns>
    public virtual string? Unprotect(string? data)
    {
        Debug.Assert(data != null);
        var split = data.IndexOf(':');
        if (split == -1 || split == data.Length - 1)
        {
            throw new InvalidOperationException("Malformed data.");
        }

        var keyId = data.Substring(0, split);
        return _encryptor.Unprotect(keyId, data.Substring(split + 1));
    }

    /// <summary>
    /// Protect the data.
    /// </summary>
    /// <param name="data">The data to protect.</param>
    /// <returns>The protected data.</returns>
    public virtual string? Protect(string? data)
    {
        var current = _keyRing.CurrentKeyId;
        return current + ":" + _encryptor.Protect(current, data);
    }
}
