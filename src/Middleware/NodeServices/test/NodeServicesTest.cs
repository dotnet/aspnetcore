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

        [Fact]
        public async Task CanGetResultAsynchronously()
        {
            // Arrange
            var nodeServices = CreateNodeServices();

            // Act
            // All the invocations are async, but this test shows we're not reliant
            // on the response coming back immediately
            var result = await nodeServices.InvokeExportAsync<string>(
                ModulePath("testCases"),
                "getFixedStringWithDelay");

            // Assert
            Assert.Equal("delayed test result", result);
        }

        [Fact]
        public async Task CanPassParameters()
        {
            // Arrange
            var nodeServices = CreateNodeServices();

            // Act
            var result = await nodeServices.InvokeExportAsync<string>(
                ModulePath("testCases"),
                "echoSimpleParameters",
                "Hey",
                123);

            // Assert
            Assert.Equal("Param0: Hey; Param1: 123", result);
        }

        [Fact]
        public async Task CanPassParametersWithCamelCaseNameConversion()
        {
            // Arrange
            var nodeServices = CreateNodeServices();

            // Act
            var result = await nodeServices.InvokeExportAsync<string>(
                ModulePath("testCases"),
                "echoComplexParameters",
                new ComplexModel { StringProp = "Abc", IntProp = 123, BoolProp = true });

            // Assert
            Assert.Equal("Received: [{\"stringProp\":\"Abc\",\"intProp\":123,\"boolProp\":true}]", result);
        }

        [Fact]
        public async Task CanReceiveComplexResultWithPascalCaseNameConversion()
        {
            // Arrange
            var nodeServices = CreateNodeServices();

            // Act
            var result = await nodeServices.InvokeExportAsync<ComplexModel>(
                ModulePath("testCases"),
                "getComplexObject");

            // Assert
            Assert.Equal("Hi from Node", result.StringProp);
            Assert.Equal(456, result.IntProp);
            Assert.True(result.BoolProp);
        }

        [Fact]
        public async Task CanInvokeDefaultModuleExport()
        {
            // Arrange
            var nodeServices = CreateNodeServices();

            // Act
            var result = await nodeServices.InvokeAsync<string>(
                ModulePath("moduleWithDefaultExport"),
                "This is from .NET");

            // Assert
            Assert.Equal("Hello from the default export. You passed: This is from .NET", result);
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

        class ComplexModel
        {
            public string StringProp { get; set; }

            public int IntProp { get; set; }

            public bool BoolProp { get; set; }
        }
    }
}
