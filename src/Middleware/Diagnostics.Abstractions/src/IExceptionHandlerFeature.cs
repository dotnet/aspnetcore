// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.Diagnostics
{
    /// <summary>
    /// Represents a feature containing the error of the original request to be examined by an exception handler.
    /// </summary>
    public interface IExceptionHandlerFeature
    {
        /// <summary>
        /// The error encountered during the original request
        /// </summary>
        Exception Error { get; }
    }
}
