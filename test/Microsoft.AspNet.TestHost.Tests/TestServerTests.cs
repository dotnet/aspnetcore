// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.DependencyInjection.Fallback;
using Microsoft.Net.Runtime;
using Xunit;

namespace Microsoft.AspNet.TestHost.Tests
{
    public class TestServerTests
    {
        [Fact]
        public void CreateWithDelegate()
        {
            // Arrange
            var services = new ServiceCollection()
                .AddSingleton<IApplicationEnvironment, TestApplicationEnvironment>()
                .BuildServiceProvider();

            // Act & Assert
            Assert.DoesNotThrow(() => TestServer.Create(services, app => { }));
        }

        [Fact]
        public async Task CreateWithGeneric()
        {
            // Arrange
            var services = new ServiceCollection()
                .AddSingleton<IApplicationEnvironment, TestApplicationEnvironment>()
                .BuildServiceProvider();

            var server = TestServer.Create<Startup>(services);
            var client = server.Handler;

            // Act
            var response = await client.GetAsync("http://any");

            // Assert
            Assert.Equal("Startup", new StreamReader(response.Body).ReadToEnd());
        }

        [Fact]
        public void ThrowsIfNoApplicationEnvironmentIsRegisteredWithTheProvider()
        {
            // Arrange
            var services = new ServiceCollection()
                .BuildServiceProvider();

            // Act & Assert
            Assert.Throws<ArgumentException>(
                "serviceProvider",
                () => TestServer.Create<Startup>(services));
        }

        public class Startup
        {
            public void Configuration(IBuilder builder)
            {
                builder.Run(ctx => ctx.Response.WriteAsync("Startup"));
            }
        }

        public class AnotherStartup
        {
            public void Configuration(IBuilder builder)
            {
                builder.Run(ctx => ctx.Response.WriteAsync("Another Startup"));
            }
        }
    }
}
