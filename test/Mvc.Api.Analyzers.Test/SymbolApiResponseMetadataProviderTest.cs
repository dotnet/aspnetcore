// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Analyzer.Testing;
using Microsoft.AspNetCore.Mvc.Api.Analyzers.TestFiles.SymbolApiResponseMetadataProviderTest;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Api.Analyzers
{
    public class SymbolApiResponseMetadataProviderTest
    {
        private static readonly string Namespace = typeof(SymbolApiResponseMetadataProviderTest).Namespace;

        [Fact]
        public async Task GetResponseMetadata_ReturnsEmptySequence_IfNoAttributesArePresent_ForGetAction()
        {
            // Arrange
            var compilation = await GetResponseMetadataCompilation();
            var controller = compilation.GetTypeByMetadataName($"{Namespace}.{nameof(GetResponseMetadata_ControllerWithoutConvention)}");
            var method = (IMethodSymbol)controller.GetMembers(nameof(GetResponseMetadata_ControllerWithoutConvention.GetPerson)).First();
            var symbolCache = new ApiControllerSymbolCache(compilation);

            // Act
            var result = SymbolApiResponseMetadataProvider.GetDeclaredResponseMetadata(symbolCache, method);

            // Assert
            Assert.Collection(
                result,
                metadata => Assert.True(metadata.IsImplicit));
        }

        [Fact]
        public async Task GetResponseMetadata_ReturnsEmptySequence_IfNoAttributesArePresent_ForPostAction()
        {
            // Arrange
            var compilation = await GetResponseMetadataCompilation();
            var controller = compilation.GetTypeByMetadataName($"{Namespace}.{nameof(GetResponseMetadata_ControllerWithoutConvention)}");
            var method = (IMethodSymbol)controller.GetMembers(nameof(GetResponseMetadata_ControllerWithoutConvention.PostPerson)).First();
            var symbolCache = new ApiControllerSymbolCache(compilation);

            // Act
            var result = SymbolApiResponseMetadataProvider.GetDeclaredResponseMetadata(symbolCache, method);

            // Assert
            Assert.Collection(
                result,
                metadata => Assert.True(metadata.IsImplicit));
        }

        [Fact]
        public async Task GetResponseMetadata_IgnoresProducesAttribute()
        {
            // Arrange
            var compilation = await GetResponseMetadataCompilation();
            var controller = compilation.GetTypeByMetadataName($"{Namespace}.{nameof(GetResponseMetadata_ControllerActionWithAttributes)}");
            var method = (IMethodSymbol)controller.GetMembers(nameof(GetResponseMetadata_ControllerActionWithAttributes.ActionWithProducesAttribute)).First();
            var symbolCache = new ApiControllerSymbolCache(compilation);

            // Act
            var result = SymbolApiResponseMetadataProvider.GetDeclaredResponseMetadata(symbolCache, method);

            // Assert
            Assert.Collection(
                result,
                metadata => Assert.True(metadata.IsImplicit));
        }

        [Fact]
        public async Task GetResponseMetadata_ReturnsValueFromProducesResponseType_WhenStatusCodeIsSpecifiedInConstructor()
        {
            // Arrange
            var compilation = await GetResponseMetadataCompilation();
            var controller = compilation.GetTypeByMetadataName($"{Namespace}.{nameof(GetResponseMetadata_ControllerActionWithAttributes)}");
            var method = (IMethodSymbol)controller.GetMembers(nameof(GetResponseMetadata_ControllerActionWithAttributes.ActionWithProducesResponseType_StatusCodeInConstructor)).First();
            var symbolCache = new ApiControllerSymbolCache(compilation);

            // Act
            var result = SymbolApiResponseMetadataProvider.GetDeclaredResponseMetadata(symbolCache, method);

            // Assert
            Assert.Collection(
                result,
                metadata =>
                {
                    Assert.Equal(201, metadata.StatusCode);
                    Assert.NotNull(metadata.Attribute);
                    Assert.Equal(method, metadata.AttributeSource);
                });
        }

        [Fact]
        public async Task GetResponseMetadata_ReturnsValueFromProducesResponseType_WhenStatusCodeIsSpecifiedInConstructorWithResponseType()
        {
            // Arrange
            var compilation = await GetResponseMetadataCompilation();
            var controller = compilation.GetTypeByMetadataName($"{Namespace}.{nameof(GetResponseMetadata_ControllerActionWithAttributes)}");
            var method = (IMethodSymbol)controller.GetMembers(nameof(GetResponseMetadata_ControllerActionWithAttributes.ActionWithProducesResponseType_StatusCodeAndTypeInConstructor)).First();
            var symbolCache = new ApiControllerSymbolCache(compilation);

            // Act
            var result = SymbolApiResponseMetadataProvider.GetDeclaredResponseMetadata(symbolCache, method);

            // Assert
            Assert.Collection(
                result,
                metadata =>
                {
                    Assert.Equal(202, metadata.StatusCode);
                    Assert.NotNull(metadata.Attribute);
                    Assert.Equal(method, metadata.AttributeSource);
                });
        }

        [Fact]
        public async Task GetResponseMetadata_ReturnsValueFromProducesResponseType_WhenStatusCodeIsSpecifiedInConstructorAndProperty()
        {
            // Arrange
            var compilation = await GetResponseMetadataCompilation();
            var controller = compilation.GetTypeByMetadataName($"{Namespace}.{nameof(GetResponseMetadata_ControllerActionWithAttributes)}");
            var method = (IMethodSymbol)controller.GetMembers(nameof(GetResponseMetadata_ControllerActionWithAttributes.ActionWithProducesResponseType_StatusCodeInConstructorAndProperty)).First();
            var symbolCache = new ApiControllerSymbolCache(compilation);

            // Act
            var result = SymbolApiResponseMetadataProvider.GetDeclaredResponseMetadata(symbolCache, method);

            // Assert
            Assert.Collection(
                result,
                metadata =>
                {
                    Assert.Equal(203, metadata.StatusCode);
                    Assert.NotNull(metadata.Attribute);
                    Assert.Equal(method, metadata.AttributeSource);
                });
        }

        [Fact]
        public async Task GetResponseMetadata_ReturnsValueFromProducesResponseType_WhenStatusCodeAndTypeIsSpecifiedInConstructorAndProperty()
        {
            // Arrange
            var compilation = await GetResponseMetadataCompilation();
            var controller = compilation.GetTypeByMetadataName($"{Namespace}.{nameof(GetResponseMetadata_ControllerActionWithAttributes)}");
            var method = (IMethodSymbol)controller.GetMembers(nameof(GetResponseMetadata_ControllerActionWithAttributes.ActionWithProducesResponseType_StatusCodeAndTypeInConstructorAndProperty)).First();
            var symbolCache = new ApiControllerSymbolCache(compilation);

            // Act
            var result = SymbolApiResponseMetadataProvider.GetDeclaredResponseMetadata(symbolCache, method);

            // Assert
            Assert.Collection(
                result,
                metadata =>
                {
                    Assert.Equal(201, metadata.StatusCode);
                    Assert.NotNull(metadata.Attribute);
                    Assert.Equal(method, metadata.AttributeSource);
                });
        }

        [Fact]
        public async Task GetResponseMetadata_ReturnsValueFromCustomProducesResponseType()
        {
            // Arrange
            var compilation = await GetResponseMetadataCompilation();
            var controller = compilation.GetTypeByMetadataName($"{Namespace}.{nameof(GetResponseMetadata_ControllerActionWithAttributes)}");
            var method = (IMethodSymbol)controller.GetMembers(nameof(GetResponseMetadata_ControllerActionWithAttributes.ActionWithCustomProducesResponseTypeAttributeWithArguments)).First();
            var symbolCache = new ApiControllerSymbolCache(compilation);

            // Act
            var result = SymbolApiResponseMetadataProvider.GetDeclaredResponseMetadata(symbolCache, method);

            // Assert
            Assert.Collection(
                result,
                metadata =>
                {
                    Assert.Equal(201, metadata.StatusCode);
                    Assert.NotNull(metadata.Attribute);
                });
        }

        [Fact]
        public async Task GetResponseMetadata_ReturnsValuesFromApiConventionMethodAttribute()
        {
            // Arrange
            var compilation = await GetResponseMetadataCompilation();
            var controller = compilation.GetTypeByMetadataName($"{Namespace}.{nameof(GetResponseMetadata_ControllerActionWithAttributes)}");
            var method = (IMethodSymbol)controller.GetMembers(nameof(GetResponseMetadata_ControllerActionWithAttributes.GetResponseMetadata_ReturnsValuesFromApiConventionMethodAttribute)).First();
            var symbolCache = new ApiControllerSymbolCache(compilation);

            // Act
            var result = SymbolApiResponseMetadataProvider.GetDeclaredResponseMetadata(symbolCache, method);

            // Assert
            Assert.Collection(
                result,
                metadata =>
                {
                    Assert.Equal(200, metadata.StatusCode);
                    Assert.NotNull(metadata.Attribute);
                },
                metadata =>
                {
                    Assert.Equal(404, metadata.StatusCode);
                    Assert.NotNull(metadata.Attribute);
                },
                metadata =>
                {
                    Assert.True(metadata.IsDefault);
                });
        }

        [Fact]
        public async Task GetResponseMetadata_WIthProducesResponseTypeAndApiConventionMethod()
        {
            // Arrange
            var compilation = await GetResponseMetadataCompilation();
            var controller = compilation.GetTypeByMetadataName($"{Namespace}.{nameof(GetResponseMetadata_ControllerActionWithAttributes)}");
            var method = (IMethodSymbol)controller.GetMembers(nameof(GetResponseMetadata_ControllerActionWithAttributes.GetResponseMetadata_WIthProducesResponseTypeAndApiConventionMethod)).First();
            var symbolCache = new ApiControllerSymbolCache(compilation);

            // Act
            var result = SymbolApiResponseMetadataProvider.GetDeclaredResponseMetadata(symbolCache, method);

            // Assert
            Assert.Collection(
                result,
                metadata =>
                {
                    Assert.Equal(204, metadata.StatusCode);
                    Assert.NotNull(metadata.Attribute);
                });
        }

        [Fact]
        public async Task GetResponseMetadata_IgnoresCustomResponseTypeMetadataProvider()
        {
            // Arrange
            var compilation = await GetResponseMetadataCompilation();
            var controller = compilation.GetTypeByMetadataName($"{Namespace}.{nameof(GetResponseMetadata_ControllerActionWithAttributes)}");
            var method = (IMethodSymbol)controller.GetMembers(nameof(GetResponseMetadata_ControllerActionWithAttributes.ActionWithCustomApiResponseMetadataProvider)).First();
            var symbolCache = new ApiControllerSymbolCache(compilation);

            // Act
            var result = SymbolApiResponseMetadataProvider.GetDeclaredResponseMetadata(symbolCache, method);

            // Assert
            Assert.Collection(
                result,
                metadata => Assert.True(metadata.IsImplicit));
        }

        [Fact]
        public Task GetResponseMetadata_IgnoresAttributesWithIncorrectStatusCodeType()
        {
            return GetResponseMetadata_WorksForInvalidOrUnsupportedAttributes(
                nameof(GetResponseMetadata_ControllerActionWithAttributes),
                nameof(GetResponseMetadata_ControllerActionWithAttributes.ActionWithProducesResponseTypeWithIncorrectStatusCodeType));
        }

        [Fact]
        public Task GetResponseMetadata_IgnoresDerivedAttributesWithoutPropertyOnConstructorArguments()
        {
            return GetResponseMetadata_WorksForInvalidOrUnsupportedAttributes(
                nameof(GetResponseMetadata_ControllerActionWithAttributes),
                nameof(GetResponseMetadata_ControllerActionWithAttributes.ActionWithCustomProducesResponseTypeAttributeWithoutArguments));
        }

        private async Task GetResponseMetadata_WorksForInvalidOrUnsupportedAttributes(string typeName, string methodName)
        {
            // Arrange
            var compilation = await GetResponseMetadataCompilation();
            var controller = compilation.GetTypeByMetadataName($"{Namespace}.{typeName}");
            var method = (IMethodSymbol)controller.GetMembers(methodName).First();
            var symbolCache = new ApiControllerSymbolCache(compilation);

            // Act
            var result = SymbolApiResponseMetadataProvider.GetDeclaredResponseMetadata(symbolCache, method);

            // Assert
            Assert.Collection(
                result,
                metadata =>
                {
                    Assert.Equal(200, metadata.StatusCode);
                    Assert.Same(method, metadata.AttributeSource);
                });
        }

        [Fact]
        public Task GetStatusCode_ReturnsValueFromConstructor()
        {
            //  Arrange
            var actionName = nameof(GetResponseMetadata_ControllerActionWithAttributes.ActionWithProducesResponseType_StatusCodeInConstructor);
            var expected = 201;

            // Act & Assert
            return GetStatusCodeTest(actionName, expected);
        }

        [Fact]
        public Task GetStatusCode_ReturnsValueFromProperty()
        {
            //  Arrange
            var actionName = nameof(GetResponseMetadata_ControllerActionWithAttributes.ActionWithProducesResponseType_StatusCodeAndTypeInConstructorAndProperty);
            var expected = 201;

            // Act & Assert
            return GetStatusCodeTest(actionName, expected);
        }

        [Fact]
        public Task GetStatusCode_ReturnsValueFromConstructor_WhenTypeIsSpecified()
        {
            //  Arrange
            var actionName = nameof(GetResponseMetadata_ControllerActionWithAttributes.ActionWithProducesResponseType_StatusCodeAndTypeInConstructor);
            var expected = 202;

            // Act & Assert
            return GetStatusCodeTest(actionName, expected);
        }

        [Fact]
        public Task GetStatusCode_Returns200_IfTypeIsNotInteger()
        {
            //  Arrange
            var actionName = nameof(GetResponseMetadata_ControllerActionWithAttributes.ActionWithProducesResponseTypeWithIncorrectStatusCodeType);
            var expected = 200;

            // Act & Assert
            return GetStatusCodeTest(actionName, expected);
        }

        [Fact]
        public Task GetStatusCode_ReturnsValueFromDerivedAttributes()
        {
            //  Arrange
            var actionName = nameof(GetResponseMetadata_ControllerActionWithAttributes.ActionWithCustomProducesResponseTypeAttributeWithArguments);
            var expected = 201;

            // Act & Assert
            return GetStatusCodeTest(actionName, expected);
        }

        private async Task GetStatusCodeTest(string actionName, int expected)
        {
            var compilation = await GetResponseMetadataCompilation();
            var controller = compilation.GetTypeByMetadataName($"{Namespace}.{nameof(GetResponseMetadata_ControllerActionWithAttributes)}");
            var method = (IMethodSymbol)controller.GetMembers(actionName).First();
            var attribute = method.GetAttributes().First();

            var statusCode = SymbolApiResponseMetadataProvider.GetStatusCode(attribute);

            Assert.Equal(expected, statusCode);
        }

        [Fact]
        public async Task GetDefaultStatusCode_ReturnsValueDefinedUsingStatusCodeConstants()
        {
            // Arrange
            var compilation = await GetCompilation("GetDefaultStatusCodeTest");
            var attribute = compilation.GetTypeByMetadataName(typeof(TestActionResultUsingStatusCodesConstants).FullName).GetAttributes()[0];

            // Act
            var actual = SymbolApiResponseMetadataProvider.GetDefaultStatusCode(attribute);

            // Assert
            Assert.Equal(412, actual);
        }

        [Fact]
        public async Task GetDefaultStatusCode_ReturnsValueDefinedUsingHttpStatusCast()
        {
            // Arrange
            var compilation = await GetCompilation("GetDefaultStatusCodeTest");
            var attribute = compilation.GetTypeByMetadataName(typeof(TestActionResultUsingHttpStatusCodeCast).FullName).GetAttributes()[0];

            // Act
            var actual = SymbolApiResponseMetadataProvider.GetDefaultStatusCode(attribute);

            // Assert
            Assert.Equal(302, actual);
        }

        [Fact]
        public async Task InspectReturnExpression_ReturnsNull_IfReturnExpressionCannotBeFound()
        {
            // Arrange & Act
            var source = @"
            using Microsoft.AspNetCore.Mvc;

namespace Microsoft.AspNetCore.Mvc.Api.Analyzers
{
    [ApiController]
    public class InspectReturnExpression_ReturnsNull_IfReturnExpressionCannotBeFound : ControllerBase
    {
        public IActionResult Get(int id)
        {
            return new DoesNotExist(id);
        }
    }
}";
            var actualResponseMetadata = await RunInspectReturnStatementSyntax(source, nameof(InspectReturnExpression_ReturnsNull_IfReturnExpressionCannotBeFound));

            // Assert
            Assert.Null(actualResponseMetadata);
        }

        [Fact]
        public async Task InspectReturnExpression_ReturnsStatusCodeFromDefaultStatusCodeAttributeOnActionResult()
        {
            // Arrange & Act
            var actualResponseMetadata = await RunInspectReturnStatementSyntax();

            // Assert
            Assert.NotNull(actualResponseMetadata);
            Assert.Equal(401, actualResponseMetadata.Value.StatusCode);
        }

        [Fact]
        public async Task InspectReturnExpression_ReturnsDefaultResponseMetadata_IfReturnedTypeIsNotActionResult()
        {
            // Arrange & Act
            var actualResponseMetadata = await RunInspectReturnStatementSyntax();

            // Assert
            Assert.NotNull(actualResponseMetadata);
            Assert.True(actualResponseMetadata.Value.IsDefaultResponse);
        }

        [Fact]
        public async Task TryGetActualResponseMetadata_ActionWithActionResultOfTReturningOkResult()
        {
            // Arrange
            var typeName = typeof(TryGetActualResponseMetadataController).FullName;
            var methodName = nameof(TryGetActualResponseMetadataController.ActionWithActionResultOfTReturningOkResult);

            // Act
            var (success, responseMetadatas, _) = await TryGetActualResponseMetadata(typeName, methodName);

            // Assert
            Assert.True(success);
            Assert.Collection(
                responseMetadatas,
                metadata =>
                {
                    Assert.False(metadata.IsDefaultResponse);
                    Assert.Equal(200, metadata.StatusCode);
                });
        }

        [Fact]
        public async Task TryGetActualResponseMetadata_ActionWithActionResultOfTReturningModel()
        {
            // Arrange
            var typeName = typeof(TryGetActualResponseMetadataController).FullName;
            var methodName = nameof(TryGetActualResponseMetadataController.ActionWithActionResultOfTReturningModel);

            // Act
            var (success, responseMetadatas, _) = await TryGetActualResponseMetadata(typeName, methodName);

            // Assert
            Assert.True(success);
            Assert.Collection(
                responseMetadatas,
                metadata =>
                {
                    Assert.True(metadata.IsDefaultResponse);
                });
        }

        [Fact]
        public async Task TryGetActualResponseMetadata_ActionReturningNotFoundAndModel()
        {
            // Arrange
            var typeName = typeof(TryGetActualResponseMetadataController).FullName;
            var methodName = nameof(TryGetActualResponseMetadataController.ActionReturningNotFoundAndModel);

            // Act
            var (success, responseMetadatas, testSource) = await TryGetActualResponseMetadata(typeName, methodName);

            // Assert
            Assert.True(success);
            Assert.Collection(
                responseMetadatas,
                metadata =>
                {
                    Assert.False(metadata.IsDefaultResponse);
                    Assert.Equal(204, metadata.StatusCode);
                    AnalyzerAssert.DiagnosticLocation(testSource.MarkerLocations["MM1"], metadata.ReturnStatement.GetLocation());
                    
                },
                metadata =>
                {
                    Assert.True(metadata.IsDefaultResponse);
                    AnalyzerAssert.DiagnosticLocation(testSource.MarkerLocations["MM2"], metadata.ReturnStatement.GetLocation());
                });
        }

        private async Task<(bool result, IList<ActualApiResponseMetadata> responseMetadatas, TestSource testSource)> TryGetActualResponseMetadata(string typeName, string methodName)
        {
            var testSource = MvcTestSource.Read(GetType().Name, "TryGetActualResponseMetadataTests");
            var project = DiagnosticProject.Create(GetType().Assembly, new[] { testSource.Source });

            var compilation = await GetCompilation("TryGetActualResponseMetadataTests");

            var type = compilation.GetTypeByMetadataName(typeName);
            var method = (IMethodSymbol)type.GetMembers(methodName).First();
            var symbolCache = new ApiControllerSymbolCache(compilation);

            var syntaxTree = method.DeclaringSyntaxReferences[0].SyntaxTree;
            var methodSyntax = (MethodDeclarationSyntax)syntaxTree.GetRoot().FindNode(method.Locations[0].SourceSpan);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);

            var result = SymbolApiResponseMetadataProvider.TryGetActualResponseMetadata(symbolCache, semanticModel, methodSyntax, CancellationToken.None, out var responseMetadatas);

            return (result, responseMetadatas, testSource);
        }

        private async Task<ActualApiResponseMetadata?> RunInspectReturnStatementSyntax([CallerMemberName]string test = null)
        {
            // Arrange
            var testSource = MvcTestSource.Read(GetType().Name, test);
            return await RunInspectReturnStatementSyntax(testSource.Source, test);
        }

        private async Task<ActualApiResponseMetadata?> RunInspectReturnStatementSyntax(string source, string test)
        {
            var project = DiagnosticProject.Create(GetType().Assembly, new[] { source });
            var compilation = await project.GetCompilationAsync();
            var symbolCache = new ApiControllerSymbolCache(compilation);

            var returnType = compilation.GetTypeByMetadataName($"{Namespace}.{test}");
            var syntaxTree = returnType.DeclaringSyntaxReferences[0].SyntaxTree;

            var method = (IMethodSymbol)returnType.GetMembers().First();
            var methodSyntax = syntaxTree.GetRoot().FindNode(method.Locations[0].SourceSpan);
            var returnStatement = methodSyntax.DescendantNodes().OfType<ReturnStatementSyntax>().First();

            return SymbolApiResponseMetadataProvider.InspectReturnStatementSyntax(
                symbolCache,
                compilation.GetSemanticModel(syntaxTree),
                returnStatement,
                CancellationToken.None);
        }

        private Task<Compilation> GetResponseMetadataCompilation() => GetCompilation("GetResponseMetadataTests");

        private Task<Compilation> GetCompilation(string test)
        {
            var testSource = MvcTestSource.Read(GetType().Name, test);
            var project = DiagnosticProject.Create(GetType().Assembly, new[] { testSource.Source });

            return project.GetCompilationAsync();
        }
    }
}
