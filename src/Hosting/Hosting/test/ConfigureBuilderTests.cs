// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Hosting.Tests;

public class ConfigureBuilderTests
{
    [Fact]
    public void CapturesServiceExceptionDetails()
    {
        var methodInfo = GetType().GetMethod(nameof(InjectedMethod), BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(methodInfo);

        var services = new ServiceCollection()
            .AddSingleton<CrasherService>()
            .BuildServiceProvider();

        var applicationBuilder = new ApplicationBuilder(services);

        var builder = new ConfigureBuilder(methodInfo);
        Action<IApplicationBuilder> action = builder.Build(instance: null);
        var ex = Assert.Throws<InvalidOperationException>(() => action.Invoke(applicationBuilder));

        Assert.NotNull(ex);
        Assert.Equal($"Could not resolve a service of type '{typeof(CrasherService).FullName}' for the parameter"
            + $" 'service' of method '{methodInfo.Name}' on type '{methodInfo.DeclaringType.FullName}'.", ex.Message);

        // the inner exception contains the root cause
        Assert.NotNull(ex.InnerException);
        Assert.Equal("Service instantiation failed", ex.InnerException.Message);
        Assert.Contains(nameof(CrasherService), ex.InnerException.StackTrace);
    }

    private static void InjectedMethod(CrasherService service)
    {
        Assert.NotNull(service);
    }

    private class CrasherService
    {
        public CrasherService()
        {
            throw new Exception("Service instantiation failed");
        }
    }
}
