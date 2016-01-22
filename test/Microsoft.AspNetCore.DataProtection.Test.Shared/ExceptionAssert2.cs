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
        /// Verifies that the code throws an <see cref="ArgumentNullException"/>.
        /// </summary>
        /// <param name="testCode">A delegate to the code to be tested</param>
        /// <param name="paramName">The name of the parameter that should throw the exception</param>
        /// <returns>The <see cref="ArgumentNullException"/> that was thrown, when successful</returns>
        /// <exception>Thrown when an exception was not thrown, or when an exception of the incorrect type is thrown</exception>
        public static ArgumentNullException ThrowsArgumentNull(Action testCode, string paramName)
        {
            var ex = Assert.Throws<ArgumentNullException>(testCode);
            Assert.Equal(paramName, ex.ParamName);
            return ex;
        }

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
