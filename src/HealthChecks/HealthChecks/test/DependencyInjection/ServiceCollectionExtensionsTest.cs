// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Microsoft.Extensions.DependencyInjection
{
    public class ServiceCollectionExtensionsTest
    {
        [Fact]
        public void AddHealthChecks_RegistersSingletonHealthCheckServiceIdempotently()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddHealthChecks();
            services.AddHealthChecks();

            // Assert
            Assert.Collection(services.OrderBy(s => s.ServiceType.FullName),
                actual =>
                {
                    Assert.Equal(ServiceLifetime.Singleton, actual.Lifetime);
                    Assert.Equal(typeof(HealthCheckService), actual.ServiceType);
                    Assert.Equal(typeof(DefaultHealthCheckService), actual.ImplementationType);
                    Assert.Null(actual.ImplementationInstance);
                    Assert.Null(actual.ImplementationFactory);
                },
                actual =>
                {
                    Assert.Equal(ServiceLifetime.Singleton, actual.Lifetime);
                    Assert.Equal(typeof(IHostedService), actual.ServiceType);
                    Assert.Equal(typeof(HealthCheckPublisherHostedService), actual.ImplementationType);
                    Assert.Null(actual.ImplementationInstance);
                    Assert.Null(actual.ImplementationFactory);
                });
        }

        [Fact] // see: https://github.com/dotnet/extensions/issues/639
        public void AddHealthChecks_RegistersPublisherService_WhenOtherHostedServicesRegistered()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddSingleton<IHostedService, DummyHostedService>();
            services.AddHealthChecks();

            // Assert
            Assert.Collection(services.OrderBy(s => s.ServiceType.FullName).ThenBy(s => s.ImplementationType.FullName),
                actual =>
                {
                    Assert.Equal(ServiceLifetime.Singleton, actual.Lifetime);
                    Assert.Equal(typeof(HealthCheckService), actual.ServiceType);
                    Assert.Equal(typeof(DefaultHealthCheckService), actual.ImplementationType);
                    Assert.Null(actual.ImplementationInstance);
                    Assert.Null(actual.ImplementationFactory);
                },
                actual =>
                {
                    Assert.Equal(ServiceLifetime.Singleton, actual.Lifetime);
                    Assert.Equal(typeof(IHostedService), actual.ServiceType);
                    Assert.Equal(typeof(DummyHostedService), actual.ImplementationType);
                    Assert.Null(actual.ImplementationInstance);
                    Assert.Null(actual.ImplementationFactory);
                },
                actual =>
                {
                    Assert.Equal(ServiceLifetime.Singleton, actual.Lifetime);
                    Assert.Equal(typeof(IHostedService), actual.ServiceType);
                    Assert.Equal(typeof(HealthCheckPublisherHostedService), actual.ImplementationType);
                    Assert.Null(actual.ImplementationInstance);
                    Assert.Null(actual.ImplementationFactory);
                });
        }

        private class DummyHostedService : IHostedService
        {
            public Task StartAsync(CancellationToken cancellationToken)
            {
                throw new System.NotImplementedException();
            }

            public Task StopAsync(CancellationToken cancellationToken)
            {
                throw new System.NotImplementedException();
            }
        }
    }
}
