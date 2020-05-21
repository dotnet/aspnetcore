// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Testing;
using Microsoft.AspNetCore.NodeServices.HostingModels;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.AspNetCore.NodeServices
{
    [SkipOnHelix("https://github.com/dotnet/aspnetcore/issues/22084", Queues = "Windows.10.Arm64;Windows.10.Arm64.Open")]
    [Obsolete("Use Microsoft.AspNetCore.SpaServices.Extensions")]
    public class NodeServicesTest : IDisposable
    {
        private readonly INodeServices _nodeServices;

        public NodeServicesTest()
        {
            // In typical ASP.NET Core applications, INodeServices is made available
            // through DI using services.AddNodeServices(). But for these tests we
            // create our own INodeServices instance manually, since the tests are
            // not about DI (and we might want different config for each test).
            var serviceProvider = new ServiceCollection().BuildServiceProvider();
            var options = new NodeServicesOptions(serviceProvider);
            _nodeServices = NodeServicesFactory.CreateNodeServices(options);
        }

        [ConditionalFact]
        public async Task CanGetSuccessResult()
        {
            // Act
            var result = await _nodeServices.InvokeExportAsync<string>(
                ModulePath("testCases"),
                "getFixedString");

            // Assert
            Assert.Equal("test result", result);
        }

        [ConditionalFact]
        public async Task CanGetErrorResult()
        {
            // Act/Assert
            var ex = await Assert.ThrowsAsync<NodeInvocationException>(() =>
                _nodeServices.InvokeExportAsync<string>(
                    ModulePath("testCases"),
                    "raiseError"));
            Assert.StartsWith("This is an error from Node", ex.Message);
        }

        [ConditionalFact]
        public async Task CanGetResultAsynchronously()
        {
            // Act
            // All the invocations are async, but this test shows we're not reliant
            // on the response coming back immediately
            var result = await _nodeServices.InvokeExportAsync<string>(
                ModulePath("testCases"),
                "getFixedStringWithDelay");

            // Assert
            Assert.Equal("delayed test result", result);
        }

        [ConditionalFact]
        public async Task CanPassParameters()
        {
            // Act
            var result = await _nodeServices.InvokeExportAsync<string>(
                ModulePath("testCases"),
                "echoSimpleParameters",
                "Hey",
                123);

            // Assert
            Assert.Equal("Param0: Hey; Param1: 123", result);
        }

        [ConditionalFact]
        public async Task CanPassParametersWithCamelCaseNameConversion()
        {
            // Act
            var result = await _nodeServices.InvokeExportAsync<string>(
                ModulePath("testCases"),
                "echoComplexParameters",
                new ComplexModel { StringProp = "Abc", IntProp = 123, BoolProp = true });

            // Assert
            Assert.Equal("Received: [{\"stringProp\":\"Abc\",\"intProp\":123,\"boolProp\":true}]", result);
        }

        [ConditionalFact]
        public async Task CanReceiveComplexResultWithPascalCaseNameConversion()
        {
            // Act
            var result = await _nodeServices.InvokeExportAsync<ComplexModel>(
                ModulePath("testCases"),
                "getComplexObject");

            // Assert
            Assert.Equal("Hi from Node", result.StringProp);
            Assert.Equal(456, result.IntProp);
            Assert.True(result.BoolProp);
        }

        [ConditionalFact]
        public async Task CanInvokeDefaultModuleExport()
        {
            // Act
            var result = await _nodeServices.InvokeAsync<string>(
                ModulePath("moduleWithDefaultExport"),
                "This is from .NET");

            // Assert
            Assert.Equal("Hello from the default export. You passed: This is from .NET", result);
        }

        private static string ModulePath(string testModuleName)
            => Path.Combine(AppContext.BaseDirectory, "js", testModuleName);

        public void Dispose()
        {
            _nodeServices.Dispose();
        }

        class ComplexModel
        {
            public string StringProp { get; set; }

            public int IntProp { get; set; }

            public bool BoolProp { get; set; }
        }
    }
}
