// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Reflection;

namespace Microsoft.AspNetCore.Routing
{
    public abstract class ApplicationDataSourceFactory
    {
        public abstract IEndpointConventionBuilder GetOrCreateApplication(IEndpointRouteBuilder builder);

        public abstract IEndpointConventionBuilder GetOrCreateAssembly(IEndpointRouteBuilder builder, Assembly assembly);
    }
}
