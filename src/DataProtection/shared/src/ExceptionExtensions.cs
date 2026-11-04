// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Security.Cryptography;

namespace Microsoft.AspNetCore.DataProtection;

internal static class ExceptionExtensions
{
    /// <summary>
    /// Determines whether an exception must be homogenized by being wrapped inside a
    /// CryptographicException before being rethrown.
    /// </summary>
    public static bool RequiresHomogenization(this Exception ex)
    {
        return !(ex is CryptographicException);
    }
}
