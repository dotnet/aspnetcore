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
    public async Task InspectReturnExpression_ReturnsStatusCodeFromStatusCodePropertyAssignment()
    {
        // Arrange & Act
        var actualResponseMetadata = await RunInspectReturnStatementSyntax();

        // Assert
        Assert.NotNull(actualResponseMetadata);
        Assert.Equal(201, actualResponseMetadata.Value.StatusCode);
    }

    [Fact]
    public async Task InspectReturnExpression_ReturnsStatusCodeFromConstructorAssignment()
    {
        // Arrange & Act
        var actualResponseMetadata = await RunInspectReturnStatementSyntax();

        // Assert
        Assert.NotNull(actualResponseMetadata);
        Assert.Equal(204, actualResponseMetadata.Value.StatusCode);
    }

    [Fact]
    public async Task InspectReturnExpression_ReturnsStatusCodeFromHelperMethod()
    {
        // Arrange & Act
        var actualResponseMetadata = await RunInspectReturnStatementSyntax();

        // Assert
        Assert.NotNull(actualResponseMetadata);
        Assert.Equal(302, actualResponseMetadata.Value.StatusCode);
    }

    [Fact]
    public async Task InspectReturnExpression_UsesExplicitlySpecifiedStatusCode_ForActionResultWithDefaultStatusCode()
    {
        // Arrange & Act
        var actualResponseMetadata = await RunInspectReturnStatementSyntax();

        // Assert
        Assert.NotNull(actualResponseMetadata);
        Assert.Equal(422, actualResponseMetadata.Value.StatusCode);
    }

    [Fact]
    public async Task InspectReturnExpression_ReadsStatusCodeConstant()
    {
        // Arrange & Act
        var actualResponseMetadata = await RunInspectReturnStatementSyntax();

        // Assert
        Assert.NotNull(actualResponseMetadata);
        Assert.Equal(423, actualResponseMetadata.Value.StatusCode);
    }

    [Fact]
    public async Task InspectReturnExpression_DoesNotReadLocalFieldWithConstantValue()
    {
        // This is a gap in the analyzer. We're using this to document the current behavior and not an expecation.
        // Arrange & Act
        var actualResponseMetadata = await RunInspectReturnStatementSyntax();

        // Assert
        Assert.Null(actualResponseMetadata);
    }

    [Fact]
    public async Task InspectReturnExpression_FallsBackToDefaultStatusCode_WhenAppliedStatusCodeCannotBeRead()
    {
        // This is a gap in the analyzer. We're using this to document the current behavior and not an expecation.
        // Arrange & Act
        var actualResponseMetadata = await RunInspectReturnStatementSyntax();

        // Assert
        Assert.NotNull(actualResponseMetadata);
        Assert.Equal(400, actualResponseMetadata.Value.StatusCode);
    }

    [Fact]
    public async Task InspectReturnExpression_SetsReturnType_WhenLiteralTypeIsSpecifiedInConstructor()
    {
        // Arrange & Act
        var actualResponseMetadata = await RunInspectReturnStatementSyntax();

        // Assert
        Assert.NotNull(actualResponseMetadata?.ReturnType);
        Assert.Equal("TestModel", actualResponseMetadata.Value.ReturnType.Name);
    }

    [Fact]
    public async Task InspectReturnExpression_SetsReturnType_WhenLocalValueIsSpecifiedInConstructor()
    {
        // Arrange & Act
        var actualResponseMetadata = await RunInspectReturnStatementSyntax();

        // Assert
        Assert.NotNull(actualResponseMetadata?.ReturnType);
        Assert.Equal("TestModel", actualResponseMetadata.Value.ReturnType.Name);
    }

    [Fact]
    public async Task InspectReturnExpression_SetsReturnType_WhenValueIsReturned()
    {
        // Arrange & Act
        var actualResponseMetadata = await RunInspectReturnStatementSyntax();

        // Assert
        Assert.NotNull(actualResponseMetadata?.ReturnType);
        Assert.Equal("TestModel", actualResponseMetadata.Value.ReturnType.Name);
    }

    [Fact]
    public async Task InspectReturnExpression_ReturnsNullReturnType_IfValueIsNotSpecified()
    {
        // Arrange & Act
        var actualResponseMetadata = await RunInspectReturnStatementSyntax();

        // Assert
        Assert.NotNull(actualResponseMetadata);
        Assert.Null(actualResponseMetadata.Value.ReturnType);
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

        var result = ActualApiResponseMetadataFactory.TryGetActualResponseMetadata(symbolCache, methodOperation, CancellationToken.None, out var responseMetadatas);

        return (result, responseMetadatas, testSource);
    }

    private async Task<ActualApiResponseMetadata?> RunInspectReturnStatementSyntax([CallerMemberName] string test = null)
    {
        // Arrange
        var compilation = await GetCompilation("InspectReturnExpressionTests");
        Assert.True(ApiControllerSymbolCache.TryCreate(compilation, out var symbolCache));

        var controllerType = compilation.GetTypeByMetadataName(typeof(TestFiles.InspectReturnExpressionTests.TestController).FullName);
        var syntaxTree = controllerType.DeclaringSyntaxReferences[0].SyntaxTree;

        var method = (IMethodSymbol)Assert.Single(controllerType.GetMembers(test));
        var methodSyntax = syntaxTree.GetRoot().FindNode(method.Locations[0].SourceSpan);
        var returnStatement = methodSyntax.DescendantNodes().OfType<ReturnStatementSyntax>().First();
        var returnOperation = (IReturnOperation)compilation.GetSemanticModel(syntaxTree).GetOperation(returnStatement);

        return ActualApiResponseMetadataFactory.InspectReturnOperation(
            symbolCache,
            returnOperation);
    }

    private async Task<ActualApiResponseMetadata?> RunInspectReturnStatementSyntax(string source, string test)
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
}
