// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Analyzer.Testing;
using Microsoft.Extensions.Internal;
using Xunit;

namespace Microsoft.AspNetCore.Components.Analyzers
{
    public class ComponentInternalUsageDiagnoticsAnalyzerTest : AnalyzerTestBase
    {
        public ComponentInternalUsageDiagnoticsAnalyzerTest()
        {
            Analyzer = new ComponentInternalUsageDiagnosticAnalyzer();
            Runner = new ComponentAnalyzerDiagnosticAnalyzerRunner(Analyzer);
        }

        private ComponentInternalUsageDiagnosticAnalyzer Analyzer { get; }
        private ComponentAnalyzerDiagnosticAnalyzerRunner Runner { get; }

        [Fact]
        public async Task InternalUsage_FindsUseOfRenderTreeFrameAsParameter()
        {
            // Arrange
            var source = Read("UsesRenderTreeFrameAsParameter");

            // Act
            var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);

            // Assert
            Assert.Collection(
                diagnostics,
                diagnostic =>
                {
                    Assert.Same(DiagnosticDescriptors.DoNotUseRenderTreeTypes, diagnostic.Descriptor);
                    AnalyzerAssert.DiagnosticLocation(source.DefaultMarkerLocation, diagnostic.Location);
                });
        }

        [Fact]
        public async Task InternalUsage_FindsUseOfRenderTreeType()
        {
            // Arrange
            var source = Read("UsesRenderTreeFrameTypeAsLocal");

            // Act
            var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);

            // Assert
            Assert.Collection(
                diagnostics,
                diagnostic =>
                {
                    Assert.Same(DiagnosticDescriptors.DoNotUseRenderTreeTypes, diagnostic.Descriptor);
                    AnalyzerAssert.DiagnosticLocation(source.DefaultMarkerLocation, diagnostic.Location);
                });
        }
    }
}
