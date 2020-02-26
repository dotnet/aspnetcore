// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Hosting.Fakes
{
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
}