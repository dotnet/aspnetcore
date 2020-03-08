// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Testing;
using Microsoft.DotNet.OpenApi.Tests;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.DotNet.OpenApi.Add.Tests
{
    public class OpenApiAddURLTests : OpenApiTestBase
    {
        public OpenApiAddURLTests(ITestOutputHelper output) : base(output){ }

        [Fact]
        public async Task OpenApi_Add_Url_WithContentDisposition()
        {
            var project = CreateBasicProject(withOpenApi: false);

            var app = GetApplication();
            var run = app.Execute(new[] { "add", "url", FakeOpenApiUrl });

            AssertNoErrors(run);

            var expectedJsonName = "filename.json";

            // csproj contents
            using (var csprojStream = new FileInfo(project.Project.Path).OpenRead())
            using (var reader = new StreamReader(csprojStream))
            {
                var content = await reader.ReadToEndAsync();
                Assert.Contains("<PackageReference Include=\"NSwag.ApiDescription.Client\" Version=\"", content);
                Assert.Contains(
    $@"<OpenApiReference Include=""{expectedJsonName}"" SourceUrl=""{FakeOpenApiUrl}"" />", content);
            }

            var jsonFile = Path.Combine(_tempDir.Root, expectedJsonName);
            Assert.True(File.Exists(jsonFile));
            using (var jsonStream = new FileInfo(jsonFile).OpenRead())
            using (var reader = new StreamReader(jsonStream))
            {
                var content = await reader.ReadToEndAsync();
                Assert.Equal(Content, content);
            }
        }

        [Fact]
        public async Task OpenAPI_Add_Url_NoContentDisposition()
        {
            var project = CreateBasicProject(withOpenApi: false);
            var url = NoDispositionUrl;

            var app = GetApplication();
            var run = app.Execute(new[] { "add", "url", url});

            AssertNoErrors(run);

            var expectedJsonName = "nodisposition.yaml";

            // csproj contents
            using (var csprojStream = new FileInfo(project.Project.Path).OpenRead())
            using (var reader = new StreamReader(csprojStream))
            {
                var content = await reader.ReadToEndAsync();
                Assert.Contains("<PackageReference Include=\"NSwag.ApiDescription.Client\" Version=\"", content);
                Assert.Contains(
    $@"<OpenApiReference Include=""{expectedJsonName}"" SourceUrl=""{url}"" />", content);
            }

            var jsonFile = Path.Combine(_tempDir.Root, expectedJsonName);
            Assert.True(File.Exists(jsonFile));
            using (var jsonStream = new FileInfo(jsonFile).OpenRead())
            using (var reader = new StreamReader(jsonStream))
            {
                var content = await reader.ReadToEndAsync();
                Assert.Equal(Content, content);
            }
        }

        [Fact]
        public async Task OpenAPI_Add_Url_NoExtension_AssumesJson()
        {
            var project = CreateBasicProject(withOpenApi: false);
            var url = NoExtensionUrl;

            var app = GetApplication();
            var run = app.Execute(new[] { "add", "url", url });

            AssertNoErrors(run);

            var expectedJsonName = "filename.json";

            // csproj contents
            using (var csprojStream = new FileInfo(project.Project.Path).OpenRead())
            using (var reader = new StreamReader(csprojStream))
            {
                var content = await reader.ReadToEndAsync();
                Assert.Contains("<PackageReference Include=\"NSwag.ApiDescription.Client\" Version=\"", content);
                Assert.Contains(
    $@"<OpenApiReference Include=""{expectedJsonName}"" SourceUrl=""{url}"" />", content);
            }

            var jsonFile = Path.Combine(_tempDir.Root, expectedJsonName);
            Assert.True(File.Exists(jsonFile));
            using (var jsonStream = new FileInfo(jsonFile).OpenRead())
            using (var reader = new StreamReader(jsonStream))
            {
                var content = await reader.ReadToEndAsync();
                Assert.Equal(Content, content);
            }
        }

        [Fact]
        public async Task OpenApi_Add_Url_NoSegment()
        {
            var project = CreateBasicProject(withOpenApi: false);
            var url = NoSegmentUrl;

            var app = GetApplication();
            var run = app.Execute(new[] { "add", "url", url });

            AssertNoErrors(run);

            var expectedJsonName = "contoso.json";

            // csproj contents
            using (var csprojStream = new FileInfo(project.Project.Path).OpenRead())
            using (var reader = new StreamReader(csprojStream))
            {
                var content = await reader.ReadToEndAsync();
                Assert.Contains("<PackageReference Include=\"NSwag.ApiDescription.Client\" Version=\"", content);
                Assert.Contains(
    $@"<OpenApiReference Include=""{expectedJsonName}"" SourceUrl=""{url}"" />", content);
            }

            var jsonFile = Path.Combine(_tempDir.Root, expectedJsonName);
            Assert.True(File.Exists(jsonFile));
            using (var jsonStream = new FileInfo(jsonFile).OpenRead())
            using (var reader = new StreamReader(jsonStream))
            {
                var content = await reader.ReadToEndAsync();
                Assert.Equal(Content, content);
            }
        }

        [Fact]
        public async Task OpenApi_Add_Url()
        {
            var project = CreateBasicProject(withOpenApi: false);

            var app = GetApplication();
            var run = app.Execute(new[] { "add", "url", FakeOpenApiUrl });

            AssertNoErrors(run);

            var expectedJsonName = "filename.json";

            // csproj contents
            using (var csprojStream = new FileInfo(project.Project.Path).OpenRead())
            using (var reader = new StreamReader(csprojStream))
            {
                var content = await reader.ReadToEndAsync();
                Assert.Contains("<PackageReference Include=\"NSwag.ApiDescription.Client\" Version=\"", content);
                Assert.Contains(
    $@"<OpenApiReference Include=""{expectedJsonName}"" SourceUrl=""{FakeOpenApiUrl}"" />", content);
            }

            var jsonFile = Path.Combine(_tempDir.Root, expectedJsonName);
            Assert.True(File.Exists(jsonFile));
            using (var jsonStream = new FileInfo(jsonFile).OpenRead())
            using (var reader = new StreamReader(jsonStream))
            {
                var content = await reader.ReadToEndAsync();
                Assert.Equal(Content, content);
            }
        }

        [Fact]
        public async Task OpenApi_Add_Url_SameName_UniqueFile()
        {
            var project = CreateBasicProject(withOpenApi: false);

            var app = GetApplication();
            var run = app.Execute(new[] { "add", "url", FakeOpenApiUrl });

            AssertNoErrors(run);

            var firstExpectedJsonName = "filename.json";

            // csproj contents
            using (var csprojStream = new FileInfo(project.Project.Path).OpenRead())
            using (var reader = new StreamReader(csprojStream))
            {
                var content = await reader.ReadToEndAsync();
                Assert.Contains("<PackageReference Include=\"NSwag.ApiDescription.Client\" Version=\"", content);
                Assert.Contains(
    $@"<OpenApiReference Include=""{firstExpectedJsonName}"" SourceUrl=""{FakeOpenApiUrl}"" />", content);
            }

            var firstJsonFile = Path.Combine(_tempDir.Root, firstExpectedJsonName);
            Assert.True(File.Exists(firstJsonFile));
            using (var jsonStream = new FileInfo(firstJsonFile).OpenRead())
            using (var reader = new StreamReader(jsonStream))
            {
                var content = await reader.ReadToEndAsync();
                Assert.Equal(Content, content);
            }

            app = GetApplication();
            run = app.Execute(new[] { "add", "url", NoExtensionUrl });

            AssertNoErrors(run);

            var secondExpectedJsonName = "filename1.json";

            // csproj contents
            using (var csprojStream = new FileInfo(project.Project.Path).OpenRead())
            using (var reader = new StreamReader(csprojStream))
            {
                var content = await reader.ReadToEndAsync();
                Assert.Contains("<PackageReference Include=\"NSwag.ApiDescription.Client\" Version=\"", content);
                Assert.Contains(
    $@"<OpenApiReference Include=""{firstExpectedJsonName}"" SourceUrl=""{FakeOpenApiUrl}"" />", content);
                Assert.Contains(
    $@"<OpenApiReference Include=""{secondExpectedJsonName}"" SourceUrl=""{NoExtensionUrl}"" />", content);
            }

            var secondJsonFile = Path.Combine(_tempDir.Root, secondExpectedJsonName);
            Assert.True(File.Exists(secondJsonFile));
            using (var jsonStream = new FileInfo(secondJsonFile).OpenRead())
            using (var reader = new StreamReader(jsonStream))
            {
                var content = await reader.ReadToEndAsync();
                Assert.Equal(Content, content);
            }
        }

        [Fact]
        public async Task OpenApi_Add_Url_NSwagCSharp()
        {
            var project = CreateBasicProject(withOpenApi: false);

            var app = GetApplication();
            var run = app.Execute(new[] { "add", "url", FakeOpenApiUrl, "--code-generator", "NSwagCSharp" });

            AssertNoErrors(run);

            var expectedJsonName = "filename.json";

            // csproj contents
            using (var csprojStream = new FileInfo(project.Project.Path).OpenRead())
            using (var reader = new StreamReader(csprojStream))
            {
                var content = await reader.ReadToEndAsync();
                Assert.Contains("<PackageReference Include=\"NSwag.ApiDescription.Client\" Version=\"", content);
                Assert.Contains(
    $@"<OpenApiReference Include=""{expectedJsonName}"" SourceUrl=""{FakeOpenApiUrl}"" CodeGenerator=""NSwagCSharp"" />", content);
            }

            var resultFile = Path.Combine(_tempDir.Root, expectedJsonName);
            Assert.True(File.Exists(resultFile));
            using (var jsonStream = new FileInfo(resultFile).OpenRead())
            using (var reader = new StreamReader(jsonStream))
            {
                var content = await reader.ReadToEndAsync();
                Assert.Equal(Content, content);
            }
        }

        [Fact]
        public async Task OpenApi_Add_Url_NSwagTypeScript()
        {
            var project = CreateBasicProject(withOpenApi: false);

            var app = GetApplication();
            var run = app.Execute(new[] { "add", "url", FakeOpenApiUrl, "--code-generator", "NSwagTypeScript" });

            AssertNoErrors(run);

            var expectedJsonName = "filename.json";

            // csproj contents
            using (var csprojStream = new FileInfo(project.Project.Path).OpenRead())
            using (var reader = new StreamReader(csprojStream))
            {
                var content = await reader.ReadToEndAsync();
                Assert.Contains("<PackageReference Include=\"NSwag.ApiDescription.Client\" Version=\"", content);
                Assert.Contains(
    $@"<OpenApiReference Include=""{expectedJsonName}"" SourceUrl=""{FakeOpenApiUrl}"" CodeGenerator=""NSwagTypeScript"" />", content);
            }

            var resultFile = Path.Combine(_tempDir.Root, expectedJsonName);
            Assert.True(File.Exists(resultFile));
            using (var jsonStream = new FileInfo(resultFile).OpenRead())
            using (var reader = new StreamReader(jsonStream))
            {
                var content = await reader.ReadToEndAsync();
                Assert.Equal(Content, content);
            }
        }

        [Fact]
        public async Task OpenApi_Add_Url_OutputFile()
        {
            var project = CreateBasicProject(withOpenApi: false);

            var app = GetApplication();
            var run = app.Execute(new[] { "add", "url", FakeOpenApiUrl, "--output-file", Path.Combine("outputdir", "file.yaml") });

            AssertNoErrors(run);

            var expectedJsonName = Path.Combine("outputdir", "file.yaml");

            // csproj contents
            using (var csprojStream = new FileInfo(project.Project.Path).OpenRead())
            using (var reader = new StreamReader(csprojStream))
            {
                var content = await reader.ReadToEndAsync();
                Assert.Contains("<PackageReference Include=\"NSwag.ApiDescription.Client\" Version=\"", content);
                Assert.Contains(
    $@"<OpenApiReference Include=""{expectedJsonName}"" SourceUrl=""{FakeOpenApiUrl}"" />", content);
            }

            var resultFile = Path.Combine(_tempDir.Root, expectedJsonName);
            Assert.True(File.Exists(resultFile));
            using (var jsonStream = new FileInfo(resultFile).OpenRead())
            using (var reader = new StreamReader(jsonStream))
            {
                var content = await reader.ReadToEndAsync();
                Assert.Equal(Content, content);
            }
        }

        [Fact]
        public async Task OpenApi_Add_URL_FileAlreadyExists_Fail()
        {
            var project = CreateBasicProject(withOpenApi: false);

            var app = GetApplication();
            var outputFile = Path.Combine("outputdir", "file.yaml");
            var appExitCode = app.Execute(new[] { "add", "url", FakeOpenApiUrl, "--output-file", outputFile });

            AssertNoErrors(appExitCode);

            var expectedJsonName = Path.Combine("outputdir", "file.yaml");

            // csproj contents
            using (var csprojStream = new FileInfo(project.Project.Path).OpenRead())
            using (var reader = new StreamReader(csprojStream))
            {
                var content = await reader.ReadToEndAsync();
                Assert.Contains("<PackageReference Include=\"NSwag.ApiDescription.Client\" Version=\"", content);
                Assert.Contains(
    $@"<OpenApiReference Include=""{expectedJsonName}"" SourceUrl=""{FakeOpenApiUrl}"" />", content);
            }

            var resultFile = Path.Combine(_tempDir.Root, expectedJsonName);
            Assert.True(File.Exists(resultFile));
            using (var jsonStream = new FileInfo(resultFile).OpenRead())
            using (var reader = new StreamReader(jsonStream))
            {
                var content = await reader.ReadToEndAsync();
                Assert.Equal(Content, content);
            }

            // Second reference, same output
            app = GetApplication();
            appExitCode = app.Execute(new[] { "add", "url", DifferentUrl, "--output-file", outputFile});
            Assert.Equal(1, appExitCode);
            Assert.True(_error.ToString().Contains("Aborting to avoid conflicts."), $"Should have aborted to avoid conflicts");

            // csproj contents
            using (var csprojStream = new FileInfo(project.Project.Path).OpenRead())
            using (var reader = new StreamReader(csprojStream))
            {
                var content = await reader.ReadToEndAsync();
                Assert.Contains("<PackageReference Include=\"NSwag.ApiDescription.Client\" Version=\"", content);
                Assert.Contains(
    $@"<OpenApiReference Include=""{expectedJsonName}"" SourceUrl=""{FakeOpenApiUrl}"" />", content);
                Assert.DoesNotContain(
                    $@"<OpenApiReference Include=""{expectedJsonName}"" SourceUrl=""{DifferentUrl}"" CodeGenerator=""NSwagCSharp"" />", content);
            }

            using (var jsonStream = new FileInfo(resultFile).OpenRead())
            using (var reader = new StreamReader(jsonStream))
            {
                var content = await reader.ReadToEndAsync();
                Assert.Equal(Content, content);
            }
        }

        [Fact]
        public void OpenApi_Add_URL_MultipleTimes_OnlyOneReference()
        {
            var project = CreateBasicProject(withOpenApi: false);

            var app = GetApplication();
            var run = app.Execute(new[] { "add", "url", FakeOpenApiUrl });

            AssertNoErrors(run);

            app = GetApplication();
            run = app.Execute(new[] { "add", "url", "--output-file", "openapi.yaml", FakeOpenApiUrl });

            AssertNoErrors(run);

            // csproj contents
            var csproj = new FileInfo(project.Project.Path);
            using var csprojStream = csproj.OpenRead();
            using var reader = new StreamReader(csprojStream);
            var content = reader.ReadToEnd();
            var escapedPkgRef = Regex.Escape("<PackageReference Include=\"NSwag.ApiDescription.Client\" Version=\"");
            Assert.Single(Regex.Matches(content, escapedPkgRef));
            var escapedApiRef = Regex.Escape($"SourceUrl=\"{FakeOpenApiUrl}\"");
            Assert.Single(Regex.Matches(content, escapedApiRef));
        }

        [Fact]
        public async Task OpenAPi_Add_URL_InvalidUrl()
        {
            var project = CreateBasicProject(withOpenApi: false);

            var app = GetApplication(realHttp: true);
            var url = BrokenUrl;
            var run = app.Execute(new[] { "add", "url", url });

            Assert.Equal($"The given url returned 'NotFound', " +
    "indicating failure. The url might be wrong, or there might be a networking issue."+Environment.NewLine, _error.ToString());
            Assert.Equal(1, run);

            var expectedJsonName = "dingos.json";

            // csproj contents
            using (var csprojStream = new FileInfo(project.Project.Path).OpenRead())
            using (var reader = new StreamReader(csprojStream))
            {
                var content = await reader.ReadToEndAsync();
                Assert.DoesNotContain("<PackageReference Include=\"NSwag.ApiDescription.Client\" Version=\"", content);
                Assert.DoesNotContain($@"<OpenApiReference", content);
            }

            var jsonFile = Path.Combine(_tempDir.Root, expectedJsonName);
            Assert.False(File.Exists(jsonFile));
        }

        [Fact]
        public void OpenApi_Add_URL_ActualResponse()
        {
            var project = CreateBasicProject(withOpenApi: false);

            var app = GetApplication(realHttp: true);
            var url = ActualUrl;
            var run = app.Execute(new[] { "add", "url", url });

            AssertNoErrors(run);

            app = GetApplication(realHttp: true);
            run = app.Execute(new[] { "add", "url", url });

            AssertNoErrors(run);

            // csproj contents
            var csproj = new FileInfo(project.Project.Path);
            using var csprojStream = csproj.OpenRead();
            using var reader = new StreamReader(csprojStream);
            var content = reader.ReadToEnd();
            var escapedPkgRef = Regex.Escape("<PackageReference Include=\"NSwag.ApiDescription.Client\" Version=\"");
            Assert.Single(Regex.Matches(content, escapedPkgRef));
            var escapedApiRef = Regex.Escape($"SourceUrl=\"{url}\"");
            Assert.Single(Regex.Matches(content, escapedApiRef));
            Assert.Contains(
$@"<OpenApiReference Include=""api-with-examples.yaml"" SourceUrl=""{ActualUrl}"" />", content);
        }
    }
}
