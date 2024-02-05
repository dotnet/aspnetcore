// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Analyzer.Testing;
using Microsoft.AspNetCore.Mvc.Api.Analyzers.TestFiles.ActualApiResponseMetadataFactoryTest;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.AspNetCore.Mvc.Api.Analyzers;

public class ActualApiResponseMetadataFactoryTest
{
    private static readonly string Namespace = typeof(ActualApiResponseMetadataFactoryTest).Namespace;

    public enum ReturnOperationTestVariant
    {
        Default,
        SwitchExpression
    }

    [Fact]
    public async Task GetDefaultStatusCode_ReturnsValueDefinedUsingStatusCodeConstants()
    {
        // Arrange
        var compilation = await GetCompilation("GetDefaultStatusCodeTest");
        var attribute = compilation.GetTypeByMetadataName(typeof(TestActionResultUsingStatusCodesConstants).FullName).GetAttributes()[0];

        // Act
        var actual = ActualApiResponseMetadataFactory.GetDefaultStatusCode(attribute);

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
        var actual = ActualApiResponseMetadataFactory.GetDefaultStatusCode(attribute);

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
    public class TestController : ControllerBase
    {
        public IActionResult Get(int id)
        {
            return new DoesNotExist(id);
        }
    }
}";
        var project = MvcDiagnosticAnalyzerRunner.CreateProjectWithReferencesInBinDir(GetType().Assembly, new[] { source });
        var compilation = await project.GetCompilationAsync();
        Assert.True(ApiControllerSymbolCache.TryCreate(compilation, out var symbolCache));

        var returnType = compilation.GetTypeByMetadataName($"{Namespace}.TestController");
        var syntaxTree = returnType.DeclaringSyntaxReferences[0].SyntaxTree;

        var method = (IMethodSymbol)returnType.GetMembers().First();
        var methodSyntax = syntaxTree.GetRoot().FindNode(method.Locations[0].SourceSpan);
        var returnStatement = methodSyntax.DescendantNodes().OfType<ReturnStatementSyntax>().First();
        var returnOperation = (IReturnOperation)compilation.GetSemanticModel(syntaxTree).GetOperation(returnStatement);

        var actualResponseMetadata = ActualApiResponseMetadataFactory.InspectReturnOperation(
            symbolCache,
            returnOperation);

        // Assert
        var metadata = Assert.Single(actualResponseMetadata);
        Assert.Null(metadata);
    }

    [Theory]
    [InlineData(ReturnOperationTestVariant.Default)]
    [InlineData(ReturnOperationTestVariant.SwitchExpression)]
    public async Task InspectReturnExpression_ReturnsStatusCodeFromDefaultStatusCodeAttributeOnActionResult(ReturnOperationTestVariant variant)
    {
        // Arrange & Act
        var actualResponseMetadata = await RunInspectReturnStatementSyntax(variant);

        // Assert
        var metadata = Assert.Single(actualResponseMetadata);
        Assert.NotNull(metadata);
        Assert.Equal(401, metadata.Value.StatusCode);
    }

    [Theory]
    [InlineData(ReturnOperationTestVariant.Default)]
    [InlineData(ReturnOperationTestVariant.SwitchExpression)]
    public async Task InspectReturnExpression_ReturnsDefaultResponseMetadata_IfReturnedTypeIsNotActionResult(ReturnOperationTestVariant variant)
    {
        // Arrange & Act
        var actualResponseMetadata = await RunInspectReturnStatementSyntax(variant);

        // Assert
        var metadata = Assert.Single(actualResponseMetadata);
        Assert.NotNull(metadata);
        Assert.True(metadata.Value.IsDefaultResponse);
    }

    [Theory]
    [InlineData(ReturnOperationTestVariant.Default)]
    [InlineData(ReturnOperationTestVariant.SwitchExpression)]
    public async Task InspectReturnExpression_ReturnsStatusCodeFromStatusCodePropertyAssignment(ReturnOperationTestVariant variant)
    {
        // Arrange & Act
        var actualResponseMetadata = await RunInspectReturnStatementSyntax(variant);

        // Assert
        var metadata = Assert.Single(actualResponseMetadata);
        Assert.NotNull(metadata);
        Assert.Equal(201, metadata.Value.StatusCode);
    }

    [Theory]
    [InlineData(ReturnOperationTestVariant.Default)]
    [InlineData(ReturnOperationTestVariant.SwitchExpression)]
    public async Task InspectReturnExpression_ReturnsStatusCodeFromConstructorAssignment(ReturnOperationTestVariant variant)
    {
        // Arrange & Act
        var actualResponseMetadata = await RunInspectReturnStatementSyntax(variant);

        // Assert
        var metadata = Assert.Single(actualResponseMetadata);
        Assert.NotNull(metadata);
        Assert.Equal(204, metadata.Value.StatusCode);
    }

    [Theory]
    [InlineData(ReturnOperationTestVariant.Default)]
    [InlineData(ReturnOperationTestVariant.SwitchExpression)]
    public async Task InspectReturnExpression_ReturnsStatusCodeFromHelperMethod(ReturnOperationTestVariant variant)
    {
        // Arrange & Act
        var actualResponseMetadata = await RunInspectReturnStatementSyntax(variant);

        // Assert
        var metadata = Assert.Single(actualResponseMetadata);
        Assert.NotNull(metadata);
        Assert.Equal(302, metadata.Value.StatusCode);
    }

    [Theory]
    [InlineData(ReturnOperationTestVariant.Default)]
    [InlineData(ReturnOperationTestVariant.SwitchExpression)]
    public async Task InspectReturnExpression_UsesExplicitlySpecifiedStatusCode_ForActionResultWithDefaultStatusCode(ReturnOperationTestVariant variant)
    {
        // Arrange & Act
        var actualResponseMetadata = await RunInspectReturnStatementSyntax(variant);

        // Assert
        var metadata = Assert.Single(actualResponseMetadata);
        Assert.NotNull(metadata);
        Assert.Equal(422, metadata.Value.StatusCode);
    }

    [Theory]
    [InlineData(ReturnOperationTestVariant.Default)]
    [InlineData(ReturnOperationTestVariant.SwitchExpression)]
    public async Task InspectReturnExpression_ReadsStatusCodeConstant(ReturnOperationTestVariant variant)
    {
        // Arrange & Act
        var actualResponseMetadata = await RunInspectReturnStatementSyntax(variant);

        // Assert
        var metadata = Assert.Single(actualResponseMetadata);
        Assert.NotNull(metadata);
        Assert.Equal(423, metadata.Value.StatusCode);
    }

    [Theory]
    [InlineData(ReturnOperationTestVariant.Default)]
    [InlineData(ReturnOperationTestVariant.SwitchExpression)]
    public async Task InspectReturnExpression_DoesNotReadLocalFieldWithConstantValue(ReturnOperationTestVariant variant)
    {
        // This is a gap in the analyzer. We're using this to document the current behavior and not an expecation.
        // Arrange & Act
        var actualResponseMetadata = await RunInspectReturnStatementSyntax(variant);

        // Assert
        var metadata = Assert.Single(actualResponseMetadata);
        Assert.Null(metadata);
    }

    [Theory]
    [InlineData(ReturnOperationTestVariant.Default)]
    [InlineData(ReturnOperationTestVariant.SwitchExpression)]
    public async Task InspectReturnExpression_FallsBackToDefaultStatusCode_WhenAppliedStatusCodeCannotBeRead(ReturnOperationTestVariant variant)
    {
        // This is a gap in the analyzer. We're using this to document the current behavior and not an expecation.
        // Arrange & Act
        var actualResponseMetadata = await RunInspectReturnStatementSyntax(variant);

        // Assert
        var metadata = Assert.Single(actualResponseMetadata);
        Assert.NotNull(metadata);
        Assert.Equal(400, metadata.Value.StatusCode);
    }

    [Theory]
    [InlineData(ReturnOperationTestVariant.Default)]
    [InlineData(ReturnOperationTestVariant.SwitchExpression)]
    public async Task InspectReturnExpression_SetsReturnType_WhenLiteralTypeIsSpecifiedInConstructor(ReturnOperationTestVariant variant)
    {
        // Arrange & Act
        var actualResponseMetadata = await RunInspectReturnStatementSyntax(variant);

        // Assert
        var metadata = Assert.Single(actualResponseMetadata);
        Assert.NotNull(metadata?.ReturnType);
        Assert.Equal("TestModel", metadata.Value.ReturnType.Name);
    }

    [Theory]
    [InlineData(ReturnOperationTestVariant.Default)]
    [InlineData(ReturnOperationTestVariant.SwitchExpression)]
    public async Task InspectReturnExpression_SetsReturnType_WhenLocalValueIsSpecifiedInConstructor(ReturnOperationTestVariant variant)
    {
        // Arrange & Act
        var actualResponseMetadata = await RunInspectReturnStatementSyntax(variant);

        // Assert
        var metadata = Assert.Single(actualResponseMetadata);
        Assert.NotNull(metadata?.ReturnType);
        Assert.Equal("TestModel", metadata.Value.ReturnType.Name);
    }

    [Theory]
    [InlineData(ReturnOperationTestVariant.Default)]
    [InlineData(ReturnOperationTestVariant.SwitchExpression)]
    public async Task InspectReturnExpression_SetsReturnType_WhenValueIsReturned(ReturnOperationTestVariant variant)
    {
        // Arrange & Act
        var actualResponseMetadata = await RunInspectReturnStatementSyntax(variant);

        // Assert
        var metadata = Assert.Single(actualResponseMetadata);
        Assert.NotNull(metadata?.ReturnType);
        Assert.Equal("TestModel", metadata.Value.ReturnType.Name);
    }

    [Theory]
    [InlineData(ReturnOperationTestVariant.Default)]
    [InlineData(ReturnOperationTestVariant.SwitchExpression)]
    public async Task InspectReturnExpression_ReturnsNullReturnType_IfValueIsNotSpecified(ReturnOperationTestVariant variant)
    {
        // Arrange & Act
        var actualResponseMetadata = await RunInspectReturnStatementSyntax(variant);

        // Assert
        var metadata = Assert.Single(actualResponseMetadata);
        Assert.NotNull(metadata);
        Assert.Null(metadata.Value.ReturnType);
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
                AnalyzerAssert.DiagnosticLocation(testSource.MarkerLocations["MM1"], metadata.ReturnOperation.Syntax.GetLocation());
            },
            metadata =>
            {
                Assert.True(metadata.IsDefaultResponse);
                AnalyzerAssert.DiagnosticLocation(testSource.MarkerLocations["MM2"], metadata.ReturnOperation.Syntax.GetLocation());
            });
    }

    [Fact]
    public async Task TryGetActualResponseMetadata_ActionWithActionResultOfTReturningOkResultExpression()
    {
        // Arrange
        var typeName = typeof(TryGetActualResponseMetadataController).FullName;
        var methodName = nameof(TryGetActualResponseMetadataController.ActionWithActionResultOfTReturningOkResultExpression);

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

    private async Task<(bool result, IList<ActualApiResponseMetadata> responseMetadatas, TestSource testSource)> TryGetActualResponseMetadata(string typeName, string methodName)
    {
        var testSource = MvcTestSource.Read(GetType().Name, "TryGetActualResponseMetadataTests");
        var project = MvcDiagnosticAnalyzerRunner.CreateProjectWithReferencesInBinDir(GetType().Assembly, new[] { testSource.Source });

        var compilation = await GetCompilation("TryGetActualResponseMetadataTests");

        var type = compilation.GetTypeByMetadataName(typeName);
        var method = (IMethodSymbol)type.GetMembers(methodName).First();
        Assert.True(ApiControllerSymbolCache.TryCreate(compilation, out var symbolCache));

        var syntaxTree = method.DeclaringSyntaxReferences[0].SyntaxTree;
        var methodSyntax = (MethodDeclarationSyntax)syntaxTree.GetRoot().FindNode(method.Locations[0].SourceSpan);
        var methodOperation = (IMethodBodyBaseOperation)compilation.GetSemanticModel(syntaxTree).GetOperation(methodSyntax);

        var result = ActualApiResponseMetadataFactory.TryGetActualResponseMetadata(symbolCache, methodOperation, out var responseMetadatas);

        return (result, responseMetadatas, testSource);
    }

    private async Task<ActualApiResponseMetadata?[]> RunInspectReturnStatementSyntax(ReturnOperationTestVariant variant = ReturnOperationTestVariant.Default, [CallerMemberName] string test = null)
    {
        var testClassName = GetTestClassName(variant);
        var controllerTypeName = GetControllerTypeName(variant);

        var compilation = await GetCompilation(testClassName);
        Assert.True(ApiControllerSymbolCache.TryCreate(compilation, out var symbolCache));

        var controllerType = compilation.GetTypeByMetadataName(controllerTypeName);
        var syntaxTree = controllerType.DeclaringSyntaxReferences[0].SyntaxTree;

        var method = (IMethodSymbol)Assert.Single(controllerType.GetMembers(test));
        var methodSyntax = syntaxTree.GetRoot().FindNode(method.Locations[0].SourceSpan);
        var returnStatement = methodSyntax.DescendantNodes().OfType<ReturnStatementSyntax>().First();
        var returnOperation = (IReturnOperation)compilation.GetSemanticModel(syntaxTree).GetOperation(returnStatement);

        return ActualApiResponseMetadataFactory.InspectReturnOperation(
            symbolCache,
            returnOperation);
    }

    private async Task<ActualApiResponseMetadata?[]> RunInspectReturnStatementSyntax(string source, string test)
    {
        var project = MvcDiagnosticAnalyzerRunner.CreateProjectWithReferencesInBinDir(GetType().Assembly, new[] { source });
        var compilation = await project.GetCompilationAsync();
        Assert.True(ApiControllerSymbolCache.TryCreate(compilation, out var symbolCache));

        var returnType = compilation.GetTypeByMetadataName($"{Namespace}.{test}");
        var syntaxTree = returnType.DeclaringSyntaxReferences[0].SyntaxTree;

        var method = (IMethodSymbol)returnType.GetMembers().First();
        var methodSyntax = syntaxTree.GetRoot().FindNode(method.Locations[0].SourceSpan);
        var returnStatement = methodSyntax.DescendantNodes().OfType<ReturnStatementSyntax>().First();
        var returnOperation = (IReturnOperation)compilation.GetSemanticModel(syntaxTree).GetOperation(returnStatement);

        return ActualApiResponseMetadataFactory.InspectReturnOperation(
            symbolCache,
            returnOperation);
    }

    private Task<Compilation> GetCompilation(string test)
    {
        var testSource = MvcTestSource.Read(GetType().Name, test);
        var project = MvcDiagnosticAnalyzerRunner.CreateProjectWithReferencesInBinDir(GetType().Assembly, new[] { testSource.Source });

        return project.GetCompilationAsync();
    }

    private string GetTestClassName(ReturnOperationTestVariant variant)
    {
        return variant switch
        {
            ReturnOperationTestVariant.SwitchExpression => "InspectReturnExpressionTestsForSwitchExpression",
            _ => "InspectReturnExpressionTests",
        };
    }

    private string GetControllerTypeName(ReturnOperationTestVariant variant)
    {
        var controllerType = variant switch
        {
            ReturnOperationTestVariant.SwitchExpression => typeof(TestFiles.InspectReturnExpressionTestsForSwitchExpression.TestController),
            _ => typeof(TestFiles.InspectReturnExpressionTests.TestController),
        };

        return controllerType.FullName;
    }
}
