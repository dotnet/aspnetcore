// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Blazor.Hosting;
using Microsoft.AspNetCore.Components.Builder;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.AspNetCore.Components.Hosting
{
    public class ConventionBasedStartupTest
    {
        [Fact]
        public void ConventionBasedStartup_GetConfigureServicesMethod_FindsConfigureServices()
        {
            // Arrange
            var startup = new ConventionBasedStartup(new MyStartup1());

            // Act
            var method = startup.GetConfigureServicesMethod();

            // Assert
            Assert.Equal(typeof(IServiceCollection), method.GetParameters()[0].ParameterType);
        }

        private class MyStartup1
        {
            public void ConfigureServices(IServiceCollection services)
            {
            }

            // Ignored
            public void ConfigureServices(DateTime x)
            {

            }

            // Ignored
            private void ConfigureServices(int x)
            {
            }

            // Ignored
            public static void ConfigureServices(string x)
            {
            }
        }

        [Fact]
        public void ConventionBasedStartup_GetConfigureServicesMethod_NoMethodFound()
        {
            // Arrange
            var startup = new ConventionBasedStartup(new MyStartup2());

            // Act
            var method = startup.GetConfigureServicesMethod();

            // Assert
            Assert.Null(method);
        }

        private class MyStartup2
        {
        }

        [Fact]
        public void ConventionBasedStartup_ConfigureServices_CallsMethod()
        {
            // Arrange
            var startup = new ConventionBasedStartup(new MyStartup3());
            var services = new ServiceCollection();

            // Act
            startup.ConfigureServices(services);

            // Assert
            Assert.NotEmpty(services);
        }

        private class MyStartup3
        {
            public void ConfigureServices(IServiceCollection services)
            {
                services.AddSingleton("foo");
            }
        }

        [Fact]
        public void ConventionBasedStartup_ConfigureServices_NoMethodFound()
        {
            // Arrange
            var startup = new ConventionBasedStartup(new MyStartup4());
            var services = new ServiceCollection();

            // Act
            startup.ConfigureServices(services);

            // Assert
            Assert.Empty(services);
        }

        private class MyStartup4
        {
        }

        [Fact]
        public void ConventionBasedStartup_GetConfigureMethod_FindsConfigure()
        {
            // Arrange
            var startup = new ConventionBasedStartup(new MyStartup5());

            // Act
            var method = startup.GetConfigureMethod();

            // Assert
            Assert.Empty(method.GetParameters());
        }

        private class MyStartup5
        {
            public void Configure()
            {
            }

            // Ignored
            private void Configure(int x)
            {
            }

            // Ignored
            public static void Configure(string x)
            {
            }
        }

        [Fact]
        public void ConventionBasedStartup_GetConfigureMethod_NoMethodFoundThrows()
        {
            // Arrange
            var startup = new ConventionBasedStartup(new MyStartup6());

            // Act
            var ex = Assert.Throws<InvalidOperationException>(() => startup.GetConfigureMethod());

            // Assert
            Assert.Equal("The startup class must define a 'Configure' method.", ex.Message);
        }

        private class MyStartup6
        {
        }

        [Fact]
        public void ConventionBasedStartup_GetConfigureMethod_OverloadedThrows()
        {
            // Arrange
            var startup = new ConventionBasedStartup(new MyStartup7());

            // Act
            var ex = Assert.Throws<InvalidOperationException>(() => startup.GetConfigureMethod());

            // Assert
            Assert.Equal("Overloading the 'Configure' method is not supported.", ex.Message);
        }

        private class MyStartup7
        {
            public void Configure()
            {
            }

            public void Configure(string x)
            {
            }
        }

        [Fact]
        public void ConventionBasedStartup_Configure()
        {
            // Arrange
            var instance = new MyStartup8();
            var startup = new ConventionBasedStartup(instance);

            var services = new ServiceCollection().AddSingleton("foo").BuildServiceProvider();
            var builder = new WebAssemblyBlazorApplicationBuilder(services);

            // Act
            startup.Configure(builder, services);

            // Assert
            Assert.Collection(
                instance.Arguments,
                a => Assert.Same(builder, a),
                a => Assert.Equal("foo", a));
        }

        private class MyStartup8
        {
            public List<object> Arguments { get; } = new List<object>();

            public void Configure(IBlazorApplicationBuilder app, string foo)
            {
                Arguments.Add(app);
                Arguments.Add(foo);
            }
        }
    }
}
