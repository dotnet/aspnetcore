// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Analyzer.Testing;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Analyzers
{
    public class TagHelpersInCodeBlocksAnalyzerTest
    {
        private readonly DiagnosticDescriptor DiagnosticDescriptor = DiagnosticDescriptors.MVC1006_FunctionsContainingTagHelpersMustBeAsyncAndReturnTask;

        private MvcDiagnosticAnalyzerRunner Executor { get; } = new MvcDiagnosticAnalyzerRunner(new TagHelpersInCodeBlocksAnalyzer());

        [Fact]
        public Task DiagnosticsAreReturned_ForUseOfTagHelpersInActions()
            => VerifyDefault(ReadTestSource());

        [Fact]
        public Task DiagnosticsAreReturned_ForUseOfTagHelpersInNonAsyncFunc()
            => VerifyDefault(ReadTestSource());

        [Fact]
        public Task DiagnosticsAreReturned_ForUseOfTagHelpersInVoidClassMethods()
            => VerifyDefault(ReadTestSource());

        [Fact]
        public Task DiagnosticsAreReturned_ForUseOfTagHelpersInVoidDelegates()
            => VerifyDefault(ReadTestSource());

        [Fact]
        public Task DiagnosticsAreReturned_ForUseOfTagHelpersInVoidLocalFunctions()
            => VerifyDefault(ReadTestSource());

        [Fact]
        public Task NoDiagnosticsAreReturned_ForUseOfTagHelpersInAsyncClassMethods()
            => VerifyNoDiagnosticsAreReturned(ReadTestSource());

        [Fact]
        public Task NoDiagnosticsAreReturned_ForUseOfTagHelpersInAsyncDelegates()
            => VerifyNoDiagnosticsAreReturned(ReadTestSource());

        [Fact]
        public Task NoDiagnosticsAreReturned_ForUseOfTagHelpersInAsyncFuncs()
            => VerifyNoDiagnosticsAreReturned(ReadTestSource());

        [Fact]
        public Task NoDiagnosticsAreReturned_ForUseOfTagHelpersInAsyncLocalFunctions()
            => VerifyNoDiagnosticsAreReturned(ReadTestSource());

        [Fact]
        public Task SingleDiagnosticIsReturned_ForMultipleTagHelpersInVoidMethod()
            => VerifyDefault(ReadTestSource());

        private async Task VerifyNoDiagnosticsAreReturned(TestSource source)
        {
            // Act
            var result = await Executor.GetDiagnosticsAsync(source.Source);

            // Assert
            Assert.Empty(result);
        }

        private async Task VerifyDefault(TestSource testSource)
        {
            // Arrange
            var expectedLocation = testSource.DefaultMarkerLocation;

            // Act
            var result = await Executor.GetDiagnosticsAsync(testSource.Source);

            // Assert
            // We want to ignore C# diagnostics because they'll include diagnostics for not awaiting bits correctly. The purpose of this analyzer is to
            // improve on those error messages but not remove them.
            var filteredDiagnostics = result.Where(diagnostic => diagnostic.Id.StartsWith("MVC"));
            Assert.Collection(
                filteredDiagnostics,
                diagnostic =>
                {
                    Assert.Equal(DiagnosticDescriptor.Id, diagnostic.Id);
                    Assert.Same(DiagnosticDescriptor, diagnostic.Descriptor);
                    AnalyzerAssert.DiagnosticLocation(expectedLocation, diagnostic.Location);
                });
        }

        private static TestSource ReadTestSource([CallerMemberName] string testMethod = "") =>
            MvcTestSource.Read(nameof(TagHelpersInCodeBlocksAnalyzerTest), testMethod);
    }
}
