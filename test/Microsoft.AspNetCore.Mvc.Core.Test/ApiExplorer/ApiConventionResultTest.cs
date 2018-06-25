// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Reflection;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.ApiExplorer
{
    public class ApiConventionResultTest
    {
        [Fact]
        public void GetApiConvention_ReturnsNull_IfNoConventionMatches()
        {
            // Arrange
            var method = typeof(GetApiConvention_ReturnsNull_IfNoConventionMatchesController).GetMethod(nameof(GetApiConvention_ReturnsNull_IfNoConventionMatchesController.NoMatch));
            var attribute = new ApiConventionTypeAttribute(typeof(DefaultApiConventions));

            // Act
            var result = ApiConventionResult.TryGetApiConvention(method, new[] { attribute }, out var conventionResult);

            // Assert
            Assert.False(result);
            Assert.Null(conventionResult);
        }

        public class GetApiConvention_ReturnsNull_IfNoConventionMatchesController
        {
            public IActionResult NoMatch(int id) => null;
        }

        [Fact]
        public void GetApiConvention_ReturnsResultFromConvention()
        {
            // Arrange
            var method = typeof(GetApiConvention_ReturnsResultFromConventionController)
                .GetMethod(nameof(GetApiConvention_ReturnsResultFromConventionController.Match));
            var attribute = new ApiConventionTypeAttribute(typeof(GetApiConvention_ReturnsResultFromConventionType));

            // Act
            var result = ApiConventionResult.TryGetApiConvention(method, new[] { attribute }, out var conventionResult);

            // Assert
            Assert.True(result);
            Assert.Collection(
                conventionResult.ResponseMetadataProviders.OrderBy(o => o.StatusCode),
                r => Assert.Equal(201, r.StatusCode),
                r => Assert.Equal(403, r.StatusCode));
        }

        public class GetApiConvention_ReturnsResultFromConventionController
        {
            public IActionResult Match(int id) => null;
        }

        public static class GetApiConvention_ReturnsResultFromConventionType
        {
            [ProducesResponseType(200)]
            [ProducesResponseType(202)]
            [ProducesResponseType(404)]
            public static void Get(int id) { }

            [ProducesResponseType(201)]
            [ProducesResponseType(403)]
            public static void Match(int id) { }
        }

        [Fact]
        public void GetApiConvention_ReturnsResultFromFirstMatchingConvention()
        {
            // Arrange
            var method = typeof(GetApiConvention_ReturnsResultFromFirstMatchingConventionController)
                .GetMethod(nameof(GetApiConvention_ReturnsResultFromFirstMatchingConventionController.Get));
            var attributes = new[]
            {
                new ApiConventionTypeAttribute(typeof(GetApiConvention_ReturnsResultFromConventionType)),
                new ApiConventionTypeAttribute(typeof(DefaultApiConventions)),
            };

            // Act
            var result = ApiConventionResult.TryGetApiConvention(method, attributes, result: out var conventionResult);

            // Assert
            Assert.True(result);
            Assert.Collection(
                conventionResult.ResponseMetadataProviders.OrderBy(o => o.StatusCode),
                r => Assert.Equal(200, r.StatusCode),
                r => Assert.Equal(202, r.StatusCode),
                r => Assert.Equal(404, r.StatusCode));
        }

        public class GetApiConvention_ReturnsResultFromFirstMatchingConventionController
        {
            public IActionResult Get(int id) => null;
        }

        [Fact]
        public void GetApiConvention_GetAction_MatchesDefaultConvention()
        {
            // Arrange
            var method = typeof(DefaultConventionController)
                .GetMethod(nameof(DefaultConventionController.GetUser));
            var attributes = new[] { new ApiConventionTypeAttribute(typeof(DefaultApiConventions)) };

            // Act
            var result = ApiConventionResult.TryGetApiConvention(method, attributes, out var conventionResult);

            // Assert
            Assert.True(result);
            Assert.Collection(
                conventionResult.ResponseMetadataProviders.OrderBy(o => o.StatusCode),
                r => Assert.Equal(200, r.StatusCode),
                r => Assert.Equal(404, r.StatusCode));
        }

        [Fact]
        public void GetApiConvention_PostAction_MatchesDefaultConvention()
        {
            // Arrange
            var method = typeof(DefaultConventionController)
                .GetMethod(nameof(DefaultConventionController.PostUser));
            var attributes = new[] { new ApiConventionTypeAttribute(typeof(DefaultApiConventions)) };

            // Act
            var result = ApiConventionResult.TryGetApiConvention(method, attributes, out var conventionResult);

            // Assert
            Assert.True(result);
            Assert.Collection(
                conventionResult.ResponseMetadataProviders.OrderBy(o => o.StatusCode),
                r => Assert.Equal(201, r.StatusCode),
                r => Assert.Equal(400, r.StatusCode));
        }

        [Fact]
        public void GetApiConvention_PutAction_MatchesDefaultConvention()
        {
            // Arrange
            var method = typeof(DefaultConventionController)
                .GetMethod(nameof(DefaultConventionController.PutUser));
            var conventions = new[]
            {
                new ApiConventionTypeAttribute(typeof(DefaultApiConventions)),
            };

            // Act
            var result = ApiConventionResult.TryGetApiConvention(method, conventions, out var conventionResult);

            // Assert
            Assert.True(result);
            Assert.Collection(
                conventionResult.ResponseMetadataProviders.OrderBy(o => o.StatusCode),
                r => Assert.Equal(204, r.StatusCode),
                r => Assert.Equal(400, r.StatusCode),
                r => Assert.Equal(404, r.StatusCode));
        }

        [Fact]
        public void GetApiConvention_DeleteAction_MatchesDefaultConvention()
        {
            // Arrange
            var method = typeof(DefaultConventionController)
                .GetMethod(nameof(DefaultConventionController.Delete));
            var conventions = new[]
            {
                new ApiConventionTypeAttribute(typeof(DefaultApiConventions)),
            };

            // Act
            var result = ApiConventionResult.TryGetApiConvention(method, conventions, out var conventionResult);

            // Assert
            Assert.True(result);
            Assert.Collection(
                conventionResult.ResponseMetadataProviders.OrderBy(o => o.StatusCode),
                r => Assert.Equal(200, r.StatusCode),
                r => Assert.Equal(400, r.StatusCode),
                r => Assert.Equal(404, r.StatusCode));
        }

        public class DefaultConventionController
        {
            public IActionResult GetUser(Guid id) => null;

            public IActionResult PostUser(User user) => null;

            public IActionResult PutUser(Guid userId, User user) => null;

            public IActionResult Delete(Guid userId) => null;
        }

        public class User { }

        [Theory]
        [InlineData("Method", "method")]
        [InlineData("Method", "ConventionMethod")]
        [InlineData("p", "model")]
        [InlineData("person", "model")]
        public void IsNameMatch_WithAny_AlwaysReturnsTrue(string name, string conventionName)
        {
            // Act
            var result = ApiConventionResult.IsNameMatch(name, conventionName, ApiConventionNameMatchBehavior.Any);

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
            var result = ApiConventionResult.IsNameMatch(name, conventionName, ApiConventionNameMatchBehavior.Exact);

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
            var result = ApiConventionResult.IsNameMatch(name, conventionName, ApiConventionNameMatchBehavior.Exact);

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
            var result = ApiConventionResult.IsNameMatch(name, conventionName, ApiConventionNameMatchBehavior.Exact);

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
            var result = ApiConventionResult.IsNameMatch(name, conventionName, ApiConventionNameMatchBehavior.Exact);

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
            var result = ApiConventionResult.IsNameMatch(name, conventionName, ApiConventionNameMatchBehavior.Exact);

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
            var result = ApiConventionResult.IsNameMatch(name, conventionName, ApiConventionNameMatchBehavior.Prefix);

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
            var result = ApiConventionResult.IsNameMatch(name, conventionName, ApiConventionNameMatchBehavior.Prefix);

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
            var result = ApiConventionResult.IsNameMatch(name, conventionName, ApiConventionNameMatchBehavior.Prefix);

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
            var result = ApiConventionResult.IsNameMatch(name, conventionName, ApiConventionNameMatchBehavior.Prefix);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsNameMatch_WithPrefix_ReturnsFalse_IfNameIsNotProperPrfix()
        {
            // Arrange
            var name = "Postman";
            var conventionName = "Post";

            // Act
            var result = ApiConventionResult.IsNameMatch(name, conventionName, ApiConventionNameMatchBehavior.Prefix);

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
            var result = ApiConventionResult.IsNameMatch(name, conventionName, ApiConventionNameMatchBehavior.Prefix);

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
            var result = ApiConventionResult.IsNameMatch(name, conventionName, ApiConventionNameMatchBehavior.Suffix);

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
            var result = ApiConventionResult.IsNameMatch(name, conventionName, ApiConventionNameMatchBehavior.Suffix);

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
            var result = ApiConventionResult.IsNameMatch(name, conventionName, ApiConventionNameMatchBehavior.Suffix);

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
            var result = ApiConventionResult.IsNameMatch(name, conventionName, ApiConventionNameMatchBehavior.Suffix);

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
            var result = ApiConventionResult.IsNameMatch(name, conventionName, ApiConventionNameMatchBehavior.Suffix);

            // Assert
            Assert.True(result);
        }

        [Theory]
        [InlineData("candid", "id")]
        [InlineData("canDid", "id")]
        public void IsNameMatch_WithSuffix_ReturnFalse_IfNameIsNotProperSuffix(string name, string conventionName)
        {
            // Act
            var result = ApiConventionResult.IsNameMatch(name, conventionName, ApiConventionNameMatchBehavior.Suffix);

            // Assert
            Assert.False(result);
        }

        [Theory]
        [InlineData(typeof(object), typeof(object))]
        [InlineData(typeof(int), typeof(void))]
        [InlineData(typeof(string), typeof(DateTime))]
        public void IsTypeMatch_WithAny_ReturnsTrue(Type type, Type conventionType)
        {
            // Act
            var result = ApiConventionResult.IsTypeMatch(type, conventionType, ApiConventionTypeMatchBehavior.Any);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsTypeMatch_WithAssinableFrom_ReturnsTrueForExact()
        {
            // Arrange
            var type = typeof(Base);
            var conventionType = typeof(Base);

            // Act
            var result = ApiConventionResult.IsTypeMatch(type, conventionType, ApiConventionTypeMatchBehavior.AssignableFrom);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsTypeMatch_WithAssinableFrom_ReturnsTrueForDerived()
        {
            // Arrange
            var type = typeof(Derived);
            var conventionType = typeof(Base);

            // Act
            var result = ApiConventionResult.IsTypeMatch(type, conventionType, ApiConventionTypeMatchBehavior.AssignableFrom);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsTypeMatch_WithAssinableFrom_ReturnsFalseForBaseTypes()
        {
            // Arrange
            var type = typeof(Base);
            var conventionType = typeof(Derived);

            // Act
            var result = ApiConventionResult.IsTypeMatch(type, conventionType, ApiConventionTypeMatchBehavior.AssignableFrom);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsTypeMatch_WithAssinableFrom_ReturnsFalseForUnrelated()
        {
            // Arrange
            var type = typeof(string);
            var conventionType = typeof(Derived);

            // Act
            var result = ApiConventionResult.IsTypeMatch(type, conventionType, ApiConventionTypeMatchBehavior.AssignableFrom);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsMatch_ReturnsFalse_IfMethodNamesDoNotMatch()
        {
            // Arrange
            var method = typeof(TestController).GetMethod(nameof(TestController.Get));
            var conventionMethod = typeof(TestConvention).GetMethod(nameof(TestConvention.Post));

            // Act
            var result = ApiConventionResult.IsMatch(method, conventionMethod);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsMatch_ReturnsFalse_IMethodHasMoreParametersThanConvention()
        {
            // Arrange
            var method = typeof(TestController).GetMethod(nameof(TestController.Get));
            var conventionMethod = typeof(TestConvention).GetMethod(nameof(TestConvention.GetNoArgs));

            // Act
            var result = ApiConventionResult.IsMatch(method, conventionMethod);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsMatch_ReturnsFalse_IfMethodHasFewerParametersThanConvention()
        {
            // Arrange
            var method = typeof(TestController).GetMethod(nameof(TestController.Get));
            var conventionMethod = typeof(TestConvention).GetMethod(nameof(TestConvention.GetTwoArgs));

            // Act
            var result = ApiConventionResult.IsMatch(method, conventionMethod);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsMatch_ReturnsFalse_IfParametersDoNotMatch()
        {
            // Arrange
            var method = typeof(TestController).GetMethod(nameof(TestController.Get));
            var conventionMethod = typeof(TestConvention).GetMethod(nameof(TestConvention.GetParameterNotMatching));

            // Act
            var result = ApiConventionResult.IsMatch(method, conventionMethod);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsMatch_ReturnsTrue_IfMethodNameAndParametersMatchs()
        {
            // Arrange
            var method = typeof(TestController).GetMethod(nameof(TestController.Get));
            var conventionMethod = typeof(TestConvention).GetMethod(nameof(TestConvention.Get));

            // Act
            var result = ApiConventionResult.IsMatch(method, conventionMethod);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsMatch_ReturnsTrue_IfParamsArrayMatchesRemainingArguments()
        {
            // Arrange
            var method = typeof(TestController).GetMethod(nameof(TestController.Search));
            var conventionMethod = typeof(TestConvention).GetMethod(nameof(TestConvention.Search));

            // Act
            var result = ApiConventionResult.IsMatch(method, conventionMethod);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsMatch_WithEmpty_MatchesMethodWithNoParameters()
        {
            // Arrange
            var method = typeof(TestController).GetMethod(nameof(TestController.SearchEmpty));
            var conventionMethod = typeof(TestConvention).GetMethod(nameof(TestConvention.SearchWithParams));

            // Act
            var result = ApiConventionResult.IsMatch(method, conventionMethod);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void GetNameMatchBehavior_ReturnsExact_WhenNoAttributesArePresent()
        {
            // Arrange
            var expected = ApiConventionNameMatchBehavior.Exact;
            var attributes = new object[0];
            var provider = Mock.Of<ICustomAttributeProvider>(p => p.GetCustomAttributes(false) == attributes);

            // Act
            var result = ApiConventionResult.GetNameMatchBehavior(provider);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void GetNameMatchBehavior_ReturnsExact_WhenNoNameMatchBehaviorAttributeIsSpecified()
        {
            // Arrange
            var expected = ApiConventionNameMatchBehavior.Exact;
            var attributes = new object[] { new CLSCompliantAttribute(false), new ProducesResponseTypeAttribute(200) };
            var provider = Mock.Of<ICustomAttributeProvider>(p => p.GetCustomAttributes(false) == attributes);

            // Act
            var result = ApiConventionResult.GetNameMatchBehavior(provider);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void GetNameMatchBehavior_ReturnsValueFromAttributes()
        {
            // Arrange
            var expected = ApiConventionNameMatchBehavior.Prefix;
            var attributes = new object[]
            {
                new CLSCompliantAttribute(false),
                new ApiConventionNameMatchAttribute(expected),
                new ProducesResponseTypeAttribute(200) }
            ;
            var provider = Mock.Of<ICustomAttributeProvider>(p => p.GetCustomAttributes(false) == attributes);

            // Act
            var result = ApiConventionResult.GetNameMatchBehavior(provider);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void GetTypeMatchBehavior_ReturnsIsAssignableFrom_WhenNoAttributesArePresent()
        {
            // Arrange
            var expected = ApiConventionTypeMatchBehavior.AssignableFrom;
            var attributes = new object[0];
            var provider = Mock.Of<ICustomAttributeProvider>(p => p.GetCustomAttributes(false) == attributes);

            // Act
            var result = ApiConventionResult.GetTypeMatchBehavior(provider);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void GetTypeMatchBehavior_ReturnsIsAssignableFrom_WhenNoMatchingAttributesArePresent()
        {
            // Arrange
            var expected = ApiConventionTypeMatchBehavior.AssignableFrom;
            var attributes = new object[] { new CLSCompliantAttribute(false), new ProducesResponseTypeAttribute(200) };
            var provider = Mock.Of<ICustomAttributeProvider>(p => p.GetCustomAttributes(false) == attributes);

            // Act
            var result = ApiConventionResult.GetTypeMatchBehavior(provider);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void GetTypeMatchBehavior_ReturnsValueFromAttributes()
        {
            // Arrange
            var expected = ApiConventionTypeMatchBehavior.Any;
            var attributes = new object[]
            {
                new CLSCompliantAttribute(false),
                new ApiConventionTypeMatchAttribute(expected),
                new ProducesResponseTypeAttribute(200) }
            ;
            var provider = Mock.Of<ICustomAttributeProvider>(p => p.GetCustomAttributes(false) == attributes);

            // Act
            var result = ApiConventionResult.GetTypeMatchBehavior(provider);

            // Assert
            Assert.Equal(expected, result);
        }

        public class Base { }

        public class Derived : Base { }

        public class TestController
        {
            public IActionResult Get(int id) => null;

            public IActionResult Search(string searchTerm, bool sortDescending, int page) => null;

            public IActionResult SearchEmpty() => null;
        }

        public static class TestConvention
        {
            [ApiConventionNameMatch(ApiConventionNameMatchBehavior.Prefix)]
            public static void Get(int id) { }

            [ApiConventionNameMatch(ApiConventionNameMatchBehavior.Any)]
            public static void GetNoArgs() { }

            [ApiConventionNameMatch(ApiConventionNameMatchBehavior.Any)]
            public static void GetTwoArgs(int id, string name) { }

            [ApiConventionNameMatch(ApiConventionNameMatchBehavior.Prefix)]
            public static void Post(Derived model) { }

            [ApiConventionNameMatch(ApiConventionNameMatchBehavior.Prefix)]
            public static void GetParameterNotMatching([ApiConventionTypeMatch(ApiConventionTypeMatchBehavior.AssignableFrom)] Derived model) { }

            [ApiConventionNameMatch(ApiConventionNameMatchBehavior.Any)]
            public static void Search(
                [ApiConventionNameMatch(ApiConventionNameMatchBehavior.Exact)]
                string searchTerm,
                params object[] others)
            { }

            [ApiConventionNameMatch(ApiConventionNameMatchBehavior.Any)]
            public static void SearchWithParams(params object[] others) { }
        }
    }
}
