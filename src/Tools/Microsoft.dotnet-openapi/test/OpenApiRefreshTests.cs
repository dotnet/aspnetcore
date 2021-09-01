// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.OpenApi.Tests;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.DotNet.OpenApi.Refresh.Tests
{
    public class OpenApiRefreshTests : OpenApiTestBase
    {
        public OpenApiRefreshTests(ITestOutputHelper output) : base(output) { }

        [Fact(Skip = "https://github.com/dotnet/aspnetcore/issues/32686")]
        public async Task OpenApi_Refresh_Basic()
        {
            CreateBasicProject(withOpenApi: false);

            var app = GetApplication();
            var run = app.Execute(new[] { "add", "url", FakeOpenApiUrl });

            AssertNoErrors(run);

            var expectedJsonPath = Path.Combine(_tempDir.Root, "filename.json");
            var json = await File.ReadAllTextAsync(expectedJsonPath);
            json += "trash";
            await File.WriteAllTextAsync(expectedJsonPath, json);

            var firstWriteTime = File.GetLastWriteTime(expectedJsonPath);

            Thread.Sleep(TimeSpan.FromSeconds(1));

            app = GetApplication();
            run = app.Execute(new[] { "refresh", FakeOpenApiUrl });

            AssertNoErrors(run);

            var secondWriteTime = File.GetLastWriteTime(expectedJsonPath);
            Assert.True(firstWriteTime < secondWriteTime, $"File wasn't updated! {firstWriteTime} {secondWriteTime}");
        }
    }
}
