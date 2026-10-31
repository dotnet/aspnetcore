// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.DataProtection;

/// <summary>
/// An interface that can be used to create <see cref="IDataProtector"/> instances.
/// </summary>
public interface IDataProtectionProvider
{
    /// <summary>
    /// Creates an <see cref="IDataProtector"/> given a purpose.
    /// </summary>
    /// <param name="purpose">
    /// The purpose to be assigned to the newly-created <see cref="IDataProtector"/>.
    /// </param>
    /// <returns>An IDataProtector tied to the provided purpose.</returns>
    /// <remarks>
    /// The <paramref name="purpose"/> parameter must be unique for the intended use case; two
    /// different <see cref="IDataProtector"/> instances created with two different <paramref name="purpose"/>
    /// values will not be able to decipher each other's payloads. The <paramref name="purpose"/> parameter
    /// value is not intended to be kept secret.
    /// </remarks>
    IDataProtector CreateProtector(string purpose);
}
