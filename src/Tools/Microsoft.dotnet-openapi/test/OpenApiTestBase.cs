// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.DotNet.Openapi.Tools;
using Microsoft.Extensions.Tools.Internal;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.DotNet.OpenApi.Tests
{
    public class OpenApiTestBase : IDisposable
    {
        protected readonly TemporaryDirectory _tempDir;
        protected readonly TextWriter _output = new StringWriter();
        protected readonly TextWriter _error = new StringWriter();
        protected readonly ITestOutputHelper _outputHelper;
        protected const string TestTFM = "netcoreapp5.0";

        protected const string Content = @"{""x-generator"": ""NSwag""}";
        protected const string ActualUrl = "https://raw.githubusercontent.com/OAI/OpenAPI-Specification/master/examples/v3.0/api-with-examples.yaml";
        protected const string BrokenUrl = "https://www.microsoft.com/en-us/dingos.json";
        protected const string FakeOpenApiUrl = "https://contoso.com/openapi.json";
        protected const string NoDispositionUrl = "https://contoso.com/nodisposition.yaml";
        protected const string NoExtensionUrl = "https://contoso.com/noextension";
        protected const string NoSegmentUrl = "https://contoso.com";
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
                .WithTargetFrameworks(TestTFM);
            var tmp = project.Dir();

            if (withOpenApi)
            {
                tmp = tmp.WithContentFile(nswagJsonFile);
            }

            tmp.WithContentFile("Startup.cs")
                .Create();

            return new TemporaryNSwagProject(project, nswagJsonFile);
        }

        internal Application GetApplication(bool realHttp = false)
        {
            IHttpClientWrapper wrapper;
            if (realHttp)
            {
                wrapper = new HttpClientWrapper(new HttpClient());
            }
            else
            {
                wrapper = new TestHttpClientWrapper(DownloadMock());
            }
            return new Application(
                _tempDir.Root, wrapper, _output, _error);
        }

        private IDictionary<string, Tuple<string, ContentDispositionHeaderValue>> DownloadMock()
        {
            var noExtension = new ContentDispositionHeaderValue("attachment");
            noExtension.Parameters.Add(new NameValueHeaderValue("filename", "filename"));
            var extension = new ContentDispositionHeaderValue("attachment");
            extension.Parameters.Add(new NameValueHeaderValue("filename", "filename.json"));
            var justAttachments = new ContentDispositionHeaderValue("attachment");

            return new Dictionary<string, Tuple<string, ContentDispositionHeaderValue>> {
                { FakeOpenApiUrl, Tuple.Create(Content, extension)},
                { DifferentUrl, Tuple.Create<string, ContentDispositionHeaderValue>(DifferentUrlContent, null) },
                { PackageUrl, Tuple.Create<string, ContentDispositionHeaderValue>(PackageUrlContent, null) },
                { NoDispositionUrl, Tuple.Create<string, ContentDispositionHeaderValue>(Content, null) },
                { NoExtensionUrl, Tuple.Create(Content, noExtension) },
                { NoSegmentUrl, Tuple.Create(Content, justAttachments) }
            };
        }

        protected void AssertNoErrors(int appExitCode)
        {
            Assert.True(string.IsNullOrEmpty(_error.ToString()), $"Threw error: {_error.ToString()}");
            Assert.Equal(0, appExitCode);
        }

        public void Dispose()
        {
            _outputHelper.WriteLine(_output.ToString());
            _tempDir.Dispose();
        }
    }

    public class TestHttpClientWrapper : IHttpClientWrapper
    {
        private readonly IDictionary<string, Tuple<string, ContentDispositionHeaderValue>> _results;

        public TestHttpClientWrapper(IDictionary<string, Tuple<string, ContentDispositionHeaderValue>> results)
        {
            _results = results;
        }

        public void Dispose()
        {
        }

        public Task<IHttpResponseMessageWrapper> GetResponseAsync(string url)
        {
            var result = _results[url];
            byte[] byteArray = Encoding.ASCII.GetBytes(result.Item1);
            var stream = new MemoryStream(byteArray);

            return Task.FromResult<IHttpResponseMessageWrapper>(new TestHttpResponseMessageWrapper(stream, result.Item2));
        }
    }

    public class TestHttpResponseMessageWrapper : IHttpResponseMessageWrapper
    {
        public Task<Stream> Stream { get; }

        public HttpStatusCode StatusCode { get; } = HttpStatusCode.OK;

        public bool IsSuccessCode()
        {
            return true;
        }

        private readonly ContentDispositionHeaderValue _contentDisposition;

        public TestHttpResponseMessageWrapper(
            MemoryStream stream,
            ContentDispositionHeaderValue header)
        {
            Stream = Task.FromResult<Stream>(stream);
            _contentDisposition = header;
        }

        public ContentDispositionHeaderValue ContentDisposition()
        {
            return _contentDisposition;
        }

        public void Dispose()
        {
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
