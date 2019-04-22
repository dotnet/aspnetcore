// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
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
            AnnounceTestStart();

            return new Application(
                _tempDir.Root,
                DownloadMock, _output, _error);
        }

        private void AnnounceTestStart()
        {
            var type = _outputHelper.GetType();
            var testMember = type.GetField("test", BindingFlags.Instance | BindingFlags.NonPublic);
            var test = (ITest)testMember.GetValue(_outputHelper);
            _outputHelper.WriteLine($"Starting test '{test.DisplayName}'");
        }

        private Task<Stream> DownloadMock(string url)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);

            _outputHelper.WriteLine($"Content to write: {Content}");
            writer.Write(Content);
            writer.Flush();
            stream.Position = 0;

            return Task.FromResult((Stream)stream);
        }

        public void Dispose()
        {
            _outputHelper.WriteLine(_output.ToString());
            _tempDir.Dispose();
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
