// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.Testing.Internal
{
    /// <summary>
    /// Fake startup class used in functional tests to decorate the registration of
    /// ConfigureServices.
    /// </summary>
    /// <typeparam name="TStartup">The startup class of your application.</typeparam>
    public class TestStartup<TStartup> where TStartup : class
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly TestServiceRegistrations _registrations;
        private readonly TStartup _instance;

        public TestStartup(IServiceProvider serviceProvider, TestServiceRegistrations registrations)
        {
            _serviceProvider = serviceProvider;
            _registrations = registrations;
            _instance = (TStartup)ActivatorUtilities.CreateInstance(serviceProvider, typeof(TStartup));
        }

        public void ConfigureServices(IServiceCollection services)
        {
            var configureServices = _instance.GetType().GetMethod(nameof(ConfigureServices));
            var parameters = Enumerable.Repeat(services, 1)
                .Concat(configureServices
                    .GetParameters()
                    .Skip(1)
                    .Select(p => ActivatorUtilities.GetServiceOrCreateInstance(_serviceProvider, p.ParameterType)))
                .ToArray();

            _registrations.ConfigureServices(services, () => configureServices.Invoke(_instance, parameters));
        }

        public void Configure(IApplicationBuilder applicationBuilder)
        {
            var configure = _instance.GetType().GetMethod(nameof(Configure));
            var parameters = Enumerable.Repeat(applicationBuilder, 1)
                .Concat(configure
                    .GetParameters()
                    .Skip(1)
                    .Select(p => ActivatorUtilities.GetServiceOrCreateInstance(_serviceProvider, p.ParameterType)))
                .ToArray();

            configure.Invoke(_instance, parameters);
        }
    }
}
