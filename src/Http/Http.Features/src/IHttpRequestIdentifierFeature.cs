// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Http.Features
{
    /// <summary>
    /// Feature to uniquely identify a request.
    /// </summary>
    public interface IHttpRequestIdentifierFeature
    {
        /// <summary>
        /// Gets or sets a value to uniquely identify a request.
        /// This can be used for logging and diagnostics.
        /// </summary>
        string TraceIdentifier { get; set; }
    }
}
