// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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

        [Fact]
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
