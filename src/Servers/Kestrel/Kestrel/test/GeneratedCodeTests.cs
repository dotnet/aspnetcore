// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.FunctionalTests
{
    public class GeneratedCodeTests
    {
        [Fact]
        public void GeneratedCodeIsUpToDate()
        {
            var repositoryRoot = typeof(GeneratedCodeTests).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>().First(f => string.Equals(f.Key, "RepositoryRoot", StringComparison.OrdinalIgnoreCase)).Value;

            var httpHeadersGeneratedPath = Path.Combine(repositoryRoot, "src/Servers/Kestrel/Core/src/Internal/Http/HttpHeaders.Generated.cs");
            var httpProtocolGeneratedPath = Path.Combine(repositoryRoot, "src/Servers/Kestrel/Core/src/Internal/Http/HttpProtocol.Generated.cs");
            var httpUtilitiesGeneratedPath = Path.Combine(repositoryRoot, "src/Servers/Kestrel/Core/src/Internal/Infrastructure/HttpUtilities.Generated.cs");
            var transportConnectionGeneratedPath = Path.Combine(repositoryRoot, "src/Servers/Kestrel/Transport.Abstractions/src/Internal/TransportConnection.Generated.cs");

            var testHttpHeadersGeneratedPath = Path.GetTempFileName();
            var testHttpProtocolGeneratedPath = Path.GetTempFileName();
            var testHttpUtilitiesGeneratedPath = Path.GetTempFileName();
            var testTransportConnectionGeneratedPath = Path.GetTempFileName();

            try
            {
                var currentHttpHeadersGenerated = File.ReadAllText(httpHeadersGeneratedPath);
                var currentHttpProtocolGenerated = File.ReadAllText(httpProtocolGeneratedPath);
                var currentHttpUtilitiesGenerated = File.ReadAllText(httpUtilitiesGeneratedPath);
                var currentTransportConnectionGenerated = File.ReadAllText(transportConnectionGeneratedPath);

                CodeGenerator.Program.Run(testHttpHeadersGeneratedPath, testHttpProtocolGeneratedPath, testHttpUtilitiesGeneratedPath, testTransportConnectionGeneratedPath);

                var testHttpHeadersGenerated = File.ReadAllText(testHttpHeadersGeneratedPath);
                var testHttpProtocolGenerated = File.ReadAllText(testHttpProtocolGeneratedPath);
                var testHttpUtilitiesGenerated = File.ReadAllText(testHttpUtilitiesGeneratedPath);
                var testTransportConnectionGenerated = File.ReadAllText(testTransportConnectionGeneratedPath);

                Assert.Equal(currentHttpHeadersGenerated, testHttpHeadersGenerated, ignoreLineEndingDifferences: true);
                Assert.Equal(currentHttpProtocolGenerated, testHttpProtocolGenerated, ignoreLineEndingDifferences: true);
                Assert.Equal(currentHttpUtilitiesGenerated, testHttpUtilitiesGenerated, ignoreLineEndingDifferences: true);
                Assert.Equal(currentTransportConnectionGenerated, testTransportConnectionGenerated, ignoreLineEndingDifferences: true);

            }
            finally
            {
                File.Delete(testHttpHeadersGeneratedPath);
                File.Delete(testHttpProtocolGeneratedPath);
                File.Delete(testHttpUtilitiesGeneratedPath);
                File.Delete(testTransportConnectionGeneratedPath);
            }
        }
    }
}
