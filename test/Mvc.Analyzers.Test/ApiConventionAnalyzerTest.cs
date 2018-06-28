// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Analyzer.Testing;
using Microsoft.AspNetCore.Mvc.Analyzers.Infrastructure;
using Microsoft.CodeAnalysis;
using Xunit;
using static Microsoft.AspNetCore.Mvc.Analyzers.ApiConventionAnalyzer;

namespace Microsoft.AspNetCore.Mvc.Analyzers
{
    public class ApiConventionAnalyzerTest
    {
        [Fact]
        public async Task GetDefaultStatusCode_ReturnsValueDefinedUsingStatusCodeConstants()
        {
            // Arrange
            var compilation = await GetCompilation();
            var attribute = compilation.GetTypeByMetadataName(typeof(TestActionResultUsingStatusCodesConstants).FullName).GetAttributes()[0];

            // Act
            var actual = ApiConventionAnalyzer.GetDefaultStatusCode(attribute);

            // Assert
            Assert.Equal(412, actual);
        }

        [Fact]
        public async Task GetDefaultStatusCode_ReturnsValueDefinedUsingHttpStatusCast()
        {
            // Arrange
            var compilation = await GetCompilation();
            var attribute = compilation.GetTypeByMetadataName(typeof(TestActionResultUsingHttpStatusCodeCast).FullName).GetAttributes()[0];

            // Act
            var actual = ApiConventionAnalyzer.GetDefaultStatusCode(attribute);

            // Assert
            Assert.Equal(302, actual);
        }

        [Fact]
        public async Task InspectReturnExpression_ReturnsNull_ForReturnTypeIf200StatusCodeIsDeclared()
        {
            // Arrange
            var compilation = await GetCompilation();

            var returnType = compilation.GetTypeByMetadataName(typeof(ApiConventionAnalyzerBaseModel).FullName);
            var context = GetContext(compilation, new[] { 200 });

            // Act
            var diagnostic = ApiConventionAnalyzer.InspectReturnExpression(context, returnType, Location.None);

            // Assert
            Assert.Null(diagnostic);
        }

        [Fact]
        public async Task InspectReturnExpression_ReturnsNull_ForReturnTypeIf201StatusCodeIsDeclared()
        {
            // Arrange
            var compilation = await GetCompilation();

            var returnType = compilation.GetTypeByMetadataName(typeof(ApiConventionAnalyzerBaseModel).FullName);
            var context = GetContext(compilation, new[] { 201 });

            // Act
            var diagnostic = ApiConventionAnalyzer.InspectReturnExpression(context, returnType, Location.None);

            // Assert
            Assert.Null(diagnostic);
        }

        [Fact]
        public async Task InspectReturnExpression_ReturnsNull_ForDerivedReturnTypeIf200StatusCodeIsDeclared()
        {
            // Arrange
            var compilation = await GetCompilation();

            var declaredReturnType = compilation.GetTypeByMetadataName(typeof(ApiConventionAnalyzerBaseModel).FullName);
            var context = GetContext(compilation, new[] { 201 });
            var actualReturnType = compilation.GetTypeByMetadataName(typeof(ApiConventionAnalyzerDerivedModel).FullName);

            // Act
            var diagnostic = ApiConventionAnalyzer.InspectReturnExpression(context, actualReturnType, Location.None);

            // Assert
            Assert.Null(diagnostic);
        }

        [Fact]
        public async Task InspectReturnExpression_ReturnsDiagnostic_If200IsNotDocumented()
        {
            // Arrange
            var compilation = await GetCompilation();

            var context = GetContext(compilation, new[] { 404 });
            var actualReturnType = compilation.GetTypeByMetadataName(typeof(ApiConventionAnalyzerDerivedModel).FullName);

            // Act
            var diagnostic = ApiConventionAnalyzer.InspectReturnExpression(context, actualReturnType, Location.None);

            // Assert
            Assert.NotNull(diagnostic);
            Assert.Same(DiagnosticDescriptors.MVC1005_ActionReturnsUndocumentedSuccessResult, diagnostic.Descriptor);
        }

        [Fact]
        public async Task InspectReturnExpression_ReturnsDiagnostic_IfReturnTypeIsActionResultReturningUndocumentedStatusCode()
        {
            // Arrange
            var compilation = await GetCompilation();

            var declaredReturnType = compilation.GetTypeByMetadataName(typeof(ApiConventionAnalyzerBaseModel).FullName);
            var context = GetContext(compilation, new[] { 200, 404 });
            var actualReturnType = compilation.GetTypeByMetadataName(typeof(BadRequestObjectResult).FullName);

            // Act
            var diagnostic = ApiConventionAnalyzer.InspectReturnExpression(context, actualReturnType, Location.None);

            // Assert
            Assert.NotNull(diagnostic);
            Assert.Same(DiagnosticDescriptors.MVC1004_ActionReturnsUndocumentedStatusCode, diagnostic.Descriptor);
        }

        [Fact]
        public async Task InspectReturnExpression_DoesNotReturnDiagnostic_IfReturnTypeDoesNotHaveStatusCodeAttribute()
        {
            // Arrange
            var compilation = await GetCompilation();

            var context = GetContext(compilation, new[] { 200, 404 });
            var actualReturnType = compilation.GetTypeByMetadataName(typeof(EmptyResult).FullName);

            // Act
            var diagnostic = ApiConventionAnalyzer.InspectReturnExpression(context, actualReturnType, Location.None);

            // Assert
            Assert.Null(diagnostic);
        }

        [Fact]
        public async Task InspectReturnExpression_DoesNotReturnDiagnostic_IfDeclaredAndActualReturnTypeAreIActionResultInstances()
        {
            // Arrange
            var compilation = await GetCompilation();

            var declaredReturnType = compilation.GetTypeByMetadataName(typeof(IActionResult).FullName);
            var context = GetContext(compilation, new[] { 404 });
            var actualReturnType = compilation.GetTypeByMetadataName(typeof(EmptyResult).FullName);

            // Act
            var diagnostic = ApiConventionAnalyzer.InspectReturnExpression(context, actualReturnType, Location.None);

            // Assert
            Assert.Null(diagnostic);
        }

        [Fact]
        public async Task InspectReturnExpression_DoesNotReturnDiagnostic_IfDeclaredAndActualReturnTypeAreIActionResult()
        {
            // Arrange
            var compilation = await GetCompilation();

            var context = GetContext(compilation, new[] { 404 });
            var actualReturnType = compilation.GetTypeByMetadataName(typeof(IActionResult).FullName);

            // Act
            var diagnostic = ApiConventionAnalyzer.InspectReturnExpression(context, actualReturnType, Location.None);

            // Assert
            Assert.Null(diagnostic);
        }

        [Fact]
        public async Task ShouldEvaluateMethod_ReturnsFalse_IfMethodReturnTypeIsInvalid()
        {
            // Arrange
            var source = @"
using Microsoft.AspNetCore.Mvc;

namespace TestNamespace
{
    [ApiController]
    public class TestController : ControllerBase
    {
        public DoesNotExist Get(int id)
        {
            if (id == 0)
            {
                return NotFound();
            }

            return new DoesNotExist(id);
        }
    }
}";
            var project = DiagnosticProject.Create(GetType().Assembly, new[] { source });
            var compilation = await project.GetCompilationAsync();
            var symbolCache = new ApiControllerSymbolCache(compilation);
            var method = (IMethodSymbol)compilation.GetTypeByMetadataName("TestNamespace.TestController").GetMembers("Get").First();

            // Act
            var result = ApiConventionAnalyzer.ShouldEvaluateMethod(symbolCache, method);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task ShouldEvaluateMethod_ReturnsFalse_IfContainingTypeIsNotController()
        {
            // Arrange
            var compilation = await GetCompilation();
            var symbolCache = new ApiControllerSymbolCache(compilation);
            var type = compilation.GetTypeByMetadataName(typeof(ApiConventionAnalyzerTest_IndexModel).FullName);
            var method = (IMethodSymbol)type.GetMembers(nameof(ApiConventionAnalyzerTest_IndexModel.OnGet)).First();

            // Act
            var result = ApiConventionAnalyzer.ShouldEvaluateMethod(symbolCache, method);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task ShouldEvaluateMethod_ReturnsFalse_IfContainingTypeIsNotApiController()
        {
            // Arrange
            var compilation = await GetCompilation();
            var symbolCache = new ApiControllerSymbolCache(compilation);
            var type = compilation.GetTypeByMetadataName(typeof(ApiConventionAnalyzerTest_NotApiController).FullName);
            var method = (IMethodSymbol)type.GetMembers(nameof(ApiConventionAnalyzerTest_NotApiController.Index)).First();

            // Act
            var result = ApiConventionAnalyzer.ShouldEvaluateMethod(symbolCache, method);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task ShouldEvaluateMethod_ReturnsFalse_IfContainingTypeIsNotAction()
        {
            // Arrange
            var compilation = await GetCompilation();
            var symbolCache = new ApiControllerSymbolCache(compilation);
            var type = compilation.GetTypeByMetadataName(typeof(ApiConventionAnalyzerTest_NotAction).FullName);
            var method = (IMethodSymbol)type.GetMembers(nameof(ApiConventionAnalyzerTest_NotAction.Index)).First();

            // Act
            var result = ApiConventionAnalyzer.ShouldEvaluateMethod(symbolCache, method);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task ShouldEvaluateMethod_ReturnsTrue_ForValidActionMethods()
        {
            // Arrange
            var compilation = await GetCompilation();
            var symbolCache = new ApiControllerSymbolCache(compilation);
            var type = compilation.GetTypeByMetadataName(typeof(ApiConventionAnalyzerTest_Valid).FullName);
            var method = (IMethodSymbol)type.GetMembers(nameof(ApiConventionAnalyzerTest_Valid.Index)).First();

            // Act
            var result = ApiConventionAnalyzer.ShouldEvaluateMethod(symbolCache, method);

            // Assert
            Assert.True(result);
        }

        private static ApiConventionContext GetContext(Compilation compilation, int[] expectedStatusCodes)
        {
            var symbolCache = new ApiControllerSymbolCache(compilation);
            var context = new ApiConventionContext(
                symbolCache,
                default,
                expectedStatusCodes.Select(s => new ApiResponseMetadata(s, null, null)).ToArray(),
                new HashSet<int>());
            return context;
        }

        private Task<Compilation> GetCompilation()
        {
            var testSource = MvcTestSource.Read(GetType().Name, "ApiConventionAnalyzerTestFile");
            var project = DiagnosticProject.Create(GetType().Assembly, new[] { testSource.Source });

            return project.GetCompilationAsync();
        }
    }
}
