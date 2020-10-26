// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// A builder that can be used to configure Server-Side Blazor.
    /// </summary>
    public interface IServerSideBlazorBuilder
    {
        /// <summary>
        /// Gets the <see cref="IServiceCollection"/>.
        /// </summary>
        IServiceCollection Services { get; }
    }
}
