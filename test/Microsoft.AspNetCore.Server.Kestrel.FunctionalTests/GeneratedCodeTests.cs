// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if NETCOREAPP1_1

using System.IO;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.FunctionalTests
{
    public class GeneratedCodeTests
    {
        [Fact]
        public void GeneratedCodeIsUpToDate()
        {
            const string frameHeadersGeneratedPath = "../../../../../src/Microsoft.AspNetCore.Server.Kestrel/Internal/Http/FrameHeaders.Generated.cs";
            const string frameGeneratedPath = "../../../../../src/Microsoft.AspNetCore.Server.Kestrel/Internal/Http/Frame.Generated.cs";

            var testFrameHeadersGeneratedPath = Path.GetTempFileName();
            var testFrameGeneratedPath = Path.GetTempFileName();

            try
            {
                var currentFrameHeadersGenerated = File.ReadAllText(frameHeadersGeneratedPath);
                var currentFrameGenerated = File.ReadAllText(frameGeneratedPath);

                CodeGenerator.Program.Run(testFrameHeadersGeneratedPath, testFrameGeneratedPath);

                var testFrameHeadersGenerated = File.ReadAllText(testFrameHeadersGeneratedPath);
                var testFrameGenerated = File.ReadAllText(testFrameGeneratedPath);

                Assert.Equal(currentFrameHeadersGenerated, testFrameHeadersGenerated, ignoreLineEndingDifferences: true);
                Assert.Equal(currentFrameGenerated, testFrameGenerated, ignoreLineEndingDifferences: true);
            }
            finally
            {
                File.Delete(testFrameHeadersGeneratedPath);
                File.Delete(testFrameGeneratedPath);
            }
        }
    }
}

#endif