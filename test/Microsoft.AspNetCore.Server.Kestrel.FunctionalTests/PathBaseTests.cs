// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.FunctionalTests
{
    public class PathBaseTests
    {
        [Theory]
        [InlineData("/base", "/base", "/base", "")]
        [InlineData("/base", "/base/", "/base", "/")]
        [InlineData("/base", "/base/something", "/base", "/something")]
        [InlineData("/base", "/base/something/", "/base", "/something/")]
        [InlineData("/base/more", "/base/more", "/base/more", "")]
        [InlineData("/base/more", "/base/more/something", "/base/more", "/something")]
        [InlineData("/base/more", "/base/more/something/", "/base/more", "/something/")]
        public Task RequestPathBaseIsServerPathBase(string registerPathBase, string requestPath, string expectedPathBase, string expectedPath)
        {
            return TestPathBase(registerPathBase, requestPath, expectedPathBase, expectedPath);
        }

        [Theory]
        [InlineData("", "/", "", "/")]
        [InlineData("", "/something", "", "/something")]
        [InlineData("/", "/", "", "/")]
        [InlineData("/base", "/", "", "/")]
        [InlineData("/base", "/something", "", "/something")]
        [InlineData("/base", "/baseandsomething", "", "/baseandsomething")]
        [InlineData("/base", "/ba", "", "/ba")]
        [InlineData("/base", "/ba/se", "", "/ba/se")]
        public Task DefaultPathBaseIsEmpty(string registerPathBase, string requestPath, string expectedPathBase, string expectedPath)
        {
            return TestPathBase(registerPathBase, requestPath, expectedPathBase, expectedPath);
        }

        [Theory]
        [InlineData("", "/", "", "/")]
        [InlineData("/", "/", "", "/")]
        [InlineData("/base", "/base/", "/base", "/")]
        [InlineData("/base/", "/base", "/base", "")]
        [InlineData("/base/", "/base/", "/base", "/")]
        public Task PathBaseNeverEndsWithSlash(string registerPathBase, string requestPath, string expectedPathBase, string expectedPath)
        {
            return TestPathBase(registerPathBase, requestPath, expectedPathBase, expectedPath);
        }

        [Fact]
        public Task PathBaseAndPathPreserveRequestCasing()
        {
            return TestPathBase("/base", "/Base/Something", "/Base", "/Something");
        }

        [Fact]
        public Task PathBaseCanHaveUTF8Characters()
        {
            return TestPathBase("/b♫se", "/b♫se/something", "/b♫se", "/something");
        }

        private async Task TestPathBase(string registerPathBase, string requestPath, string expectedPathBase, string expectedPath)
        {
            var builder = new WebHostBuilder()
                .UseKestrel()
                .UseUrls($"http://127.0.0.1:0{registerPathBase}")
                .Configure(app =>
                {
                    app.Run(async context =>
                    {
                        await context.Response.WriteAsync(JsonConvert.SerializeObject(new
                        {
                            PathBase = context.Request.PathBase.Value,
                            Path = context.Request.Path.Value
                        }));
                    });
                });

            using (var host = builder.Build())
            {
                host.Start();

                using (var client = new HttpClient())
                {
                    var response = await client.GetAsync($"http://localhost:{host.GetPort()}{requestPath}");
                    response.EnsureSuccessStatusCode();

                    var responseText = await response.Content.ReadAsStringAsync();
                    Assert.NotEmpty(responseText);

                    var pathFacts = JsonConvert.DeserializeObject<JObject>(responseText);
                    Assert.Equal(expectedPathBase, pathFacts["PathBase"].Value<string>());
                    Assert.Equal(expectedPath, pathFacts["Path"].Value<string>());
                }
            }
        }
    }
}
