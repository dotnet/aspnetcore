// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Analyzer.Testing;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Api.Analyzers
{
    public class AddResponseTypeAttributeCodeFixProviderIntegrationTest
    {
        private MvcDiagnosticAnalyzerRunner AnalyzerRunner { get; } = new MvcDiagnosticAnalyzerRunner(new ApiConventionAnalyzer());

        private CodeFixRunner CodeFixRunner { get; } = new IgnoreCS1701WarningCodeFixRunner();

        [Fact]
        public Task CodeFixAddsStatusCodes() => RunTest();

        [Fact]
        public Task CodeFixAddsMissingStatusCodes() => RunTest();

        [Fact]
        public Task CodeFixAddsMissingStatusCodesAndTypes() => RunTest();

        [Fact]
        public Task CodeFixWithConventionAddsMissingStatusCodes() => RunTest();

        [Fact]
        public Task CodeFixWithConventionMethodAddsMissingStatusCodes() => RunTest();

        [Fact]
        public Task CodeFixAddsSuccessStatusCode() => RunTest();

        [Fact]
        public Task CodeFixAddsFullyQualifiedProducesResponseType() => RunTest();

        [Fact]
        public Task CodeFixAddsNumericLiteralForNonExistingStatusCodeConstants() => RunTest();

        [Fact]
        public Task CodeFixAddsResponseTypeWhenDifferentFromErrorType() => RunTest();

        [Fact]
        public Task CodeFixAddsStatusCodesFromMethodParameters() => RunTest();

        [Fact]
        public Task CodeFixAddsStatusCodesFromConstructorParameters() => RunTest();

        [Fact]
        public Task CodeFixAddsStatusCodesFromObjectInitializer() => RunTest();

        [Fact]
        public Task CodeFixWorksWhenMultipleIdenticalStatusCodesAreInError() => RunTest();

        private async Task RunTest([CallerMemberName] string testMethod = "")
        {
            // Arrange
            var project = GetProject(testMethod);
            var controllerDocument = project.DocumentIds[0];

            var expectedOutput = Read(testMethod + ".Output");

            // Act
            var diagnostics = await AnalyzerRunner.GetDiagnosticsAsync(project);
            var actualOutput = await CodeFixRunner.ApplyCodeFixAsync(
                new AddResponseTypeAttributeCodeFixProvider(),
                project.GetDocument(controllerDocument),
                diagnostics[0]);

            Assert.Equal(expectedOutput, actualOutput, ignoreLineEndingDifferences: true);
        }

        private Project GetProject(string testMethod)
        {
            var testSource = Read(testMethod + ".Input");
            return MvcDiagnosticAnalyzerRunner.CreateProjectWithReferencesInBinDir(GetType().Assembly, new[] { testSource });
        }

        private string Read(string fileName)
        {
            return MvcTestSource.Read(GetType().Name, fileName)
                .Source
                .Replace("_INPUT_", "_TEST_")
                .Replace("_OUTPUT_", "_TEST_");
        }
    }
}
