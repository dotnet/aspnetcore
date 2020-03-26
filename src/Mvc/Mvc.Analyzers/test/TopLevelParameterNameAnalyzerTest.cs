// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Analyzer.Testing;
using Microsoft.AspNetCore.Mvc.Analyzers.TopLevelParameterNameAnalyzerTestFiles;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Analyzers
{
    public class TopLevelParameterNameAnalyzerTest
    {
        private MvcDiagnosticAnalyzerRunner Runner { get; } = new MvcDiagnosticAnalyzerRunner(new TopLevelParameterNameAnalyzer());

        [Fact]
        public Task DiagnosticsAreReturned_ForControllerActionsWithParametersThatMatchProperties()
            => RunTest(nameof(DiagnosticsAreReturned_ForControllerActionsWithParametersThatMatchPropertiesModel), "model");

        [Fact]
        public Task DiagnosticsAreReturned_ForModelBoundParameters()
            => RunTest(nameof(DiagnosticsAreReturned_ForModelBoundParametersModel), "value");

        [Fact]
        public Task DiagnosticsAreReturned_IfModelNameProviderIsUsedToModifyParameterName()
            => RunTest(nameof(DiagnosticsAreReturned_IfModelNameProviderIsUsedToModifyParameterNameModel), "parameter");

        [Fact]
        public Task NoDiagnosticsAreReturnedForApiControllers()
            => RunNoDiagnosticsAreReturned();

        [Fact]
        public Task NoDiagnosticsAreReturnedIfParameterIsRenamedUsingBindingAttribute()
            => RunNoDiagnosticsAreReturned();

        [Fact]
        public Task NoDiagnosticsAreReturnedForNonActions()
            => RunNoDiagnosticsAreReturned();

        [Fact]
        public async Task IsProblematicParameter_ReturnsTrue_IfParameterNameIsTheSameAsModelProperty()
        {
            var result = await IsProblematicParameterTest();
            Assert.True(result);
        }

        [Fact]
        public async Task IsProblematicParameter_ReturnsTrue_IfParameterNameWithBinderAttributeIsTheSameNameAsModelProperty()
        {
            var result = await IsProblematicParameterTest();
            Assert.True(result);
        }

        [Fact]
        public async Task IsProblematicParameter_ReturnsTrue_IfPropertyWithModelBindingAttributeHasSameNameAsParameter()
        {
            var result = await IsProblematicParameterTest();
            Assert.True(result);
        }

        [Fact]
        public async Task IsProblematicParameter_ReturnsTrue_IfModelBinderAttributeIsUsedToRenameParameter()
        {
            var result = await IsProblematicParameterTest();
            Assert.True(result);
        }

        [Fact]
        public async Task IsProblematicParameter_ReturnsFalse_IfBindingSourceAttributeIsUsedToRenameProperty()
        {
            var result = await IsProblematicParameterTest();
            Assert.False(result);
        }

        [Fact]
        public async Task IsProblematicParameter_ReturnsFalse_IfBindingSourceAttributeIsUsedToRenameParameter()
        {
            var result = await IsProblematicParameterTest();
            Assert.False(result);
        }

        [Fact]
        public async Task IsProblematicParameter_ReturnsFalse_ForFromBodyParameter()
        {
            var result = await IsProblematicParameterTest();
            Assert.False(result);
        }

        [Fact]
        public async Task IsProblematicParameter_ReturnsFalse_ForParametersWithCustomModelBinder()
        {
            var result = await IsProblematicParameterTest();
            Assert.False(result);
        }

        // Test for https://github.com/dotnet/aspnetcore/issues/6945
        [Fact]
        public async Task IsProblematicParameter_ReturnsFalse_ForSimpleTypes()
        {
            var testName = nameof(IsProblematicParameter_ReturnsFalse_ForSimpleTypes);
            var testSource = MvcTestSource.Read(GetType().Name, testName);
            var project = DiagnosticProject.Create(GetType().Assembly, new[] { testSource.Source });

            var compilation = await project.GetCompilationAsync();

            var modelType = compilation.GetTypeByMetadataName($"Microsoft.AspNetCore.Mvc.Analyzers.TopLevelParameterNameAnalyzerTestFiles.{testName}");
            var method = (IMethodSymbol)modelType.GetMembers("ActionMethod").First();

            Assert.True(TopLevelParameterNameAnalyzer.SymbolCache.TryCreate(compilation, out var symbolCache));

            Assert.Collection(
                method.Parameters,
                p => Assert.False(TopLevelParameterNameAnalyzer.IsProblematicParameter(symbolCache, p)),
                p => Assert.False(TopLevelParameterNameAnalyzer.IsProblematicParameter(symbolCache, p)),
                p => Assert.False(TopLevelParameterNameAnalyzer.IsProblematicParameter(symbolCache, p)),
                p => Assert.False(TopLevelParameterNameAnalyzer.IsProblematicParameter(symbolCache, p)),
                p => Assert.False(TopLevelParameterNameAnalyzer.IsProblematicParameter(symbolCache, p)));
        }

        [Fact]
        public async Task IsProblematicParameter_IgnoresStaticProperties()
        {
            var result = await IsProblematicParameterTest();
            Assert.False(result);
        }

        [Fact]
        public async Task IsProblematicParameter_IgnoresFields()
        {
            var result = await IsProblematicParameterTest();
            Assert.False(result);
        }

        [Fact]
        public async Task IsProblematicParameter_IgnoresMethods()
        {
            var result = await IsProblematicParameterTest();
            Assert.False(result);
        }

        [Fact]
        public async Task IsProblematicParameter_IgnoresNonPublicProperties()
        {
            var result = await IsProblematicParameterTest();
            Assert.False(result);
        }

        private async Task<bool> IsProblematicParameterTest([CallerMemberName] string testMethod = "")
        {
            var testSource = MvcTestSource.Read(GetType().Name, testMethod);
            var project = MvcDiagnosticAnalyzerRunner.CreateProjectWithReferencesInBinDir(GetType().Assembly, new[] { testSource.Source });

            var compilation = await project.GetCompilationAsync();

            var modelType = compilation.GetTypeByMetadataName($"Microsoft.AspNetCore.Mvc.Analyzers.TopLevelParameterNameAnalyzerTestFiles.{testMethod}");
            var method = (IMethodSymbol)modelType.GetMembers("ActionMethod").First();
            var parameter = method.Parameters[0];

            Assert.True(TopLevelParameterNameAnalyzer.SymbolCache.TryCreate(compilation, out var symbolCache));

            var result = TopLevelParameterNameAnalyzer.IsProblematicParameter(symbolCache, parameter);
            return result;
        }

        [Fact]
        public async Task GetName_ReturnsValueFromFirstAttributeWithValue()
        {
            var methodName = nameof(GetNameTests.SingleAttribute);
            var compilation = await GetCompilationForGetName();
            Assert.True(TopLevelParameterNameAnalyzer.SymbolCache.TryCreate(compilation, out var symbolCache));

            var type = compilation.GetTypeByMetadataName(typeof(GetNameTests).FullName);
            var method = (IMethodSymbol)type.GetMembers(methodName).First();

            var parameter = method.Parameters[0];
            var name = TopLevelParameterNameAnalyzer.GetName(symbolCache, parameter);

            Assert.Equal("testModelName", name);
        }

        [Fact]
        public async Task GetName_ReturnsName_IfNoAttributesAreSpecified()
        {
            var methodName = nameof(GetNameTests.NoAttribute);
            var compilation = await GetCompilationForGetName();
            Assert.True(TopLevelParameterNameAnalyzer.SymbolCache.TryCreate(compilation, out var symbolCache));

            var type = compilation.GetTypeByMetadataName(typeof(GetNameTests).FullName);
            var method = (IMethodSymbol)type.GetMembers(methodName).First();

            var parameter = method.Parameters[0];
            var name = TopLevelParameterNameAnalyzer.GetName(symbolCache, parameter);

            Assert.Equal("param", name);
        }

        [Fact]
        public async Task GetName_ReturnsName_IfAttributeDoesNotSpecifyName()
        {
            var methodName = nameof(GetNameTests.SingleAttributeWithoutName);
            var compilation = await GetCompilationForGetName();
            Assert.True(TopLevelParameterNameAnalyzer.SymbolCache.TryCreate(compilation, out var symbolCache));

            var type = compilation.GetTypeByMetadataName(typeof(GetNameTests).FullName);
            var method = (IMethodSymbol)type.GetMembers(methodName).First();

            var parameter = method.Parameters[0];
            var name = TopLevelParameterNameAnalyzer.GetName(symbolCache, parameter);

            Assert.Equal("param", name);
        }

        [Fact]
        public async Task GetName_ReturnsFirstName_IfMultipleAttributesAreSpecified()
        {
            var methodName = nameof(GetNameTests.MultipleAttributes);
            var compilation = await GetCompilationForGetName();
            Assert.True(TopLevelParameterNameAnalyzer.SymbolCache.TryCreate(compilation, out var symbolCache));

            var type = compilation.GetTypeByMetadataName(typeof(GetNameTests).FullName);
            var method = (IMethodSymbol)type.GetMembers(methodName).First();

            var parameter = method.Parameters[0];
            var name = TopLevelParameterNameAnalyzer.GetName(symbolCache, parameter);

            Assert.Equal("name1", name);
        }

        private async Task<Compilation> GetCompilationForGetName()
        {
            var testSource = MvcTestSource.Read(GetType().Name, "GetNameTests");
            var project = MvcDiagnosticAnalyzerRunner.CreateProjectWithReferencesInBinDir(GetType().Assembly, new[] { testSource.Source });

            var compilation = await project.GetCompilationAsync();
            return compilation;
        }

        [Fact]
        public async Task SpecifiesModelType_ReturnsFalse_IfModelBinderDoesNotSpecifyType()
        {
            var testMethod = nameof(SpecifiesModelType_ReturnsFalse_IfModelBinderDoesNotSpecifyType);
            var testSource = MvcTestSource.Read(GetType().Name, "SpecifiesModelTypeTests");
            var project = MvcDiagnosticAnalyzerRunner.CreateProjectWithReferencesInBinDir(GetType().Assembly, new[] { testSource.Source });

            var compilation = await project.GetCompilationAsync();
            Assert.True(TopLevelParameterNameAnalyzer.SymbolCache.TryCreate(compilation, out var symbolCache));

            var type = compilation.GetTypeByMetadataName(typeof(SpecifiesModelTypeTests).FullName);
            var method = (IMethodSymbol)type.GetMembers(testMethod).First();

            var parameter = method.Parameters[0];
            var result = TopLevelParameterNameAnalyzer.SpecifiesModelType(symbolCache, parameter);
            Assert.False(result);
        }

        [Fact]
        public async Task SpecifiesModelType_ReturnsTrue_IfModelBinderSpecifiesTypeFromConstructor()
        {
            var testMethod = nameof(SpecifiesModelType_ReturnsTrue_IfModelBinderSpecifiesTypeFromConstructor);
            var testSource = MvcTestSource.Read(GetType().Name, "SpecifiesModelTypeTests");
            var project = MvcDiagnosticAnalyzerRunner.CreateProjectWithReferencesInBinDir(GetType().Assembly, new[] { testSource.Source });

            var compilation = await project.GetCompilationAsync();
            Assert.True(TopLevelParameterNameAnalyzer.SymbolCache.TryCreate(compilation, out var symbolCache));

            var type = compilation.GetTypeByMetadataName(typeof(SpecifiesModelTypeTests).FullName);
            var method = (IMethodSymbol)type.GetMembers(testMethod).First();

            var parameter = method.Parameters[0];
            var result = TopLevelParameterNameAnalyzer.SpecifiesModelType(symbolCache, parameter);
            Assert.True(result);
        }

        [Fact]
        public async Task SpecifiesModelType_ReturnsTrue_IfModelBinderSpecifiesTypeFromProperty()
        {
            var testMethod = nameof(SpecifiesModelType_ReturnsTrue_IfModelBinderSpecifiesTypeFromProperty);
            var testSource = MvcTestSource.Read(GetType().Name, "SpecifiesModelTypeTests");
            var project = MvcDiagnosticAnalyzerRunner.CreateProjectWithReferencesInBinDir(GetType().Assembly, new[] { testSource.Source });

            var compilation = await project.GetCompilationAsync();
            Assert.True(TopLevelParameterNameAnalyzer.SymbolCache.TryCreate(compilation, out var symbolCache));

            var type = compilation.GetTypeByMetadataName(typeof(SpecifiesModelTypeTests).FullName);
            var method = (IMethodSymbol)type.GetMembers(testMethod).First();

            var parameter = method.Parameters[0];
            var result = TopLevelParameterNameAnalyzer.SpecifiesModelType(symbolCache, parameter);
            Assert.True(result);
        }

        private async Task RunNoDiagnosticsAreReturned([CallerMemberName] string testMethod = "")
        {
            // Arrange
            var testSource = MvcTestSource.Read(GetType().Name, testMethod);
            var expectedLocation = testSource.DefaultMarkerLocation;

            // Act
            var result = await Runner.GetDiagnosticsAsync(testSource.Source);

            // Assert
            Assert.Empty(result);
        }

        private async Task RunTest(string typeName, string parameterName, [CallerMemberName] string testMethod = "")
        {
            // Arrange
            var descriptor = DiagnosticDescriptors.MVC1004_ParameterNameCollidesWithTopLevelProperty;
            var testSource = MvcTestSource.Read(GetType().Name, testMethod);
            var expectedLocation = testSource.DefaultMarkerLocation;

            // Act
            var result = await Runner.GetDiagnosticsAsync(testSource.Source);

            // Assert
            Assert.Collection(
                result,
                diagnostic =>
                {
                    Assert.Equal(descriptor.Id, diagnostic.Id);
                    Assert.Same(descriptor, diagnostic.Descriptor);
                    AnalyzerAssert.DiagnosticLocation(expectedLocation, diagnostic.Location);
                    Assert.Equal(string.Format(descriptor.MessageFormat.ToString(), typeName, parameterName), diagnostic.GetMessage());
                });
        }
    }
}
