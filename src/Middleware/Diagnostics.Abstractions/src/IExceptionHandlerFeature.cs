// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
