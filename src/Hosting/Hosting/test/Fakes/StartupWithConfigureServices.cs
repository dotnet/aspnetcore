// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Hosting.Fakes;

public class StartupWithConfigureServices
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IFoo, Foo>();
    }

    public void Configure(IApplicationBuilder app, IFoo foo)
    {
        foo.Bar();
    }

    public interface IFoo
    {
        bool Invoked { get; }
        void Bar();
    }

    public class Foo : IFoo
    {
        public bool Invoked { get; private set; }

        public void Bar()
        {
            Invoked = true;
        }
    }
}
