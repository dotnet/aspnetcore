// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Http.Features
{
    /// <summary>
    /// Provides acccess to the request-scoped <see cref="IServiceProvider"/>.
    /// </summary>
    public interface IServiceProvidersFeature
    {
        /// <summary>
        /// Gets or sets the <see cref="IServiceProvider"/> scoped to the current request.
        /// </summary>
        IServiceProvider RequestServices { get; set; }
    }
}
