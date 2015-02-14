// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Session;

namespace Microsoft.Framework.DependencyInjection
{
    public static class CachingServicesExtensions
    {
        public static IServiceCollection AddSessionServices(this IServiceCollection collection)
        {
            return collection.AddTransient<ISessionStore, DistributedSessionStore>();
        }
    }
}