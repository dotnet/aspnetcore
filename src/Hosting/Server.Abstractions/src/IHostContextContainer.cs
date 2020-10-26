// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Hosting.Server.Abstractions
{
    /// <summary>
    /// When implemented by a Server allows an <see cref="IHttpApplication{TContext}"/> to pool and reuse
    /// its <typeparamref name="TContext"/> between requests.
    /// </summary>
    /// <typeparam name="TContext">The <see cref="IHttpApplication{TContext}"/> Host context</typeparam>
    public interface IHostContextContainer<TContext>
    {
        TContext HostContext { get; set; }
    }
}
