// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Components.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Components.Hosting
{
    internal interface IBlazorStartup
    {
        void ConfigureServices(IServiceCollection services);

        void Configure(IBlazorApplicationBuilder app, IServiceProvider services);
    }
}
