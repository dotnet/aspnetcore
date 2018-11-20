// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Http.Features
{
    /// <summary>
    /// Controls the IO behavior for the <see cref="IHttpRequestFeature.Body"/> and <see cref="IHttpResponseFeature.Body"/> 
    /// </summary>
    public interface IHttpBodyControlFeature
    {
        /// <summary>
        /// Gets or sets a value that controls whether synchronous IO is allowed for the <see cref="IHttpRequestFeature.Body"/> and <see cref="IHttpResponseFeature.Body"/> 
        /// </summary>
        bool AllowSynchronousIO { get; set; }
    }
}
