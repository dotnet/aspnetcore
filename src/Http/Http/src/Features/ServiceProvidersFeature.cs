// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Http.Features
{
    /// <summary>
    /// Default implementation for <see cref="IServiceProvidersFeature"/>.
    /// </summary>
    public class ServiceProvidersFeature : IServiceProvidersFeature
    {
        /// <inheritdoc />
        public IServiceProvider RequestServices { get; set; } = default!;
    }
}
