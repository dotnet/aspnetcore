// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Cryptography;
using Xunit;

namespace Microsoft.AspNetCore.Testing
{
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
}
