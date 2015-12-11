// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Testing.xunit;
using Xunit;

namespace Microsoft.AspNet.TestHost
{
    public class RequestBuilderTests
    {
        // c.f. https://github.com/mono/mono/pull/1832
        [ConditionalFact]
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        public void AddRequestHeader()
        {
            var builder = new WebApplicationBuilder().Configure(app => { });
            var server = new TestServer(builder);
            server.CreateRequest("/")
                .AddHeader("Host", "MyHost:90")
                .And(request =>
                {
                    Assert.Equal("MyHost:90", request.Headers.Host.ToString());
                });
        }

        [Fact]
        public void AddContentHeaders()
        {
            var builder = new WebApplicationBuilder().Configure(app => { });
            var server = new TestServer(builder);
            server.CreateRequest("/")
                .AddHeader("Content-Type", "Test/Value")
                .And(request =>
                {
                    Assert.NotNull(request.Content);
                    Assert.Equal("Test/Value", request.Content.Headers.ContentType.ToString());
                });
        }
    }
}
