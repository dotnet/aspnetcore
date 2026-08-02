// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Security.Cryptography;
using Xunit;

namespace Microsoft.AspNetCore.InternalTesting;

internal static class ExceptionAssert2
{
    /// <summary>
    /// Verifies that the code throws a <see cref="CryptographicException"/>.
    /// </summary>
    /// <param name="testCode">A delegate to the code to be tested</param>
    /// <returns>The <see cref="CryptographicException"/> that was thrown, when successful</returns>
    /// <exception>Thrown when an exception was not thrown, or when an exception of the incorrect type is thrown</exception>
    public static CryptographicException ThrowsCryptographicException(Action testCode)
    {
        return Assert.Throws<CryptographicException>(testCode);
    }
}
