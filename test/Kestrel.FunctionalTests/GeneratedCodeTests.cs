// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if NETCOREAPP2_0
using System.IO;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.FunctionalTests
{
    public class GeneratedCodeTests
    {
        [Fact]
        public void GeneratedCodeIsUpToDate()
        {
            const string httpHeadersGeneratedPath = "../../../../../src/Kestrel.Core/Internal/Http/HttpHeaders.Generated.cs";
            const string httpProtocolGeneratedPath = "../../../../../src/Kestrel.Core/Internal/Http/HttpProtocol.Generated.cs";
            const string httpUtilitiesGeneratedPath = "../../../../../src/Kestrel.Core/Internal/Infrastructure/HttpUtilities.Generated.cs";

            var testHttpHeadersGeneratedPath = Path.GetTempFileName();
            var testHttpProtocolGeneratedPath = Path.GetTempFileName();
            var testHttpUtilitiesGeneratedPath = Path.GetTempFileName();

            try
            {
                var currentHttpHeadersGenerated = File.ReadAllText(httpHeadersGeneratedPath);
                var currentHttpProtocolGenerated = File.ReadAllText(httpProtocolGeneratedPath);
                var currentHttpUtilitiesGenerated = File.ReadAllText(httpUtilitiesGeneratedPath);

                CodeGenerator.Program.Run(testHttpHeadersGeneratedPath, testHttpProtocolGeneratedPath, testHttpUtilitiesGeneratedPath);

                var testHttpHeadersGenerated = File.ReadAllText(testHttpHeadersGeneratedPath);
                var testHttpProtocolGenerated = File.ReadAllText(testHttpProtocolGeneratedPath);
                var testHttpUtilitiesGenerated = File.ReadAllText(testHttpUtilitiesGeneratedPath);

                Assert.Equal(currentHttpHeadersGenerated, testHttpHeadersGenerated, ignoreLineEndingDifferences: true);
                Assert.Equal(currentHttpProtocolGenerated, testHttpProtocolGenerated, ignoreLineEndingDifferences: true);
                Assert.Equal(currentHttpUtilitiesGenerated, testHttpUtilitiesGenerated, ignoreLineEndingDifferences: true);

            }
            finally
            {
                File.Delete(testHttpHeadersGeneratedPath);
                File.Delete(testHttpProtocolGeneratedPath);
                File.Delete(testHttpUtilitiesGeneratedPath);
            }
        }
    }
}
#elif NET461
#else
#error Target framework needs to be updated
#endif
