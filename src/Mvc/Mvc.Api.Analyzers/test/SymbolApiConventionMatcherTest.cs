// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Analyzer.Testing;
using Microsoft.CodeAnalysis;
using Xunit;
using static Microsoft.AspNetCore.Mvc.Api.Analyzers.SymbolApiConventionMatcher;

namespace Microsoft.AspNetCore.Mvc.Api.Analyzers
{
    public class SymbolApiConventionMatcherTest
    {
        private static readonly string BaseTypeName = typeof(Base).FullName;
        private static readonly string DerivedTypeName = typeof(Derived).FullName;
        private static readonly string TestControllerName = typeof(TestController).FullName;
        private static readonly string TestConventionName = typeof(TestConvention).FullName;

        [Theory]
        [InlineData("Method", "method")]
        [InlineData("Method", "ConventionMethod")]
        [InlineData("p", "model")]
        [InlineData("person", "model")]
        public void IsNameMatch_WithAny_AlwaysReturnsTrue(string name, string conventionName)
        {
            // Act
            var result = IsNameMatch(name, conventionName, SymbolApiConventionNameMatchBehavior.Any);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsNameMatch_WithExact_ReturnsFalse_IfNamesDifferInCase()
        {
            // Arrange
            var name = "Name";
            var conventionName = "name";

            // Act
            var result = IsNameMatch(name, conventionName, SymbolApiConventionNameMatchBehavior.Exact);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsNameMatch_WithExact_ReturnsFalse_IfNamesAreDifferent()
        {
            // Arrange
            var name = "Name";
            var conventionName = "Different";

            // Act
            var result = IsNameMatch(name, conventionName, SymbolApiConventionNameMatchBehavior.Exact);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsNameMatch_WithExact_ReturnsFalse_IfConventionNameIsSubString()
        {
            // Arrange
            var name = "RegularName";
            var conventionName = "Regular";

            // Act
            var result = IsNameMatch(name, conventionName, SymbolApiConventionNameMatchBehavior.Exact);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsNameMatch_WithExact_ReturnsFalse_IfConventionNameIsSuperString()
        {
            // Arrange
            var name = "Regular";
            var conventionName = "RegularName";

            // Act
            var result = IsNameMatch(name, conventionName, SymbolApiConventionNameMatchBehavior.Exact);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsNameMatch_WithExact_ReturnsTrue_IfExactMatch()
        {
            // Arrange
            var name = "parameterName";
            var conventionName = "parameterName";

            // Act
            var result = IsNameMatch(name, conventionName, SymbolApiConventionNameMatchBehavior.Exact);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsNameMatch_WithPrefix_ReturnsTrue_IfNamesAreExact()
        {
            // Arrange
            var name = "PostPerson";
            var conventionName = "PostPerson";

            // Act
            var result = IsNameMatch(name, conventionName, SymbolApiConventionNameMatchBehavior.Prefix);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsNameMatch_WithPrefix_ReturnsTrue_IfNameIsProperPrefix()
        {
            // Arrange
            var name = "PostPerson";
            var conventionName = "Post";

            // Act
            var result = IsNameMatch(name, conventionName, SymbolApiConventionNameMatchBehavior.Prefix);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsNameMatch_WithPrefix_ReturnsFalse_IfNamesAreDifferent()
        {
            // Arrange
            var name = "GetPerson";
            var conventionName = "Post";

            // Act
            var result = IsNameMatch(name, conventionName, SymbolApiConventionNameMatchBehavior.Prefix);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsNameMatch_WithPrefix_ReturnsFalse_IfNamesDifferInCase()
        {
            // Arrange
            var name = "GetPerson";
            var conventionName = "post";

            // Act
            var result = IsNameMatch(name, conventionName, SymbolApiConventionNameMatchBehavior.Prefix);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsNameMatch_WithPrefix_ReturnsFalse_IfNameIsNotProperPrefix()
        {
            // Arrange
            var name = "Postman";
            var conventionName = "Post";

            // Act
            var result = IsNameMatch(name, conventionName, SymbolApiConventionNameMatchBehavior.Prefix);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsNameMatch_WithPrefix_ReturnsFalse_IfNameIsSuffix()
        {
            // Arrange
            var name = "GoPost";
            var conventionName = "Post";

            // Act
            var result = IsNameMatch(name, conventionName, SymbolApiConventionNameMatchBehavior.Prefix);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsNameMatch_WithSuffix_ReturnsFalse_IfNamesAreDifferent()
        {
            // Arrange
            var name = "name";
            var conventionName = "diff";

            // Act
            var result = IsNameMatch(name, conventionName, SymbolApiConventionNameMatchBehavior.Suffix);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsNameMatch_WithSuffix_ReturnsFalse_IfNameIsNotSuffix()
        {
            // Arrange
            var name = "personId";
            var conventionName = "idx";

            // Act
            var result = IsNameMatch(name, conventionName, SymbolApiConventionNameMatchBehavior.Suffix);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsNameMatch_WithSuffix_ReturnTrue_IfNameIsExact()
        {
            // Arrange
            var name = "test";
            var conventionName = "test";

            // Act
            var result = IsNameMatch(name, conventionName, SymbolApiConventionNameMatchBehavior.Suffix);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsNameMatch_WithSuffix_ReturnFalse_IfNameDiffersInCase()
        {
            // Arrange
            var name = "test";
            var conventionName = "Test";

            // Act
            var result = IsNameMatch(name, conventionName, SymbolApiConventionNameMatchBehavior.Suffix);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsNameMatch_WithSuffix_ReturnTrue_IfNameIsProperSuffix()
        {
            // Arrange
            var name = "personId";
            var conventionName = "id";

            // Act
            var result = IsNameMatch(name, conventionName, SymbolApiConventionNameMatchBehavior.Suffix);

            // Assert
            Assert.True(result);
        }

        [Theory]
        [InlineData("candid", "id")]
        [InlineData("canDid", "id")]
        public void IsNameMatch_WithSuffix_ReturnFalse_IfNameIsNotProperSuffix(string name, string conventionName)
        {
            // Act
            var result = IsNameMatch(name, conventionName, SymbolApiConventionNameMatchBehavior.Suffix);

            // Assert
            Assert.False(result);
        }

        [Theory]
        [InlineData(typeof(object), typeof(object))]
        [InlineData(typeof(int), typeof(void))]
        [InlineData(typeof(string), typeof(DateTime))]
        public async Task IsTypeMatch_WithAny_ReturnsTrue(Type type, Type conventionType)
        {
            // Arrange
            var compilation = await GetCompilationAsync();
            var typeSymbol = compilation.GetTypeByMetadataName(type.FullName);
            var conventionTypeSymbol = compilation.GetTypeByMetadataName(conventionType.FullName);

            // Act
            var result = IsTypeMatch(typeSymbol, conventionTypeSymbol, SymbolApiConventionTypeMatchBehavior.Any);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task IsTypeMatch_WithAssignableFrom_ReturnsTrueForExact()
        {
            // Arrange
            var compilation = await GetCompilationAsync();

            var type = compilation.GetTypeByMetadataName(BaseTypeName);
            var conventionType = compilation.GetTypeByMetadataName(BaseTypeName);

            // Act
            var result = IsTypeMatch(type, conventionType, SymbolApiConventionTypeMatchBehavior.AssignableFrom);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task IsTypeMatch_WithAssignableFrom_ReturnsTrueForDerived()
        {
            // Arrange
            var compilation = await GetCompilationAsync();

            var type = compilation.GetTypeByMetadataName(DerivedTypeName);
            var conventionType = compilation.GetTypeByMetadataName(BaseTypeName);


            // Act
            var result = IsTypeMatch(type, conventionType, SymbolApiConventionTypeMatchBehavior.AssignableFrom);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task IsTypeMatch_WithAssignableFrom_ReturnsFalseForBaseTypes()
        {
            // Arrange
            var compilation = await GetCompilationAsync();

            var type = compilation.GetTypeByMetadataName(BaseTypeName);
            var conventionType = compilation.GetTypeByMetadataName(DerivedTypeName);

            // Act
            var result = IsTypeMatch(type, conventionType, SymbolApiConventionTypeMatchBehavior.AssignableFrom);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task IsTypeMatch_WithAssignableFrom_ReturnsFalseForUnrelated()
        {
            // Arrange
            var compilation = await GetCompilationAsync();

            var type = compilation.GetSpecialType(SpecialType.System_String);
            var conventionType = compilation.GetTypeByMetadataName(BaseTypeName);

            // Act
            var result = IsTypeMatch(type, conventionType, SymbolApiConventionTypeMatchBehavior.AssignableFrom);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public Task IsMatch_ReturnsFalse_IfMethodNamesDoNotMatch()
        {
            // Arrange
            var methodName = nameof(TestController.Get);
            var conventionMethodName = nameof(TestConvention.Post);
            var expected = false;

            return RunMatchTest(methodName, conventionMethodName, expected);
        }

        [Fact]
        public Task IsMatch_ReturnsFalse_IMethodHasMoreParametersThanConvention()
        {
            // Arrange
            var methodName = nameof(TestController.Get);
            var conventionMethodName = nameof(TestConvention.GetNoArgs);
            var expected = false;

            return RunMatchTest(methodName, conventionMethodName, expected);
        }

        [Fact]
        public Task IsMatch_ReturnsFalse_IfMethodHasFewerParametersThanConvention()
        {
            // Arrange
            var methodName = nameof(TestController.Get);
            var conventionMethodName = nameof(TestConvention.GetTwoArgs);
            var expected = false;

            return RunMatchTest(methodName, conventionMethodName, expected);
        }

        [Fact]
        public Task IsMatch_ReturnsFalse_IfParametersDoNotMatch()
        {
            // Arrange
            var methodName = nameof(TestController.Get);
            var conventionMethodName = nameof(TestConvention.GetParameterNotMatching);
            var expected = false;

            return RunMatchTest(methodName, conventionMethodName, expected);
        }

        [Fact]
        public Task IsMatch_ReturnsTrue_IfMethodNameAndParametersMatches()
        {
            // Arrange
            var methodName = nameof(TestController.Get);
            var conventionMethodName = nameof(TestConvention.Get);
            var expected = true;

            return RunMatchTest(methodName, conventionMethodName, expected);
        }

        [Fact]
        public Task IsMatch_ReturnsTrue_IfParamsArrayMatchesRemainingArguments()
        {
            // Arrange
            var methodName = nameof(TestController.Search);
            var conventionMethodName = nameof(TestConvention.Search);
            var expected = true;

            return RunMatchTest(methodName, conventionMethodName, expected);
        }

        [Fact]
        public Task IsMatch_WithEmpty_MatchesMethodWithNoParameters()
        {
            // Arrange
            var methodName = nameof(TestController.SearchEmpty);
            var conventionMethodName = nameof(TestConvention.SearchWithParams);
            var expected = true;

            return RunMatchTest(methodName, conventionMethodName, expected);
        }

        private async Task RunMatchTest(string methodName, string conventionMethodName, bool expected)
        {
            var compilation = await GetCompilationAsync();
            Assert.True(ApiControllerSymbolCache.TryCreate(compilation, out var symbolCache));

            var testController = compilation.GetTypeByMetadataName(TestControllerName);
            var testConvention = compilation.GetTypeByMetadataName(TestConventionName);
            var method = (IMethodSymbol)testController.GetMembers(methodName).First();
            var conventionMethod = (IMethodSymbol)testConvention.GetMembers(conventionMethodName).First();

            // Act
            var result = IsMatch(symbolCache, method, conventionMethod);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task GetNameMatchBehavior_ReturnsExact_WhenNoAttributesArePresent()
        {
            // Arrange
            var expected = SymbolApiConventionNameMatchBehavior.Exact;
            var compilation = await GetCompilationAsync();
            Assert.True(ApiControllerSymbolCache.TryCreate(compilation, out var symbolCache));

            var testConvention = compilation.GetTypeByMetadataName(TestConventionName);
            var method = testConvention.GetMembers(nameof(TestConvention.MethodWithoutMatchBehavior)).First();

            // Act
            var result = GetNameMatchBehavior(symbolCache, method);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task GetNameMatchBehavior_ReturnsExact_WhenNoNameMatchBehaviorAttributeIsSpecified()
        {
            // Arrange
            var expected = SymbolApiConventionNameMatchBehavior.Exact;
            var compilation = await GetCompilationAsync();
            Assert.True(ApiControllerSymbolCache.TryCreate(compilation, out var symbolCache));

            var testConvention = compilation.GetTypeByMetadataName(TestConventionName);
            var method = testConvention.GetMembers(nameof(TestConvention.MethodWithRandomAttributes)).First();

            // Act
            var result = GetNameMatchBehavior(symbolCache, method);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task GetNameMatchBehavior_ReturnsValueFromAttributes()
        {
            // Arrange
            var expected = SymbolApiConventionNameMatchBehavior.Prefix;
            var compilation = await GetCompilationAsync();
            Assert.True(ApiControllerSymbolCache.TryCreate(compilation, out var symbolCache));

            var testConvention = compilation.GetTypeByMetadataName(TestConventionName);
            var method = testConvention.GetMembers(nameof(TestConvention.Get)).First();

            // Act
            var result = GetNameMatchBehavior(symbolCache, method);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task GetTypeMatchBehavior_ReturnsIsAssignableFrom_WhenNoAttributesArePresent()
        {
            // Arrange
            var expected = SymbolApiConventionTypeMatchBehavior.AssignableFrom;
            var compilation = await GetCompilationAsync();
            Assert.True(ApiControllerSymbolCache.TryCreate(compilation, out var symbolCache));

            var testConvention = compilation.GetTypeByMetadataName(TestConventionName);
            var method = (IMethodSymbol)testConvention.GetMembers(nameof(TestConvention.Get)).First();
            var parameter = method.Parameters[0];

            // Act
            var result = GetTypeMatchBehavior(symbolCache, parameter);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task GetTypeMatchBehavior_ReturnsIsAssignableFrom_WhenNoMatchingAttributesArePresent()
        {
            // Arrange
            var expected = SymbolApiConventionTypeMatchBehavior.AssignableFrom;
            var compilation = await GetCompilationAsync();
            Assert.True(ApiControllerSymbolCache.TryCreate(compilation, out var symbolCache));

            var testConvention = compilation.GetTypeByMetadataName(TestConventionName);
            var method = (IMethodSymbol)testConvention.GetMembers(nameof(TestConvention.MethodParameterWithRandomAttributes)).First();
            var parameter = method.Parameters[0];

            // Act
            var result = GetTypeMatchBehavior(symbolCache, parameter);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task GetTypeMatchBehavior_ReturnsValueFromAttributes()
        {
            // Arrange
            var expected = SymbolApiConventionTypeMatchBehavior.Any;
            var compilation = await GetCompilationAsync();
            Assert.True(ApiControllerSymbolCache.TryCreate(compilation, out var symbolCache));

            var testConvention = compilation.GetTypeByMetadataName(TestConventionName);
            var method = (IMethodSymbol)testConvention.GetMembers(nameof(TestConvention.MethodWithAnyTypeMatchBehaviorParameter)).First();
            var parameter = method.Parameters[0];

            // Act
            var result = GetTypeMatchBehavior(symbolCache, parameter);

            // Assert
            Assert.Equal(expected, result);
        }

        private Task<Compilation> GetCompilationAsync(string test = "SymbolApiConventionMatcherTestFile")
        {
            var testSource = MvcTestSource.Read(GetType().Name, test);
            var project = MvcDiagnosticAnalyzerRunner.CreateProjectWithReferencesInBinDir(GetType().Assembly, new[] { testSource.Source });

            return project.GetCompilationAsync();
        }
    }
}