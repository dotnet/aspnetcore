// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.Api.Analyzers.TestFiles.SymbolApiResponseMetadataProviderTest;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Mvc.Api.Analyzers;

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
        Assert.True(ApiControllerSymbolCache.TryCreate(compilation, out var symbolCache));

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
        Assert.True(ApiControllerSymbolCache.TryCreate(compilation, out var symbolCache));

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
        Assert.True(ApiControllerSymbolCache.TryCreate(compilation, out var symbolCache));

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
        Assert.True(ApiControllerSymbolCache.TryCreate(compilation, out var symbolCache));

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
        Assert.True(ApiControllerSymbolCache.TryCreate(compilation, out var symbolCache));

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
        Assert.True(ApiControllerSymbolCache.TryCreate(compilation, out var symbolCache));

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
        Assert.True(ApiControllerSymbolCache.TryCreate(compilation, out var symbolCache));

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
        Assert.True(ApiControllerSymbolCache.TryCreate(compilation, out var symbolCache));

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
        Assert.True(ApiControllerSymbolCache.TryCreate(compilation, out var symbolCache));

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
    public async Task GetResponseMetadata_WithProducesResponseTypeAndApiConventionMethod()
    {
        // Arrange
        var compilation = await GetResponseMetadataCompilation();
        var controller = compilation.GetTypeByMetadataName($"{Namespace}.{nameof(GetResponseMetadata_ControllerActionWithAttributes)}");
        var method = (IMethodSymbol)controller.GetMembers(nameof(GetResponseMetadata_ControllerActionWithAttributes.GetResponseMetadata_WithProducesResponseTypeAndApiConventionMethod)).First();
        Assert.True(ApiControllerSymbolCache.TryCreate(compilation, out var symbolCache));

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
        Assert.True(ApiControllerSymbolCache.TryCreate(compilation, out var symbolCache));

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
        Assert.True(ApiControllerSymbolCache.TryCreate(compilation, out var symbolCache));

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
    public async Task GetDeclaredResponseMetadata_ApiConventionTypeAttributeOnType_Works()
    {
        // Arrange
        var type = typeof(GetDeclaredResponseMetadata_ApiConventionTypeAttributeOnType);
        var compilation = await GetResponseMetadataCompilation();
        var controller = compilation.GetTypeByMetadataName(type.FullName);
        var method = (IMethodSymbol)controller.GetMembers().First();
        Assert.True(ApiControllerSymbolCache.TryCreate(compilation, out var symbolCache));

        // Act
        var result = SymbolApiResponseMetadataProvider.GetDeclaredResponseMetadata(symbolCache, method);

        // Assert
        // We should expect 3 entries specified by DefaultApiConventions.Post
        Assert.Collection(
            result.OrderBy(r => r.StatusCode),
            metadata => Assert.True(metadata.IsDefault),
            metadata => Assert.Equal(201, metadata.StatusCode),
            metadata => Assert.Equal(400, metadata.StatusCode));
    }

    [Fact]
    public async Task GetDeclaredResponseMetadata_ApiConventionTypeAttributeOnBaseType_Works()
    {
        // Arrange
        var type = typeof(GetDeclaredResponseMetadata_ApiConventionTypeAttributeOnBaseType);
        var compilation = await GetResponseMetadataCompilation();
        var controller = compilation.GetTypeByMetadataName(type.FullName);
        var method = (IMethodSymbol)controller.GetMembers().First();
        Assert.True(ApiControllerSymbolCache.TryCreate(compilation, out var symbolCache));

        // Act
        var result = SymbolApiResponseMetadataProvider.GetDeclaredResponseMetadata(symbolCache, method);

        // Assert
        // We should expect 3 entries specified by DefaultApiConventions.Post
        Assert.Collection(
            result.OrderBy(r => r.StatusCode),
            metadata => Assert.True(metadata.IsDefault),
            metadata => Assert.Equal(201, metadata.StatusCode),
            metadata => Assert.Equal(400, metadata.StatusCode));
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
    public async Task GetErrorResponseType_ReturnsProblemDetails_IfNoAttributeIsDiscovered()
    {
        // Arrange
        var compilation = await GetCompilation(nameof(GetErrorResponseType_ReturnsProblemDetails_IfNoAttributeIsDiscovered));
        var expected = compilation.GetTypeByMetadataName(typeof(ProblemDetails).FullName);

        var type = compilation.GetTypeByMetadataName(typeof(GetErrorResponseType_ReturnsProblemDetails_IfNoAttributeIsDiscoveredController).FullName);
        var method = (IMethodSymbol)type.GetMembers("Action").First();
        Assert.True(ApiControllerSymbolCache.TryCreate(compilation, out var symbolCache));

        // Act
        var result = SymbolApiResponseMetadataProvider.GetErrorResponseType(symbolCache, method);

        // Assert
        Assert.Same(expected, result);
    }

    [Fact]
    public async Task GetErrorResponseType_ReturnsTypeDefinedAtAssembly()
    {
        // Arrange
        var compilation = await GetCompilation(nameof(GetErrorResponseType_ReturnsTypeDefinedAtAssembly));
        var expected = compilation.GetTypeByMetadataName(typeof(GetErrorResponseType_ReturnsTypeDefinedAtAssemblyModel).FullName);

        var type = compilation.GetTypeByMetadataName(typeof(GetErrorResponseType_ReturnsTypeDefinedAtAssemblyController).FullName);
        var method = (IMethodSymbol)type.GetMembers("Action").First();
        Assert.True(ApiControllerSymbolCache.TryCreate(compilation, out var symbolCache));

        // Act
        var result = SymbolApiResponseMetadataProvider.GetErrorResponseType(symbolCache, method);

        // Assert
        Assert.Same(expected, result);
    }

    [Fact]
    public async Task GetErrorResponseType_ReturnsTypeDefinedAtController()
    {
        // Arrange
        var compilation = await GetCompilation(nameof(GetErrorResponseType_ReturnsTypeDefinedAtController));
        var expected = compilation.GetTypeByMetadataName(typeof(GetErrorResponseType_ReturnsTypeDefinedAtControllerModel).FullName);

        var type = compilation.GetTypeByMetadataName(typeof(GetErrorResponseType_ReturnsTypeDefinedAtControllerController).FullName);
        var method = (IMethodSymbol)type.GetMembers("Action").First();
        Assert.True(ApiControllerSymbolCache.TryCreate(compilation, out var symbolCache));

        // Act
        var result = SymbolApiResponseMetadataProvider.GetErrorResponseType(symbolCache, method);

        // Assert
        Assert.Same(expected, result);
    }

    [Fact]
    public async Task GetErrorResponseType_ReturnsTypeDefinedAtAction()
    {
        // Arrange
        var compilation = await GetCompilation(nameof(GetErrorResponseType_ReturnsTypeDefinedAtAction));
        var expected = compilation.GetTypeByMetadataName(typeof(GetErrorResponseType_ReturnsTypeDefinedAtActionModel).FullName);

        var type = compilation.GetTypeByMetadataName(typeof(GetErrorResponseType_ReturnsTypeDefinedAtActionController).FullName);
        var method = (IMethodSymbol)type.GetMembers("Action").First();
        Assert.True(ApiControllerSymbolCache.TryCreate(compilation, out var symbolCache));

        // Act
        var result = SymbolApiResponseMetadataProvider.GetErrorResponseType(symbolCache, method);

        // Assert
        Assert.Same(expected, result);
    }

    private Task<Compilation> GetResponseMetadataCompilation() => GetCompilation("GetResponseMetadataTests");

    private Task<Compilation> GetCompilation(string test)
    {
        var testSource = MvcTestSource.Read(GetType().Name, test);
        var project = MvcDiagnosticAnalyzerRunner.CreateProjectWithReferencesInBinDir(GetType().Assembly, new[] { testSource.Source });

        return project.GetCompilationAsync();
    }
}
