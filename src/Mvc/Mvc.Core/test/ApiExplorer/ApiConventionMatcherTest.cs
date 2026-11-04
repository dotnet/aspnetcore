// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Moq;

namespace Microsoft.AspNetCore.Mvc.ApiExplorer;

public class ApiConventionMatcherTest
{
    [Theory]
    [InlineData("Method", "method")]
    [InlineData("Method", "ConventionMethod")]
    [InlineData("p", "model")]
    [InlineData("person", "model")]
    public void IsNameMatch_WithAny_AlwaysReturnsTrue(string name, string conventionName)
    {
        // Act
        var result = ApiConventionMatcher.IsNameMatch(name, conventionName, ApiConventionNameMatchBehavior.Any);

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
        var result = ApiConventionMatcher.IsNameMatch(name, conventionName, ApiConventionNameMatchBehavior.Exact);

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
        var result = ApiConventionMatcher.IsNameMatch(name, conventionName, ApiConventionNameMatchBehavior.Exact);

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
        var result = ApiConventionMatcher.IsNameMatch(name, conventionName, ApiConventionNameMatchBehavior.Exact);

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
        var result = ApiConventionMatcher.IsNameMatch(name, conventionName, ApiConventionNameMatchBehavior.Exact);

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
        var result = ApiConventionMatcher.IsNameMatch(name, conventionName, ApiConventionNameMatchBehavior.Exact);

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
        var result = ApiConventionMatcher.IsNameMatch(name, conventionName, ApiConventionNameMatchBehavior.Prefix);

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
        var result = ApiConventionMatcher.IsNameMatch(name, conventionName, ApiConventionNameMatchBehavior.Prefix);

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
        var result = ApiConventionMatcher.IsNameMatch(name, conventionName, ApiConventionNameMatchBehavior.Prefix);

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
        var result = ApiConventionMatcher.IsNameMatch(name, conventionName, ApiConventionNameMatchBehavior.Prefix);

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
        var result = ApiConventionMatcher.IsNameMatch(name, conventionName, ApiConventionNameMatchBehavior.Prefix);

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
        var result = ApiConventionMatcher.IsNameMatch(name, conventionName, ApiConventionNameMatchBehavior.Prefix);

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
        var result = ApiConventionMatcher.IsNameMatch(name, conventionName, ApiConventionNameMatchBehavior.Suffix);

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
        var result = ApiConventionMatcher.IsNameMatch(name, conventionName, ApiConventionNameMatchBehavior.Suffix);

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
        var result = ApiConventionMatcher.IsNameMatch(name, conventionName, ApiConventionNameMatchBehavior.Suffix);

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
        var result = ApiConventionMatcher.IsNameMatch(name, conventionName, ApiConventionNameMatchBehavior.Suffix);

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
        var result = ApiConventionMatcher.IsNameMatch(name, conventionName, ApiConventionNameMatchBehavior.Suffix);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData("candid", "id")]
    [InlineData("canDid", "id")]
    public void IsNameMatch_WithSuffix_ReturnFalse_IfNameIsNotProperSuffix(string name, string conventionName)
    {
        // Act
        var result = ApiConventionMatcher.IsNameMatch(name, conventionName, ApiConventionNameMatchBehavior.Suffix);

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
        var result = ApiConventionMatcher.IsTypeMatch(type, conventionType, ApiConventionTypeMatchBehavior.Any);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsTypeMatch_WithAssignableFrom_ReturnsTrueForExact()
    {
        // Arrange
        var type = typeof(Base);
        var conventionType = typeof(Base);

        // Act
        var result = ApiConventionMatcher.IsTypeMatch(type, conventionType, ApiConventionTypeMatchBehavior.AssignableFrom);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsTypeMatch_WithAssignableFrom_ReturnsTrueForDerived()
    {
        // Arrange
        var type = typeof(Derived);
        var conventionType = typeof(Base);

        // Act
        var result = ApiConventionMatcher.IsTypeMatch(type, conventionType, ApiConventionTypeMatchBehavior.AssignableFrom);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsTypeMatch_WithAssignableFrom_ReturnsFalseForBaseTypes()
    {
        // Arrange
        var type = typeof(Base);
        var conventionType = typeof(Derived);

        // Act
        var result = ApiConventionMatcher.IsTypeMatch(type, conventionType, ApiConventionTypeMatchBehavior.AssignableFrom);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsTypeMatch_WithAssignableFrom_ReturnsFalseForUnrelated()
    {
        // Arrange
        var type = typeof(string);
        var conventionType = typeof(Derived);

        // Act
        var result = ApiConventionMatcher.IsTypeMatch(type, conventionType, ApiConventionTypeMatchBehavior.AssignableFrom);

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
        var result = ApiConventionMatcher.IsMatch(method, conventionMethod);

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
        var result = ApiConventionMatcher.IsMatch(method, conventionMethod);

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
        var result = ApiConventionMatcher.IsMatch(method, conventionMethod);

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
        var result = ApiConventionMatcher.IsMatch(method, conventionMethod);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsMatch_ReturnsTrue_IfMethodNameAndParametersMatches()
    {
        // Arrange
        var method = typeof(TestController).GetMethod(nameof(TestController.Get));
        var conventionMethod = typeof(TestConvention).GetMethod(nameof(TestConvention.Get));

        // Act
        var result = ApiConventionMatcher.IsMatch(method, conventionMethod);

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
        var result = ApiConventionMatcher.IsMatch(method, conventionMethod);

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
        var result = ApiConventionMatcher.IsMatch(method, conventionMethod);

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
        var result = ApiConventionMatcher.GetNameMatchBehavior(provider);

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
        var result = ApiConventionMatcher.GetNameMatchBehavior(provider);

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
        var result = ApiConventionMatcher.GetNameMatchBehavior(provider);

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
        var result = ApiConventionMatcher.GetTypeMatchBehavior(provider);

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
        var result = ApiConventionMatcher.GetTypeMatchBehavior(provider);

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
        var result = ApiConventionMatcher.GetTypeMatchBehavior(provider);

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
