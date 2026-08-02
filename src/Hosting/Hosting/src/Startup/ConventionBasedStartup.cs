// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Runtime.ExceptionServices;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Hosting;

internal sealed class ConventionBasedStartup : IStartup
{
    private readonly StartupMethods _methods;

    public ConventionBasedStartup(StartupMethods methods)
    {
        _methods = methods;
    }

    public void Configure(IApplicationBuilder app)
    {
        try
        {
            _methods.ConfigureDelegate(app);
        }
        catch (TargetInvocationException ex)
        {
            ExceptionDispatchInfo.Capture(ex.InnerException!).Throw();
            throw;
        }
    }

    public IServiceProvider ConfigureServices(IServiceCollection services)
    {
        try
        {
            return _methods.ConfigureServicesDelegate(services);
        }
        catch (TargetInvocationException ex)
        {
            ExceptionDispatchInfo.Capture(ex.InnerException!).Throw();
            throw;
        }
    }
}
