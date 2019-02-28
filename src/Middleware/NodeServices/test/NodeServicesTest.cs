// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.NodeServices
{
    public class NodeServicesTest
    {
        [Fact]
        public async Task CanInvokeExportWithNoArgs()
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
