// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.NodeServices.HostingModels;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.NodeServices
{
    public class NodeServicesTest
    {
        [Fact]
        public async Task CanGetSuccessResult()
        {
            // Arrange
            var nodeServices = CreateNodeServices();

            // Act
            var result = await nodeServices.InvokeExportAsync<string>(
                ModulePath("testCases"),
                "getFixedString");

            // Assert
            Assert.Equal("test result", result);
        }

        [Fact]
        public async Task CanGetErrorResult()
        {
            // Arrange
            var nodeServices = CreateNodeServices();

            // Act/Assert
            var ex = await Assert.ThrowsAsync<NodeInvocationException>(() =>
                nodeServices.InvokeExportAsync<string>(
                    ModulePath("testCases"),
                    "raiseError"));
            Assert.StartsWith("This is an error from Node", ex.Message);
        }

        private static string ModulePath(string testModuleName)
            => $"../../../node/{testModuleName}";

        private static INodeServices CreateNodeServices(Action<NodeServicesOptions> configure = null)
        {
            var serviceProvider = new ServiceCollection().BuildServiceProvider();
            var options = new NodeServicesOptions(serviceProvider);
            configure?.Invoke(options);
            return NodeServicesFactory.CreateNodeServices(options);
        }
    }
}
