// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Hosting.Fakes
{
    public class StartupBase
    {
        public void ConfigureBaseClassServices(IServiceCollection services)
        {
            services.AddOptions();
            services.Configure<FakeOptions>(o =>
            {
                o.Configured = true;
                o.Environment = "BaseClass";
            });
        }
    }
}