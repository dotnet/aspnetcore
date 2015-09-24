// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Builder;
using Microsoft.Framework.DependencyInjection;

namespace Microsoft.AspNet.Hosting.Fakes
{
    public class StartupConfigureServicesThrows
    {
        public void ConfigureServices(IServiceCollection services)
        {
            throw new Exception("Exception from ConfigureServices");
        }

        public void Configure(IApplicationBuilder builder)
        {

        }
    }
}