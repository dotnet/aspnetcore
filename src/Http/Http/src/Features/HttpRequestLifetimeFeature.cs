// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;

namespace Microsoft.AspNetCore.Http.Features
{
    /// <summary>
    /// Default implementation for <see cref="IHttpRequestLifetimeFeature"/>.
    /// </summary>
    public class HttpRequestLifetimeFeature : IHttpRequestLifetimeFeature
    {
        /// <inheritdoc />
        public CancellationToken RequestAborted { get; set; }

        /// <inheritdoc />
        public void Abort()
        {
        }
    }
}
