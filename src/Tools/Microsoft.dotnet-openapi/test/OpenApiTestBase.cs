// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.DotNet.Openapi.Tools;
using Microsoft.Extensions.Tools.Internal;
using Xunit.Abstractions;

namespace Microsoft.DotNet.OpenApi.Tests
{
    public class OpenApiTestBase : IDisposable
    {
        protected readonly TemporaryDirectory _tempDir;
        protected readonly TextWriter _output = new StringWriter();
        protected readonly TextWriter _error = new StringWriter();
        protected readonly ITestOutputHelper _outputHelper;

        protected const string Content = @"{""x-generator"": ""NSwag""}";
        protected const string FakeOpenApiUrl = "https://contoso.com/openapi.json";
        protected const string DifferentUrl = "https://contoso.com/different.json";
        protected const string PackageUrl = "https://go.microsoft.com/fwlink/?linkid=2099561";
        protected const string DifferentUrlContent = @"
{
    ""x-generator"": ""NSwag""
}";
        protected const string PackageUrlContent = @"
{
  ""Version"" : ""1.0"",
  ""Packages""  :  {
    ""Microsoft.Azure.SignalR"": ""1.1.0-preview1-10442"",
    ""Grpc.AspNetCore.Server"": ""0.1.22-pre2"",
    ""Grpc.Net.ClientFactory"": ""0.1.22-pre2"",
    ""Google.Protobuf"": ""3.8.0"",
    ""Grpc.Tools"": ""1.22.0"",
    ""NSwag.ApiDescription.Client"": ""13.0.3"",
    ""Microsoft.Extensions.ApiDescription.Client"": ""0.3.0-preview7.19365.7"",
    ""Newtonsoft.Json"": ""12.0.2""
  }
}";

        public OpenApiTestBase(ITestOutputHelper output)
        {
            _tempDir = new TemporaryDirectory();
            _outputHelper = output;
        }

        public TemporaryNSwagProject CreateBasicProject(bool withOpenApi)
        {
            var nswagJsonFile = "openapi.json";
            var project = _tempDir
                .WithCSharpProject("testproj", sdk: "Microsoft.NET.Sdk.Web")
                .WithTargetFrameworks("netcoreapp3.0");
            var tmp = project.Dir();

            if (withOpenApi)
            {
                tmp = tmp.WithContentFile(nswagJsonFile);
            }

            tmp.WithContentFile("Startup.cs")
                .Create();

            return new TemporaryNSwagProject(project, nswagJsonFile);
        }

        internal Application GetApplication()
        {
            return new Application(
                _tempDir.Root, new TestHttpClientWrapper(DownloadMock()), _output, _error);
        }

        private IDictionary<string, string> DownloadMock()
        {
            return new Dictionary<string, string> {
                { FakeOpenApiUrl, Content },
                { DifferentUrl, DifferentUrlContent },
                { PackageUrl, PackageUrlContent }
            };
        }

        public void Dispose()
        {
            _outputHelper.WriteLine(_output.ToString());
            _tempDir.Dispose();
        }
    }

    public class TestHttpClientWrapper : IHttpClientWrapper
    {
        private readonly IDictionary<string, string> _results;

        public TestHttpClientWrapper(IDictionary<string, string> results)
        {
            _results = results;
        }

        public void Dispose()
        {
        }

        public Task<Stream> GetStreamAsync(string url)
        {
            byte[] byteArray = Encoding.ASCII.GetBytes(_results[url]);
            return Task.FromResult<Stream>(new MemoryStream(byteArray));
        }
    }

    public class TemporaryNSwagProject
    {
        public TemporaryNSwagProject(TemporaryCSharpProject project, string jsonFile)
        {
            Project = project;
            NSwagJsonFile = jsonFile;
        }

        public TemporaryCSharpProject Project { get; set; }
        public string NSwagJsonFile { get; set; }
    }
}
