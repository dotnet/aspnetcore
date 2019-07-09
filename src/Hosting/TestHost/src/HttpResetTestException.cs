// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.TestHost
{
    /// <summary>
    /// Used to surface to the test client that the application invoked <see cref="IHttpResetFeature.Reset"/>
    /// </summary>
    public class HttpResetTestException : Exception
    {
        /// <summary>
        /// Creates a new test exception
        /// </summary>
        /// <param name="errorCode">The error code passed to <see cref="IHttpResetFeature.Reset"/></param>
        public HttpResetTestException(int errorCode)
            : base($"The application reset the request with error code {errorCode}.")
        {
            ErrorCode = errorCode;
        }

        /// <summary>
        /// The error code passed to <see cref="IHttpResetFeature.Reset"/>
        /// </summary>
        public int ErrorCode { get; }
    }
}
