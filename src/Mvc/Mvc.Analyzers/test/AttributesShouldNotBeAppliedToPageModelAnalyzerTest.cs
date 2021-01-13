// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Analyzer.Testing;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Analyzers
{
    public class AttributesShouldNotBeAppliedToPageModelAnalyzerTest
    {
        private MvcDiagnosticAnalyzerRunner Executor { get; } = new MvcDiagnosticAnalyzerRunner(new AttributesShouldNotBeAppliedToPageModelAnalyzer());

        [Fact]
        public async Task NoDiagnosticsAreReturned_FoEmptyScenarios()
        {
            // Act
            var result = await Executor.GetDiagnosticsAsync(source: string.Empty);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public Task NoDiagnosticsAreReturned_ForControllerBaseActions()
            => VerifyNoDiagnosticsAreReturned();

        [Fact]
        public Task NoDiagnosticsAreReturned_ForControllerActions()
            => VerifyNoDiagnosticsAreReturned();

        [Fact]
        public Task NoDiagnosticsAreReturned_ForPageHandlersWithNonFilterAttributes()
            => VerifyNoDiagnosticsAreReturned();

        [Fact]
        public Task NoDiagnosticsAreReturned_IfFiltersAreAppliedToPageModel()
            => VerifyNoDiagnosticsAreReturned();

        [Fact]
        public Task NoDiagnosticsAreReturned_IfAuthorizeAttributeIsAppliedToPageModel()
            => VerifyNoDiagnosticsAreReturned();

        [Fact]
        public Task NoDiagnosticsAreReturned_IfAllowAnonymousIsAppliedToPageModel()
           => VerifyNoDiagnosticsAreReturned();

        [Fact]
        public Task NoDiagnosticsAreReturned_ForNonHandlerMethodsWithAttributes()
            => VerifyNoDiagnosticsAreReturned();

        [Fact]
        public Task DiagnosticsAreReturned_IfFiltersAreAppliedToPageHandlerMethod()
            => VerifyDefault(DiagnosticDescriptors.MVC1001_FiltersShouldNotBeAppliedToPageHandlerMethods);

        [Fact]
        public Task DiagnosticsAreReturned_IfFiltersAreAppliedToPageHandlerMethodDerivingFromCustomModel()
            => VerifyDefault(DiagnosticDescriptors.MVC1001_FiltersShouldNotBeAppliedToPageHandlerMethods);

        [Fact]
        public Task DiagnosticsAreReturned_IfAuthorizeAttributeIsAppliedToPageHandlerMethod()
            => VerifyDefault(DiagnosticDescriptors.MVC1001_FiltersShouldNotBeAppliedToPageHandlerMethods);

        [Fact]
        public Task DiagnosticsAreReturned_IfFiltersAreAppliedToPageHandlerMethodForTypeWithPageModelAttribute()
            => VerifyDefault(DiagnosticDescriptors.MVC1001_FiltersShouldNotBeAppliedToPageHandlerMethods);

        [Fact]
        public Task DiagnosticsAreReturned_IfAttributeIsAppliedToBaseType()
            => VerifyDefault(DiagnosticDescriptors.MVC1001_FiltersShouldNotBeAppliedToPageHandlerMethods);

        [Fact]
        public Task DiagnosticsAreReturned_IfRouteAttributesAreAppliedToPageHandlerMethod()
            => VerifyDefault(DiagnosticDescriptors.MVC1002_RouteAttributesShouldNotBeAppliedToPageHandlerMethods);

        [Fact]
        public Task DiagnosticsAreReturned_IfAllowAnonymousIsAppliedToPageHandlerMethod()
            => VerifyDefault(DiagnosticDescriptors.MVC1001_FiltersShouldNotBeAppliedToPageHandlerMethods);

        [Fact]
        public Task DiagnosticsAreReturned_IfRouteAttribute_IsAppliedToPageModel()
            => VerifyDefault(DiagnosticDescriptors.MVC1003_RouteAttributesShouldNotBeAppliedToPageModels);

        private async Task VerifyNoDiagnosticsAreReturned([CallerMemberName] string testMethod = "")
        {
            // Arrange
            var source = MvcTestSource.Read(GetType().Name, testMethod);

            // Act
            var result = await Executor.GetDiagnosticsAsync(source.Source);

            // Assert
            Assert.Empty(result);
        }

        private async Task VerifyDefault(DiagnosticDescriptor descriptor, [CallerMemberName] string testMethod = "")
        {
            // Arrange
            var testSource = MvcTestSource.Read(GetType().Name, testMethod);
            var expectedLocation = testSource.DefaultMarkerLocation;

            // Act
            var result = await Executor.GetDiagnosticsAsync(testSource.Source);

            // Assert
            Assert.Collection(
                result,
                diagnostic =>
                {

                    Assert.Equal(descriptor.Id, diagnostic.Id);
                    Assert.Same(descriptor, diagnostic.Descriptor);
                    AnalyzerAssert.DiagnosticLocation(expectedLocation, diagnostic.Location);
                });
        }
    }
}
