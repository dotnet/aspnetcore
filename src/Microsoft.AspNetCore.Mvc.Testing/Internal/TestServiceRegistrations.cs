// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.Testing.Internal
{
    /// <summary>
    /// Helper class to orchestrate service registrations in <see cref="TestStartup{TStartup}"/>.
    /// </summary>
    public class TestServiceRegistrations
    {
        public IList<Action<IServiceCollection>> Before { get; set; } = new List<Action<IServiceCollection>>();
        public IList<Action<IServiceCollection>> After { get; set; } = new List<Action<IServiceCollection>>();

        public void ConfigureServices(IServiceCollection services, Action startupConfigureServices)
        {
            foreach (var config in Before)
            {
                config(services);
            }

            startupConfigureServices();

            foreach (var config in After)
            {
                config(services);
            }
        }
    }
}
