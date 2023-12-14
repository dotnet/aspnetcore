// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Microsoft.AspNetCore.InternalTesting;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Server.Kestrel.FunctionalTests;

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
        var transportMultiplexedConnectionGeneratedPath = Path.Combine(AppContext.BaseDirectory, "shared", "GeneratedContent", "TransportMultiplexedConnection.Generated.cs");
        var transportConnectionGeneratedPath = Path.Combine(AppContext.BaseDirectory, "shared", "GeneratedContent", "TransportConnection.Generated.cs");

        var testHttpHeadersGeneratedPath = Path.GetTempFileName();
        var testHttpProtocolGeneratedPath = Path.GetTempFileName();
        var testHttpUtilitiesGeneratedPath = Path.GetTempFileName();
        var testTransportMultiplexedConnectionGeneratedPath = Path.GetTempFileName();
        var testTransportConnectionGeneratedPath = Path.GetTempFileName();

        try
        {
            var currentHttpHeadersGenerated = File.ReadAllText(httpHeadersGeneratedPath);
            var currentHttpProtocolGenerated = File.ReadAllText(httpProtocolGeneratedPath);
            var currentHttpUtilitiesGenerated = File.ReadAllText(httpUtilitiesGeneratedPath);
            var currentTransportConnectionBaseGenerated = File.ReadAllText(transportMultiplexedConnectionGeneratedPath);
            var currentTransportConnectionGenerated = File.ReadAllText(transportConnectionGeneratedPath);

            CodeGenerator.Program.Run(testHttpHeadersGeneratedPath,
                testHttpProtocolGeneratedPath,
                testHttpUtilitiesGeneratedPath,
                testTransportMultiplexedConnectionGeneratedPath,
                testTransportConnectionGeneratedPath);

            var testHttpHeadersGenerated = File.ReadAllText(testHttpHeadersGeneratedPath);
            var testHttpProtocolGenerated = File.ReadAllText(testHttpProtocolGeneratedPath);
            var testHttpUtilitiesGenerated = File.ReadAllText(testHttpUtilitiesGeneratedPath);
            var testTransportMultiplxedConnectionGenerated = File.ReadAllText(testTransportMultiplexedConnectionGeneratedPath);
            var testTransportConnectionGenerated = File.ReadAllText(testTransportConnectionGeneratedPath);

            AssertFileContentEqual(currentHttpHeadersGenerated, testHttpHeadersGenerated, "HTTP headers");
            AssertFileContentEqual(currentHttpProtocolGenerated, testHttpProtocolGenerated, "HTTP protocol");
            AssertFileContentEqual(currentHttpUtilitiesGenerated, testHttpUtilitiesGenerated, "HTTP utilities");
            AssertFileContentEqual(currentTransportConnectionBaseGenerated, testTransportMultiplxedConnectionGenerated, "TransportConnectionBase");
            AssertFileContentEqual(currentTransportConnectionGenerated, testTransportConnectionGenerated, "TransportConnection");
        }
        finally
        {
            File.Delete(testHttpHeadersGeneratedPath);
            File.Delete(testHttpProtocolGeneratedPath);
            File.Delete(testHttpUtilitiesGeneratedPath);
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
