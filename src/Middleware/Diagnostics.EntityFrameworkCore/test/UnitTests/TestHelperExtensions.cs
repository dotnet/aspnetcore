// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.EntityFrameworkCore.Tests;

public static class TestHelperExtensions
{
    public static IServiceCollection AddProviderServices(this IServiceCollection serviceCollection)
    {
        return serviceCollection.AddEntityFrameworkInMemoryDatabase();
    }

    public static DbContextOptions UseProviderOptions(this DbContextOptions options)
    {
        return options;
    }
}
