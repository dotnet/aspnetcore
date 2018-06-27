// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Analyzer.Testing;
using Microsoft.AspNetCore.Mvc.Analyzers.Infrastructure;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Analyzers
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
            var typeCache = new ApiControllerTypeCache(compilation);

            // Act
            var result = SymbolApiResponseMetadataProvider.GetResponseMetadata(typeCache, method);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetResponseMetadata_ReturnsEmptySequence_IfNoAttributesArePresent_ForPostAction()
        {
            // Arrange
            var compilation = await GetResponseMetadataCompilation();
            var controller = compilation.GetTypeByMetadataName($"{Namespace}.{nameof(GetResponseMetadata_ControllerWithoutConvention)}");
            var method = (IMethodSymbol)controller.GetMembers(nameof(GetResponseMetadata_ControllerWithoutConvention.PostPerson)).First();
            var typeCache = new ApiControllerTypeCache(compilation);

            // Act
            var result = SymbolApiResponseMetadataProvider.GetResponseMetadata(typeCache, method);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetResponseMetadata_IgnoresProducesAttribute()
        {
            // Arrange
            var compilation = await GetResponseMetadataCompilation();
            var controller = compilation.GetTypeByMetadataName($"{Namespace}.{nameof(GetResponseMetadata_ControllerActionWithAttributes)}");
            var method = (IMethodSymbol)controller.GetMembers(nameof(GetResponseMetadata_ControllerActionWithAttributes.ActionWithProducesAttribute)).First();
            var typeCache = new ApiControllerTypeCache(compilation);

            // Act
            var result = SymbolApiResponseMetadataProvider.GetResponseMetadata(typeCache, method);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetResponseMetadata_ReturnsValueFromProducesResponseType_WhenStatusCodeIsSpecifiedInConstructor()
        {
            // Arrange
            var compilation = await GetResponseMetadataCompilation();
            var controller = compilation.GetTypeByMetadataName($"{Namespace}.{nameof(GetResponseMetadata_ControllerActionWithAttributes)}");
            var method = (IMethodSymbol)controller.GetMembers(nameof(GetResponseMetadata_ControllerActionWithAttributes.ActionWithProducesResponseType_StatusCodeInConstructor)).First();
            var typeCache = new ApiControllerTypeCache(compilation);

            // Act
            var result = SymbolApiResponseMetadataProvider.GetResponseMetadata(typeCache, method);

            // Assert
            Assert.Collection(
                result,
                metadata =>
                {
                    Assert.Equal(201, metadata.StatusCode);
                    Assert.NotNull(metadata.Attribute);
                    Assert.Null(metadata.Convention);
                });
        }

        [Fact]
        public async Task GetResponseMetadata_ReturnsValueFromProducesResponseType_WhenStatusCodeIsSpecifiedInConstructorWithResponseType()
        {
            // Arrange
            var compilation = await GetResponseMetadataCompilation();
            var controller = compilation.GetTypeByMetadataName($"{Namespace}.{nameof(GetResponseMetadata_ControllerActionWithAttributes)}");
            var method = (IMethodSymbol)controller.GetMembers(nameof(GetResponseMetadata_ControllerActionWithAttributes.ActionWithProducesResponseType_StatusCodeAndTypeInConstructor)).First();
            var typeCache = new ApiControllerTypeCache(compilation);

            // Act
            var result = SymbolApiResponseMetadataProvider.GetResponseMetadata(typeCache, method);

            // Assert
            Assert.Collection(
                result,
                metadata =>
                {
                    Assert.Equal(202, metadata.StatusCode);
                    Assert.NotNull(metadata.Attribute);
                    Assert.Null(metadata.Convention);
                });
        }

        [Fact]
        public async Task GetResponseMetadata_ReturnsValueFromProducesResponseType_WhenStatusCodeIsSpecifiedInConstructorAndProperty()
        {
            // Arrange
            var compilation = await GetResponseMetadataCompilation();
            var controller = compilation.GetTypeByMetadataName($"{Namespace}.{nameof(GetResponseMetadata_ControllerActionWithAttributes)}");
            var method = (IMethodSymbol)controller.GetMembers(nameof(GetResponseMetadata_ControllerActionWithAttributes.ActionWithProducesResponseType_StatusCodeInConstructorAndProperty)).First();
            var typeCache = new ApiControllerTypeCache(compilation);

            // Act
            var result = SymbolApiResponseMetadataProvider.GetResponseMetadata(typeCache, method);

            // Assert
            Assert.Collection(
                result,
                metadata =>
                {
                    Assert.Equal(203, metadata.StatusCode);
                    Assert.NotNull(metadata.Attribute);
                    Assert.Null(metadata.Convention);
                });
        }

        [Fact]
        public async Task GetResponseMetadata_ReturnsValueFromProducesResponseType_WhenStatusCodeAndTypeIsSpecifiedInConstructorAndProperty()
        {
            // Arrange
            var compilation = await GetResponseMetadataCompilation();
            var controller = compilation.GetTypeByMetadataName($"{Namespace}.{nameof(GetResponseMetadata_ControllerActionWithAttributes)}");
            var method = (IMethodSymbol)controller.GetMembers(nameof(GetResponseMetadata_ControllerActionWithAttributes.ActionWithProducesResponseType_StatusCodeAndTypeInConstructorAndProperty)).First();
            var typeCache = new ApiControllerTypeCache(compilation);

            // Act
            var result = SymbolApiResponseMetadataProvider.GetResponseMetadata(typeCache, method);

            // Assert
            Assert.Collection(
                result,
                metadata =>
                {
                    Assert.Equal(201, metadata.StatusCode);
                    Assert.NotNull(metadata.Attribute);
                    Assert.Null(metadata.Convention);
                });
        }

        [Fact]
        public async Task GetResponseMetadata_ReturnsValueFromCustomProducesResponseType()
        {
            // Arrange
            var compilation = await GetResponseMetadataCompilation();
            var controller = compilation.GetTypeByMetadataName($"{Namespace}.{nameof(GetResponseMetadata_ControllerActionWithAttributes)}");
            var method = (IMethodSymbol)controller.GetMembers(nameof(GetResponseMetadata_ControllerActionWithAttributes.ActionWithCustomProducesResponseTypeAttributeWithArguments)).First();
            var typeCache = new ApiControllerTypeCache(compilation);

            // Act
            var result = SymbolApiResponseMetadataProvider.GetResponseMetadata(typeCache, method);

            // Assert
            Assert.Collection(
                result,
                metadata =>
                {
                    Assert.Equal(201, metadata.StatusCode);
                    Assert.NotNull(metadata.Attribute);
                    Assert.Null(metadata.Convention);
                });
        }

        [Fact]
        public async Task GetResponseMetadata_IgnoresCustomResponseTypeMetadataProvider()
        {
            // Arrange
            var compilation = await GetResponseMetadataCompilation();
            var controller = compilation.GetTypeByMetadataName($"{Namespace}.{nameof(GetResponseMetadata_ControllerActionWithAttributes)}");
            var method = (IMethodSymbol)controller.GetMembers(nameof(GetResponseMetadata_ControllerActionWithAttributes.ActionWithCustomApiResponseMetadataProvider)).First();
            var typeCache = new ApiControllerTypeCache(compilation);

            // Act
            var result = SymbolApiResponseMetadataProvider.GetResponseMetadata(typeCache, method);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public Task GetResponseMetadata_IgnoresAttributesWithIncorrectStatusCodeType()
        {
            return GetResponseMetadata_IgnoresInvalidOrUnsupportedAttribues(
                nameof(GetResponseMetadata_ControllerActionWithAttributes),
                nameof(GetResponseMetadata_ControllerActionWithAttributes.ActionWithProducesResponseTypeWithIncorrectStatusCodeType));
        }

        [Fact]
        public Task GetResponseMetadata_IgnoresDerivedAttributesWithoutPropertyOnConstructorArguments()
        {
            return GetResponseMetadata_IgnoresInvalidOrUnsupportedAttribues(
                nameof(GetResponseMetadata_ControllerActionWithAttributes),
                nameof(GetResponseMetadata_ControllerActionWithAttributes.ActionWithCustomProducesResponseTypeAttributeWithoutArguments));
        }

        private async Task GetResponseMetadata_IgnoresInvalidOrUnsupportedAttribues(string typeName, string methodName)
        {
            // Arrange
            var compilation = await GetResponseMetadataCompilation();
            var controller = compilation.GetTypeByMetadataName($"{Namespace}.{typeName}");
            var method = (IMethodSymbol)controller.GetMembers(methodName).First();
            var typeCache = new ApiControllerTypeCache(compilation);

            // Act
            var result = SymbolApiResponseMetadataProvider.GetResponseMetadata(typeCache, method);

            // Assert
            Assert.Collection(
                result,
                metadata =>
                {
                    Assert.Equal(200, metadata.StatusCode);
                    Assert.NotNull(metadata.Attribute);
                    Assert.Null(metadata.Convention);
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

        private Task<Compilation> GetResponseMetadataCompilation() => GetCompilation("GetResponseMetadataTests");

        private Task<Compilation> GetCompilation(string test)
        {
            var testSource = MvcTestSource.Read(GetType().Name, test);
            var project = DiagnosticProject.Create(GetType().Assembly, new[] { testSource.Source });

            return project.GetCompilationAsync();
        }
    }
}
