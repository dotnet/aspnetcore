// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.AspNetCore.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Server.Kestrel.FunctionalTests
{
    public class GeneratedCodeTests
    {
        private readonly ITestOutputHelper _output;

        public GeneratedCodeTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [ConditionalFact]
        public void GeneratedCodeIsUpToDate()
        {
            var httpHeadersGeneratedPath = Path.Combine(AppContext.BaseDirectory, "shared", "GeneratedContent", "HttpHeaders.Generated.cs");
            var httpProtocolGeneratedPath = Path.Combine(AppContext.BaseDirectory, "shared", "GeneratedContent", "HttpProtocol.Generated.cs");
            var httpUtilitiesGeneratedPath = Path.Combine(AppContext.BaseDirectory, "shared", "GeneratedContent", "HttpUtilities.Generated.cs");
            var http2ConnectionGeneratedPath = Path.Combine(AppContext.BaseDirectory, "shared", "GeneratedContent", "Http2Connection.Generated.cs");
            var transportMultiplexedConnectionGeneratedPath = Path.Combine(AppContext.BaseDirectory, "shared", "GeneratedContent", "TransportMultiplexedConnection.Generated.cs");
            var transportConnectionGeneratedPath = Path.Combine(AppContext.BaseDirectory, "shared", "GeneratedContent", "TransportConnection.Generated.cs");

            var testHttpHeadersGeneratedPath = Path.GetTempFileName();
            var testHttpProtocolGeneratedPath = Path.GetTempFileName();
            var testHttpUtilitiesGeneratedPath = Path.GetTempFileName();
            var testHttp2ConnectionGeneratedPath = Path.GetTempFileName();
            var testTransportMultiplexedConnectionGeneratedPath = Path.GetTempFileName();
            var testTransportConnectionGeneratedPath = Path.GetTempFileName();

            try
            {
                var currentHttpHeadersGenerated = File.ReadAllText(httpHeadersGeneratedPath);
                var currentHttpProtocolGenerated = File.ReadAllText(httpProtocolGeneratedPath);
                var currentHttpUtilitiesGenerated = File.ReadAllText(httpUtilitiesGeneratedPath);
                var currentHttp2ConnectionGenerated = File.ReadAllText(http2ConnectionGeneratedPath);
                var currentTransportConnectionBaseGenerated = File.ReadAllText(transportMultiplexedConnectionGeneratedPath);
                var currentTransportConnectionGenerated = File.ReadAllText(transportConnectionGeneratedPath);

                CodeGenerator.Program.Run(testHttpHeadersGeneratedPath,
                    testHttpProtocolGeneratedPath,
                    testHttpUtilitiesGeneratedPath,
                    testHttp2ConnectionGeneratedPath,
                    testTransportMultiplexedConnectionGeneratedPath,
                    testTransportConnectionGeneratedPath);

                var testHttpHeadersGenerated = File.ReadAllText(testHttpHeadersGeneratedPath);
                var testHttpProtocolGenerated = File.ReadAllText(testHttpProtocolGeneratedPath);
                var testHttpUtilitiesGenerated = File.ReadAllText(testHttpUtilitiesGeneratedPath);
                var testHttp2ConnectionGenerated = File.ReadAllText(testHttp2ConnectionGeneratedPath);
                var testTransportMultiplxedConnectionGenerated = File.ReadAllText(testTransportMultiplexedConnectionGeneratedPath);
                var testTransportConnectionGenerated = File.ReadAllText(testTransportConnectionGeneratedPath);

                AssertFileContentEqual(currentHttpHeadersGenerated, testHttpHeadersGenerated, "HTTP headers");
                AssertFileContentEqual(currentHttpProtocolGenerated, testHttpProtocolGenerated, "HTTP protocol");
                AssertFileContentEqual(currentHttpUtilitiesGenerated, testHttpUtilitiesGenerated, "HTTP utilities");
                AssertFileContentEqual(currentHttp2ConnectionGenerated, testHttp2ConnectionGenerated, "HTTP2 connection");
                AssertFileContentEqual(currentTransportConnectionBaseGenerated, testTransportMultiplxedConnectionGenerated, "TransportConnectionBase");
                AssertFileContentEqual(currentTransportConnectionGenerated, testTransportConnectionGenerated, "TransportConnection");
            }
            finally
            {
                File.Delete(testHttpHeadersGeneratedPath);
                File.Delete(testHttpProtocolGeneratedPath);
                File.Delete(testHttpUtilitiesGeneratedPath);
                File.Delete(testHttp2ConnectionGeneratedPath);
                File.Delete(testTransportMultiplexedConnectionGeneratedPath);
                File.Delete(testTransportConnectionGeneratedPath);
            }
        }

        private void AssertFileContentEqual(string expected, string actual, string type)
        {
            try
            {
                Assert.Equal(expected.Trim(), actual.Trim(), ignoreLineEndingDifferences: true);
            }
            catch (Exception)
            {
                _output.WriteLine($"Error when comparing {type}.");
                _output.WriteLine("Expected:");
                _output.WriteLine(expected);
                _output.WriteLine("Actual:");
                _output.WriteLine(actual);
                throw;
            }
        }
    }
}
