// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Http.Features
{
    /// <summary>
    /// A feature to gracefully end a response.
    /// </summary>
    public interface IHttpResponseCompletionFeature
    {
        /// <summary>
        /// Flush any remaining response headers, data, or trailers.
        /// This may throw if the response is in an invalid state such as a Content-Length mismatch.
        /// </summary>
        /// <returns></returns>
        Task CompleteAsync();
    }
}
