// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Analyzer.Testing;
using Microsoft.AspNetCore.Mvc.Api.Analyzers.TestFiles.ApiControllerFactsTest;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Api.Analyzers
{
    public class ApiControllerFactsTest
    {
        [Fact]
        public async Task IsApiControllerAction_ReturnsFalse_IfMethodReturnTypeIsInvalid()
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
            var project = MvcDiagnosticAnalyzerRunner.CreateProjectWithReferencesInBinDir(GetType().Assembly, new[] { source });
            var compilation = await project.GetCompilationAsync();
            Assert.True(ApiControllerSymbolCache.TryCreate(compilation, out var symbolCache));
            var method = (IMethodSymbol)compilation.GetTypeByMetadataName("TestNamespace.TestController").GetMembers("Get").First();

            // Act
            var result = ApiControllerFacts.IsApiControllerAction(symbolCache, method);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task IsApiControllerAction_ReturnsFalse_IfContainingTypeIsNotController()
        {
            // Arrange
            var compilation = await GetCompilation();
            Assert.True(ApiControllerSymbolCache.TryCreate(compilation, out var symbolCache));
            var type = compilation.GetTypeByMetadataName(typeof(ApiConventionAnalyzerTest_IndexModel).FullName);
            var method = (IMethodSymbol)type.GetMembers(nameof(ApiConventionAnalyzerTest_IndexModel.OnGet)).First();

            // Act
            var result = ApiControllerFacts.IsApiControllerAction(symbolCache, method);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task IsApiControllerAction_ReturnsFalse_IfContainingTypeIsNotApiController()
        {
            // Arrange
            var compilation = await GetCompilation();
            Assert.True(ApiControllerSymbolCache.TryCreate(compilation, out var symbolCache));
            var type = compilation.GetTypeByMetadataName(typeof(ApiConventionAnalyzerTest_NotApiController).FullName);
            var method = (IMethodSymbol)type.GetMembers(nameof(ApiConventionAnalyzerTest_NotApiController.Index)).First();

            // Act
            var result = ApiControllerFacts.IsApiControllerAction(symbolCache, method);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task IsApiControllerAction_ReturnsFalse_IfContainingTypeIsNotAction()
        {
            // Arrange
            var compilation = await GetCompilation();
            Assert.True(ApiControllerSymbolCache.TryCreate(compilation, out var symbolCache));
            var type = compilation.GetTypeByMetadataName(typeof(ApiConventionAnalyzerTest_NotAction).FullName);
            var method = (IMethodSymbol)type.GetMembers(nameof(ApiConventionAnalyzerTest_NotAction.Index)).First();

            // Act
            var result = ApiControllerFacts.IsApiControllerAction(symbolCache, method);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task IsApiControllerAction_ReturnsTrue_ForValidActionMethods()
        {
            // Arrange
            var compilation = await GetCompilation();
            Assert.True(ApiControllerSymbolCache.TryCreate(compilation, out var symbolCache));
            var type = compilation.GetTypeByMetadataName(typeof(ApiConventionAnalyzerTest_Valid).FullName);
            var method = (IMethodSymbol)type.GetMembers(nameof(ApiConventionAnalyzerTest_Valid.Index)).First();

            // Act
            var result = ApiControllerFacts.IsApiControllerAction(symbolCache, method);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task IsApiControllerAction_ReturnsTrue_IfAttributeIsDeclaredOnAssembly()
        {
            // Arrange
            var compilation = await GetCompilation(nameof(IsApiControllerAction_ReturnsTrue_IfAttributeIsDeclaredOnAssembly));
            Assert.True(ApiControllerSymbolCache.TryCreate(compilation, out var symbolCache));
            var type = compilation.GetTypeByMetadataName(typeof(IsApiControllerAction_ReturnsTrue_IfAttributeIsDeclaredOnAssemblyController).FullName);
            var method = (IMethodSymbol)type.GetMembers(nameof(IsApiControllerAction_ReturnsTrue_IfAttributeIsDeclaredOnAssemblyController.Action)).First();

            // Act
            var result = ApiControllerFacts.IsApiControllerAction(symbolCache, method);

            // Assert
            Assert.True(result);
        }

        private Task<Compilation> GetCompilation(string testFile = "TestFile")
        {
            var testSource = MvcTestSource.Read(GetType().Name, testFile);
            var project = MvcDiagnosticAnalyzerRunner.CreateProjectWithReferencesInBinDir(GetType().Assembly, new[] { testSource.Source });

            return project.GetCompilationAsync();
        }
    }
}
