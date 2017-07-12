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
            const string frameHeadersGeneratedPath = "../../../../../src/Kestrel.Core/Internal/Http/FrameHeaders.Generated.cs";
            const string frameGeneratedPath = "../../../../../src/Kestrel.Core/Internal/Http/Frame.Generated.cs";
            const string httpUtilitiesGeneratedPath = "../../../../../src/Kestrel.Core/Internal/Infrastructure/HttpUtilities.Generated.cs";

            var testFrameHeadersGeneratedPath = Path.GetTempFileName();
            var testFrameGeneratedPath = Path.GetTempFileName();
            var testHttpUtilitiesGeneratedPath = Path.GetTempFileName();

            try
            {
                var currentFrameHeadersGenerated = File.ReadAllText(frameHeadersGeneratedPath);
                var currentFrameGenerated = File.ReadAllText(frameGeneratedPath);
                var currentHttpUtilitiesGenerated = File.ReadAllText(httpUtilitiesGeneratedPath);

                CodeGenerator.Program.Run(testFrameHeadersGeneratedPath, testFrameGeneratedPath, testHttpUtilitiesGeneratedPath);

                var testFrameHeadersGenerated = File.ReadAllText(testFrameHeadersGeneratedPath);
                var testFrameGenerated = File.ReadAllText(testFrameGeneratedPath);
                var testHttpUtilitiesGenerated = File.ReadAllText(testHttpUtilitiesGeneratedPath);

                Assert.Equal(currentFrameHeadersGenerated, testFrameHeadersGenerated, ignoreLineEndingDifferences: true);
                Assert.Equal(currentFrameGenerated, testFrameGenerated, ignoreLineEndingDifferences: true);
                Assert.Equal(currentHttpUtilitiesGenerated, testHttpUtilitiesGenerated, ignoreLineEndingDifferences: true);

            }
            finally
            {
                File.Delete(testFrameHeadersGeneratedPath);
                File.Delete(testFrameGeneratedPath);
                File.Delete(testHttpUtilitiesGeneratedPath);
            }
        }
    }
}
#elif NET461
#else
#error Target framework needs to be updated
#endif
