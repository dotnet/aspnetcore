// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Data.Entity.Infrastructure;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class EntityFrameworkServiceBuilderExtension
    {
        public static IServiceCollection ServiceCollection(this EntityFrameworkServicesBuilder services)
            => services.GetInfrastructure();
    }
}
