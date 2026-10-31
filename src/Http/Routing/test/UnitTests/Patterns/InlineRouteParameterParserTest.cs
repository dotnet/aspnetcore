// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Routing.Patterns;

public class InlineRouteParameterParserTest
{
    [Theory]
    [InlineData("=")]
    [InlineData(":")]
    public void ParseRouteParameter_WithoutADefaultValue(string parameterName)
    {
        // Arrange & Act
        var templatePart = ParseParameter(parameterName);

        // Assert
        Assert.Equal(parameterName, templatePart.Name);
        Assert.Null(templatePart.Default);
        Assert.Empty(templatePart.ParameterPolicies);
    }

    [Fact]
    public void ParseRouteParameter_WithEmptyDefaultValue()
    {
        // Arrange & Act
        var templatePart = ParseParameter("param=");

        // Assert
        Assert.Equal("param", templatePart.Name);
        Assert.Equal("", templatePart.Default);
        Assert.Empty(templatePart.ParameterPolicies);
    }

    [Fact]
    public void ParseRouteParameter_WithoutAConstraintName()
    {
        // Arrange & Act
        var templatePart = ParseParameter("param:");

        // Assert
        Assert.Equal("param", templatePart.Name);
        Assert.Null(templatePart.Default);
        Assert.Empty(templatePart.ParameterPolicies);
    }

    [Fact]
    public void ParseRouteParameter_WithoutAConstraintNameOrParameterName()
    {
        // Arrange & Act
        var templatePart = ParseParameter("param:=");

        // Assert
        Assert.Equal("param", templatePart.Name);
        Assert.Equal("", templatePart.Default);
        Assert.Empty(templatePart.ParameterPolicies);
    }

    [Fact]
    public void ParseRouteParameter_WithADefaultValueContainingConstraintSeparator()
    {
        // Arrange & Act
        var templatePart = ParseParameter("param=:");

        // Assert
        Assert.Equal("param", templatePart.Name);
        Assert.Equal(":", templatePart.Default);
        Assert.Empty(templatePart.ParameterPolicies);
    }

    [Fact]
    public void ParseRouteParameter_ConstraintAndDefault_ParsedCorrectly()
    {
        // Arrange & Act
        var templatePart = ParseParameter("param:int=111111");

        // Assert
        Assert.Equal("param", templatePart.Name);
        Assert.Equal("111111", templatePart.Default);

        var constraint = Assert.Single(templatePart.ParameterPolicies);
        Assert.Equal("int", constraint.Content);
    }

    [Fact]
    public void ParseRouteParameter_ConstraintWithArgumentsAndDefault_ParsedCorrectly()
    {
        // Arrange & Act
        var templatePart = ParseParameter(@"param:test(\d+)=111111");

        // Assert
        Assert.Equal("param", templatePart.Name);
        Assert.Equal("111111", templatePart.Default);

        var constraint = Assert.Single(templatePart.ParameterPolicies);
        Assert.Equal(@"test(\d+)", constraint.Content);
    }

    [Fact]
    public void ParseRouteParameter_ConstraintAndOptional_ParsedCorrectly()
    {
        // Arrange & Act
        var templatePart = ParseParameter(@"param:int?");

        // Assert
        Assert.Equal("param", templatePart.Name);
        Assert.True(templatePart.IsOptional);

        var constraint = Assert.Single(templatePart.ParameterPolicies);
        Assert.Equal("int", constraint.Content);
    }

    [Fact]
    public void ParseRouteParameter_ConstraintAndOptional_WithDefaultValue_ParsedCorrectly()
    {
        // Arrange & Act
        var templatePart = ParseParameter(@"param:int=12?");

        // Assert
        Assert.Equal("param", templatePart.Name);
        Assert.Equal("12", templatePart.Default);
        Assert.True(templatePart.IsOptional);

        var constraint = Assert.Single(templatePart.ParameterPolicies);
        Assert.Equal("int", constraint.Content);
    }

    [Fact]
    public void ParseRouteParameter_ConstraintAndOptional_WithDefaultValueWithQuestionMark_ParsedCorrectly()
    {
        // Arrange & Act
        var templatePart = ParseParameter(@"param:int=12??");

        // Assert
        Assert.Equal("param", templatePart.Name);
        Assert.Equal("12?", templatePart.Default);
        Assert.True(templatePart.IsOptional);

        var constraint = Assert.Single(templatePart.ParameterPolicies);
        Assert.Equal("int", constraint.Content);
    }

    [Fact]
    public void ParseRouteParameter_ConstraintWithArgumentsAndOptional_ParsedCorrectly()
    {
        // Arrange & Act
        var templatePart = ParseParameter(@"param:test(\d+)?");

        // Assert
        Assert.Equal("param", templatePart.Name);
        Assert.True(templatePart.IsOptional);

        var constraint = Assert.Single(templatePart.ParameterPolicies);
        Assert.Equal(@"test(\d+)", constraint.Content);
    }

    [Fact]
    public void ParseRouteParameter_ConstraintWithArgumentsAndOptional_WithDefaultValue_ParsedCorrectly()
    {
        // Arrange & Act
        var templatePart = ParseParameter(@"param:test(\d+)=abc?");

        // Assert
        Assert.Equal("param", templatePart.Name);
        Assert.True(templatePart.IsOptional);

        Assert.Equal("abc", templatePart.Default);

        var constraint = Assert.Single(templatePart.ParameterPolicies);
        Assert.Equal(@"test(\d+)", constraint.Content);
    }

    [Fact]
    public void ParseRouteParameter_ChainedConstraints_ParsedCorrectly()
    {
        // Arrange & Act
        var templatePart = ParseParameter(@"param:test(d+):test(w+)");

        // Assert
        Assert.Equal("param", templatePart.Name);

        Assert.Collection(templatePart.ParameterPolicies,
            constraint => Assert.Equal(@"test(d+)", constraint.Content),
            constraint => Assert.Equal(@"test(w+)", constraint.Content));
    }

    [Fact]
    public void ParseRouteParameter_ChainedConstraints_DoubleDelimiters_ParsedCorrectly()
    {
        // Arrange & Act
        var templatePart = ParseParameter(@"param::test(d+)::test(w+)");

        // Assert
        Assert.Equal("param", templatePart.Name);

        Assert.Collection(
            templatePart.ParameterPolicies,
            constraint => Assert.Equal(@"test(d+)", constraint.Content),
            constraint => Assert.Equal(@"test(w+)", constraint.Content));
    }

    [Fact]
    public void ParseRouteParameter_ChainedConstraints_ColonInPattern_ParsedCorrectly()
    {
        // Arrange & Act
        var templatePart = ParseParameter(@"param:test(\d+):test(\w:+)");

        // Assert
        Assert.Equal("param", templatePart.Name);

        Assert.Collection(templatePart.ParameterPolicies,
            constraint => Assert.Equal(@"test(\d+)", constraint.Content),
            constraint => Assert.Equal(@"test(\w:+)", constraint.Content));
    }

    [Fact]
    public void ParseRouteParameter_ChainedConstraints_WithDefaultValue_ParsedCorrectly()
    {
        // Arrange & Act
        var templatePart = ParseParameter(@"param:test(\d+):test(\w+)=qwer");

        // Assert
        Assert.Equal("param", templatePart.Name);

        Assert.Equal("qwer", templatePart.Default);

        Assert.Collection(templatePart.ParameterPolicies,
            constraint => Assert.Equal(@"test(\d+)", constraint.Content),
            constraint => Assert.Equal(@"test(\w+)", constraint.Content));
    }

    [Fact]
    public void ParseRouteParameter_ChainedConstraints_WithDefaultValue_DoubleDelimiters_ParsedCorrectly()
    {
        // Arrange & Act
        var templatePart = ParseParameter(@"param:test(\d+)::test(\w+)==qwer");

        // Assert
        Assert.Equal("param", templatePart.Name);

        Assert.Equal("=qwer", templatePart.Default);

        Assert.Collection(
            templatePart.ParameterPolicies,
            constraint => Assert.Equal(@"test(\d+)", constraint.Content),
            constraint => Assert.Equal(@"test(\w+)", constraint.Content));
    }

    [Theory]
    [InlineData("=")]
    [InlineData("+=")]
    [InlineData(">= || <= || ==")]
    public void ParseRouteParameter_WithDefaultValue_ContainingDelimiter(string defaultValue)
    {
        // Arrange & Act
        var templatePart = ParseParameter($"comparison-operator:length(6)={defaultValue}");

        // Assert
        Assert.Equal("comparison-operator", templatePart.Name);
        Assert.Equal(defaultValue, templatePart.Default);

        var constraint = Assert.Single(templatePart.ParameterPolicies);
        Assert.Equal("length(6)", constraint.Content);
    }

    [Fact]
    public void ParseRouteTemplate_ConstraintsDefaultsAndOptionalsInMultipleSections_ParsedCorrectly()
    {
        // Arrange & Act
        var routePattern = RoutePatternFactory.Parse(@"some/url-{p1:int:test(3)=hello}/{p2=abc}/{p3?}");

        // Assert
        var parameters = routePattern.Parameters.ToArray();

        var param1 = parameters[0];
        Assert.Equal("p1", param1.Name);
        Assert.Equal("hello", param1.Default);
        Assert.False(param1.IsOptional);

        Assert.Collection(param1.ParameterPolicies,
            constraint => Assert.Equal("int", constraint.Content),
            constraint => Assert.Equal("test(3)", constraint.Content)
        );

        var param2 = parameters[1];
        Assert.Equal("p2", param2.Name);
        Assert.Equal("abc", param2.Default);
        Assert.False(param2.IsOptional);

        var param3 = parameters[2];
        Assert.Equal("p3", param3.Name);
        Assert.True(param3.IsOptional);
    }

    [Fact]
    public void ParseRouteParameter_NoTokens_ParsedCorrectly()
    {
        // Arrange & Act
        var templatePart = ParseParameter("world");

        // Assert
        Assert.Equal("world", templatePart.Name);
    }

    [Fact]
    public void ParseRouteParameter_ParamDefault_ParsedCorrectly()
    {
        // Arrange & Act
        var templatePart = ParseParameter("param=world");

        // Assert
        Assert.Equal("param", templatePart.Name);
        Assert.Equal("world", templatePart.Default);
    }

    [Fact]
    public void ParseRouteParameter_ConstraintWithClosingBraceInPattern_ClosingBraceIsParsedCorrectly()
    {
        // Arrange & Act
        var templatePart = ParseParameter(@"param:test(\})");

        // Assert
        Assert.Equal("param", templatePart.Name);

        var constraint = Assert.Single(templatePart.ParameterPolicies);
        Assert.Equal(@"test(\})", constraint.Content);
    }

    [Fact]
    public void ParseRouteParameter_ConstraintWithClosingBraceInPattern_WithDefaultValue_ParsedCorrectly()
    {
        // Arrange & Act
        var templatePart = ParseParameter(@"param:test(\})=wer");

        // Assert
        Assert.Equal("param", templatePart.Name);

        Assert.Equal("wer", templatePart.Default);

        var constraint = Assert.Single(templatePart.ParameterPolicies);
        Assert.Equal(@"test(\})", constraint.Content);
    }

    [Fact]
    public void ParseRouteParameter_ConstraintWithClosingParenInPattern_ClosingParenIsParsedCorrectly()
    {
        // Arrange & Act
        var templatePart = ParseParameter(@"param:test(\))");

        // Assert
        Assert.Equal("param", templatePart.Name);

        var constraint = Assert.Single(templatePart.ParameterPolicies);
        Assert.Equal(@"test(\))", constraint.Content);
    }

    [Fact]
    public void ParseRouteParameter_ConstraintWithClosingParenInPattern_WithDefaultValue_ParsedCorrectly()
    {
        // Arrange & Act
        var templatePart = ParseParameter(@"param:test(\))=fsd");

        // Assert
        Assert.Equal("param", templatePart.Name);

        Assert.Equal("fsd", templatePart.Default);

        var constraint = Assert.Single(templatePart.ParameterPolicies);
        Assert.Equal(@"test(\))", constraint.Content);
    }

    [Fact]
    public void ParseRouteParameter_ConstraintWithColonInPattern_ColonIsParsedCorrectly()
    {
        // Arrange & Act
        var templatePart = ParseParameter(@"param:test(:)");

        // Assert
        Assert.Equal("param", templatePart.Name);

        var constraint = Assert.Single(templatePart.ParameterPolicies);
        Assert.Equal(@"test(:)", constraint.Content);
    }

    [Fact]
    public void ParseRouteParameter_ConstraintWithColonInPattern_WithDefaultValue_ParsedCorrectly()
    {
        // Arrange & Act
        var templatePart = ParseParameter(@"param:test(:)=mnf");

        // Assert
        Assert.Equal("param", templatePart.Name);

        Assert.Equal("mnf", templatePart.Default);

        var constraint = Assert.Single(templatePart.ParameterPolicies);
        Assert.Equal(@"test(:)", constraint.Content);
    }

    [Fact]
    public void ParseRouteParameter_ConstraintWithColonsInPattern_ParsedCorrectly()
    {
        // Arrange & Act
        var templatePart = ParseParameter(@"param:test(a:b:c)");

        // Assert
        Assert.Equal("param", templatePart.Name);

        var constraint = Assert.Single(templatePart.ParameterPolicies);
        Assert.Equal(@"test(a:b:c)", constraint.Content);
    }

    [Fact]
    public void ParseRouteParameter_ConstraintWithColonInParamName_ParsedCorrectly()
    {
        // Arrange & Act
        var templatePart = ParseParameter(@":param:test=12");

        // Assert
        Assert.Equal(":param", templatePart.Name);

        Assert.Equal("12", templatePart.Default);

        var constraint = Assert.Single(templatePart.ParameterPolicies);
        Assert.Equal("test", constraint.Content);
    }

    [Fact]
    public void ParseRouteParameter_ConstraintWithTwoColonInParamName_ParsedCorrectly()
    {
        // Arrange & Act
        var templatePart = ParseParameter(@":param::test=12");

        // Assert
        Assert.Equal(":param", templatePart.Name);

        Assert.Equal("12", templatePart.Default);

        Assert.Collection(
            templatePart.ParameterPolicies,
            constraint => Assert.Equal("test", constraint.Content));
    }

    [Fact]
    public void ParseRouteParameter_EmptyConstraint_ParsedCorrectly()
    {
        // Arrange & Act
        var templatePart = ParseParameter(@":param:test:");

        // Assert
        Assert.Equal(":param", templatePart.Name);

        Assert.Collection(
            templatePart.ParameterPolicies,
            constraint => Assert.Equal("test", constraint.Content));
    }

    [Fact]
    public void ParseRouteParameter_ConstraintWithCommaInPattern_PatternIsParsedCorrectly()
    {
        // Arrange & Act
        var templatePart = ParseParameter(@"param:test(\w,\w)");

        // Assert
        Assert.Equal("param", templatePart.Name);

        var constraint = Assert.Single(templatePart.ParameterPolicies);
        Assert.Equal(@"test(\w,\w)", constraint.Content);
    }

    [Fact]
    public void ParseRouteParameter_ConstraintWithCommaInName_ParsedCorrectly()
    {
        // Arrange & Act
        var templatePart = ParseParameter(@"par,am:test(\w)");

        // Assert
        Assert.Equal("par,am", templatePart.Name);

        var constraint = Assert.Single(templatePart.ParameterPolicies);
        Assert.Equal(@"test(\w)", constraint.Content);
    }

    [Fact]
    public void ParseRouteParameter_ConstraintWithCommaInPattern_WithDefaultValue_ParsedCorrectly()
    {
        // Arrange & Act
        var templatePart = ParseParameter(@"param:test(\w,\w)=jsd");

        // Assert
        Assert.Equal("param", templatePart.Name);

        Assert.Equal("jsd", templatePart.Default);

        var constraint = Assert.Single(templatePart.ParameterPolicies);
        Assert.Equal(@"test(\w,\w)", constraint.Content);
    }

    [Fact]
    public void ParseRouteParameter_ConstraintWithEqualsFollowedByQuestionMark_PatternIsParsedCorrectly()
    {
        // Arrange & Act
        var templatePart = ParseParameter(@"param:int=?");

        // Assert
        Assert.Equal("param", templatePart.Name);
        Assert.Equal("", templatePart.Default);

        Assert.True(templatePart.IsOptional);

        var constraint = Assert.Single(templatePart.ParameterPolicies);
        Assert.Equal("int", constraint.Content);
    }

    [Fact]
    public void ParseRouteParameter_ConstraintWithEqualsSignInPattern_PatternIsParsedCorrectly()
    {
        // Arrange & Act
        var templatePart = ParseParameter(@"param:test(=)");

        // Assert
        Assert.Equal("param", templatePart.Name);
        Assert.Null(templatePart.Default);

        var constraint = Assert.Single(templatePart.ParameterPolicies);
        Assert.Equal("test(=)", constraint.Content);
    }

    [Fact]
    public void ParseRouteParameter_EqualsSignInDefaultValue_ParsedCorrectly()
    {
        // Arrange & Act
        var templatePart = ParseParameter(@"param=test=bar");

        // Assert
        Assert.Equal("param", templatePart.Name);
        Assert.Equal("test=bar", templatePart.Default);
    }

    [Fact]
    public void ParseRouteParameter_ConstraintWithEqualEqualSignInPattern_ParsedCorrectly()
    {
        // Arrange & Act
        var templatePart = ParseParameter(@"param:test(a==b)");

        // Assert
        Assert.Equal("param", templatePart.Name);
        Assert.Null(templatePart.Default);

        var constraint = Assert.Single(templatePart.ParameterPolicies);
        Assert.Equal("test(a==b)", constraint.Content);
    }

    [Fact]
    public void ParseRouteParameter_ConstraintWithEqualEqualSignInPattern_WithDefaultValue_ParsedCorrectly()
    {
        // Arrange & Act
        var templatePart = ParseParameter(@"param:test(a==b)=dvds");

        // Assert
        Assert.Equal("param", templatePart.Name);
        Assert.Equal("dvds", templatePart.Default);

        var constraint = Assert.Single(templatePart.ParameterPolicies);
        Assert.Equal("test(a==b)", constraint.Content);
    }

    [Fact]
    public void ParseRouteParameter_EqualEqualSignInName_WithDefaultValue_ParsedCorrectly()
    {
        // Arrange & Act
        var templatePart = ParseParameter(@"par==am:test=dvds");

        // Assert
        Assert.Equal("par", templatePart.Name);
        Assert.Equal("=am:test=dvds", templatePart.Default);
    }

    [Fact]
    public void ParseRouteParameter_EqualEqualSignInDefaultValue_ParsedCorrectly()
    {
        // Arrange & Act
        var templatePart = ParseParameter(@"param:test==dvds");

        // Assert
        Assert.Equal("param", templatePart.Name);
        Assert.Equal("=dvds", templatePart.Default);
    }

    [Fact]
    public void ParseRouteParameter_DefaultValueWithColonAndParens_ParsedCorrectly()
    {
        // Arrange & Act
        var templatePart = ParseParameter(@"par=am:test(asd)");

        // Assert
        Assert.Equal("par", templatePart.Name);
        Assert.Equal("am:test(asd)", templatePart.Default);
    }

    [Fact]
    public void ParseRouteParameter_DefaultValueWithEqualsSignIn_ParsedCorrectly()
    {
        // Arrange & Act
        var templatePart = ParseParameter(@"par=test(am):est=asd");

        // Assert
        Assert.Equal("par", templatePart.Name);
        Assert.Equal("test(am):est=asd", templatePart.Default);
    }

    [Fact]
    public void ParseRouteParameter_ConstraintWithEqualsSignInPattern_WithDefaultValue_ParsedCorrectly()
    {
        // Arrange & Act
        var templatePart = ParseParameter(@"param:test(=)=sds");

        // Assert
        Assert.Equal("param", templatePart.Name);
        Assert.Equal("sds", templatePart.Default);

        var constraint = Assert.Single(templatePart.ParameterPolicies);
        Assert.Equal("test(=)", constraint.Content);
    }

    [Fact]
    public void ParseRouteParameter_ConstraintWithOpenBraceInPattern_PatternIsParsedCorrectly()
    {
        // Arrange & Act
        var templatePart = ParseParameter(@"param:test(\{)");

        // Assert
        Assert.Equal("param", templatePart.Name);

        var constraint = Assert.Single(templatePart.ParameterPolicies);
        Assert.Equal(@"test(\{)", constraint.Content);
    }

    [Fact]
    public void ParseRouteParameter_ConstraintWithOpenBraceInName_ParsedCorrectly()
    {
        // Arrange & Act
        var templatePart = ParseParameter(@"par{am:test(\sd)");

        // Assert
        Assert.Equal("par{am", templatePart.Name);

        var constraint = Assert.Single(templatePart.ParameterPolicies);
        Assert.Equal(@"test(\sd)", constraint.Content);
    }

    [Fact]
    public void ParseRouteParameter_ConstraintWithOpenBraceInPattern_WithDefaultValue_ParsedCorrectly()
    {
        // Arrange & Act
        var templatePart = ParseParameter(@"param:test(\{)=xvc");

        // Assert
        Assert.Equal("param", templatePart.Name);

        Assert.Equal("xvc", templatePart.Default);

        var constraint = Assert.Single(templatePart.ParameterPolicies);
        Assert.Equal(@"test(\{)", constraint.Content);
    }

    [Fact]
    public void ParseRouteParameter_ConstraintWithOpenParenInName_ParsedCorrectly()
    {
        // Arrange & Act
        var templatePart = ParseParameter(@"par(am:test(\()");

        // Assert
        Assert.Equal("par(am", templatePart.Name);

        var constraint = Assert.Single(templatePart.ParameterPolicies);
        Assert.Equal(@"test(\()", constraint.Content);
    }

    [Fact]
    public void ParseRouteParameter_ConstraintWithOpenParenInPattern_ParsedCorrectly()
    {
        // Arrange & Act
        var templatePart = ParseParameter(@"param:test(\()");

        // Assert
        Assert.Equal("param", templatePart.Name);

        var constraint = Assert.Single(templatePart.ParameterPolicies);
        Assert.Equal(@"test(\()", constraint.Content);
    }

    [Fact]
    public void ParseRouteParameter_ConstraintWithOpenParenNoCloseParen_ParsedCorrectly()
    {
        // Arrange & Act
        var templatePart = ParseParameter(@"param:test(#$%");

        // Assert
        Assert.Equal("param", templatePart.Name);

        var constraint = Assert.Single(templatePart.ParameterPolicies);
        Assert.Equal("test(#$%", constraint.Content);
    }

    [Fact]
    public void ParseRouteParameter_ConstraintWithOpenParenAndColon_ParsedCorrectly()
    {
        // Arrange & Act
        var templatePart = ParseParameter(@"param:test(#:test1");

        // Assert
        Assert.Equal("param", templatePart.Name);

        Assert.Collection(templatePart.ParameterPolicies,
            constraint => Assert.Equal(@"test(#", constraint.Content),
            constraint => Assert.Equal(@"test1", constraint.Content));
    }

    [Fact]
    public void ParseRouteParameter_ConstraintWithOpenParenAndColonWithDefaultValue_ParsedCorrectly()
    {
        // Arrange & Act
        var templatePart = ParseParameter(@"param:test(abc:somevalue):name(test1:differentname=default-value");

        // Assert
        Assert.Equal("param", templatePart.Name);
        Assert.Equal("default-value", templatePart.Default);

        Assert.Collection(templatePart.ParameterPolicies,
            constraint => Assert.Equal(@"test(abc:somevalue)", constraint.Content),
            constraint => Assert.Equal(@"name(test1", constraint.Content),
            constraint => Assert.Equal(@"differentname", constraint.Content));
    }

    [Fact]
    public void ParseRouteParameter_ConstraintWithOpenParenAndDefaultValue_ParsedCorrectly()
    {
        // Arrange & Act
        var templatePart = ParseParameter(@"param:test(constraintvalue=test1");

        // Assert
        Assert.Equal("param", templatePart.Name);
        Assert.Equal("test1", templatePart.Default);

        var constraint = Assert.Single(templatePart.ParameterPolicies);
        Assert.Equal(@"test(constraintvalue", constraint.Content);
    }

    [Fact]
    public void ParseRouteParameter_ConstraintWithOpenParenInPattern_WithDefaultValue_ParsedCorrectly()
    {
        // Arrange & Act
        var templatePart = ParseParameter(@"param:test(\()=djk");

        // Assert
        Assert.Equal("param", templatePart.Name);

        Assert.Equal("djk", templatePart.Default);

        var constraint = Assert.Single(templatePart.ParameterPolicies);
        Assert.Equal(@"test(\()", constraint.Content);
    }

    [Fact]
    public void ParseRouteParameter_ConstraintWithQuestionMarkInPattern_PatternIsParsedCorrectly()
    {
        // Arrange & Act
        var templatePart = ParseParameter(@"param:test(\?)");

        // Assert
        Assert.Equal("param", templatePart.Name);
        Assert.Null(templatePart.Default);
        Assert.False(templatePart.IsOptional);

        var constraint = Assert.Single(templatePart.ParameterPolicies);
        Assert.Equal(@"test(\?)", constraint.Content);
    }

    [Fact]
    public void ParseRouteParameter_ConstraintWithQuestionMarkInPattern_Optional_PatternIsParsedCorrectly()
    {
        // Arrange & Act
        var templatePart = ParseParameter(@"param:test(\?)?");

        // Assert
        Assert.Equal("param", templatePart.Name);
        Assert.Null(templatePart.Default);
        Assert.True(templatePart.IsOptional);

        var constraint = Assert.Single(templatePart.ParameterPolicies);
        Assert.Equal(@"test(\?)", constraint.Content);
    }

    [Fact]
    public void ParseRouteParameter_ConstraintWithQuestionMarkInPattern_WithDefaultValue_ParsedCorrectly()
    {
        // Arrange & Act
        var templatePart = ParseParameter(@"param:test(\?)=sdf");

        // Assert
        Assert.Equal("param", templatePart.Name);
        Assert.Equal("sdf", templatePart.Default);
        Assert.False(templatePart.IsOptional);

        var constraint = Assert.Single(templatePart.ParameterPolicies);
        Assert.Equal(@"test(\?)", constraint.Content);
    }

    [Fact]
    public void ParseRouteParameter_ConstraintWithQuestionMarkInPattern_Optional_WithDefaultValue_ParsedCorrectly()
    {
        // Arrange & Act
        var templatePart = ParseParameter(@"param:test(\?)=sdf?");

        // Assert
        Assert.Equal("param", templatePart.Name);
        Assert.Equal("sdf", templatePart.Default);
        Assert.True(templatePart.IsOptional);

        var constraint = Assert.Single(templatePart.ParameterPolicies);
        Assert.Equal(@"test(\?)", constraint.Content);
    }

    [Fact]
    public void ParseRouteParameter_ConstraintWithQuestionMarkInName_ParsedCorrectly()
    {
        // Arrange & Act
        var templatePart = ParseParameter(@"par?am:test(\?)");

        // Assert
        Assert.Equal("par?am", templatePart.Name);
        Assert.Null(templatePart.Default);
        Assert.False(templatePart.IsOptional);

        var constraint = Assert.Single(templatePart.ParameterPolicies);
        Assert.Equal(@"test(\?)", constraint.Content);
    }

    [Fact]
    public void ParseRouteParameter_ConstraintWithClosedParenAndColonInPattern_ParsedCorrectly()
    {
        // Arrange & Act
        var templatePart = ParseParameter(@"param:test(#):$)");

        // Assert
        Assert.Equal("param", templatePart.Name);
        Assert.Null(templatePart.Default);
        Assert.False(templatePart.IsOptional);

        Assert.Collection(templatePart.ParameterPolicies,
            constraint => Assert.Equal(@"test(#)", constraint.Content),
            constraint => Assert.Equal(@"$)", constraint.Content));
    }

    [Fact]
    public void ParseRouteParameter_ConstraintWithColonAndClosedParenInPattern_PatternIsParsedCorrectly()
    {
        // Arrange & Act
        var templatePart = ParseParameter(@"param:test(#:)$)");

        // Assert
        Assert.Equal("param", templatePart.Name);
        Assert.Null(templatePart.Default);
        Assert.False(templatePart.IsOptional);

        var constraint = Assert.Single(templatePart.ParameterPolicies);
        Assert.Equal(@"test(#:)$)", constraint.Content);
    }

    [Fact]
    public void ParseRouteParameter_ContainingMultipleUnclosedParenthesisInConstraint()
    {
        // Arrange & Act
        var templatePart = ParseParameter(@"foo:regex(\\(\\(\\(\\()");

        // Assert
        Assert.Equal("foo", templatePart.Name);
        Assert.Null(templatePart.Default);
        Assert.False(templatePart.IsOptional);

        var constraint = Assert.Single(templatePart.ParameterPolicies);
        Assert.Equal(@"regex(\\(\\(\\(\\()", constraint.Content);
    }

    [Fact]
    public void ParseRouteParameter_ConstraintWithBraces_PatternIsParsedCorrectly()
    {
        // Arrange & Act
        var templatePart = ParseParameter(@"p1:regex(^\d{{3}}-\d{{3}}-\d{{4}}$)"); // ssn

        // Assert
        Assert.Equal("p1", templatePart.Name);
        Assert.Null(templatePart.Default);
        Assert.False(templatePart.IsOptional);

        var constraint = Assert.Single(templatePart.ParameterPolicies);
        Assert.Equal(@"regex(^\d{{3}}-\d{{3}}-\d{{4}}$)", constraint.Content);
    }

    [Fact]
    public void ParseRouteParameter_ConstraintWithBraces_WithDefaultValue()
    {
        // Arrange & Act
        var templatePart = ParseParameter(@"p1:regex(^\d{{3}}-\d{{3}}-\d{{4}}$)=123-456-7890"); // ssn

        // Assert
        Assert.Equal("p1", templatePart.Name);
        Assert.Equal("123-456-7890", templatePart.Default);
        Assert.False(templatePart.IsOptional);

        var constraint = Assert.Single(templatePart.ParameterPolicies);
        Assert.Equal(@"regex(^\d{{3}}-\d{{3}}-\d{{4}}$)", constraint.Content);
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("?", "")]
    [InlineData("*", "")]
    [InlineData("**", "")]
    [InlineData(" ", " ")]
    [InlineData("\t", "\t")]
    [InlineData("#!@#$%Q@#@%", "#!@#$%Q@#@%")]
    [InlineData(",,,", ",,,")]
    public void ParseRouteParameter_ParameterWithoutInlineConstraint_ReturnsTemplatePartWithEmptyInlineValues(
                                                                                    string parameter,
                                                                                    string expectedParameterName)
    {
        // Arrange & Act
        var templatePart = ParseParameter(parameter);

        // Assert
        Assert.Equal(expectedParameterName, templatePart.Name);
        Assert.Empty(templatePart.ParameterPolicies);
        Assert.Null(templatePart.Default);
    }

    [Fact]
    public void ParseRouteParameter_WithSingleAsteriskCatchAll_IsParsedCorrectly()
    {
        // Arrange & Act
        var parameterPart = ParseParameter("*path");

        // Assert
        Assert.Equal("path", parameterPart.Name);
        Assert.True(parameterPart.IsCatchAll);
        Assert.Equal(RoutePatternParameterKind.CatchAll, parameterPart.ParameterKind);
        Assert.True(parameterPart.EncodeSlashes);
    }

    [Fact]
    public void ParseRouteParameter_WithSingleAsteriskCatchAll_AndDefaultValue_IsParsedCorrectly()
    {
        // Arrange & Act
        var parameterPart = ParseParameter("*path=a/b/c");

        // Assert
        Assert.Equal("path", parameterPart.Name);
        Assert.True(parameterPart.IsCatchAll);
        Assert.NotNull(parameterPart.Default);
        Assert.Equal("a/b/c", parameterPart.Default.ToString());
        Assert.Equal(RoutePatternParameterKind.CatchAll, parameterPart.ParameterKind);
        Assert.True(parameterPart.EncodeSlashes);
    }

    [Fact]
    public void ParseRouteParameter_WithSingleAsteriskCatchAll_AndConstraints_IsParsedCorrectly()
    {
        // Arrange
        var constraintContent = "regex(^(/[^/ ]*)+/?$)";

        // Act
        var parameterPart = ParseParameter($"*path:{constraintContent}");

        // Assert
        Assert.Equal("path", parameterPart.Name);
        Assert.True(parameterPart.IsCatchAll);
        Assert.Equal(RoutePatternParameterKind.CatchAll, parameterPart.ParameterKind);
        var constraintReference = Assert.Single(parameterPart.ParameterPolicies);
        Assert.Equal(constraintContent, constraintReference.Content);
        Assert.True(parameterPart.EncodeSlashes);
    }

    [Fact]
    public void ParseRouteParameter_WithSingleAsteriskCatchAll_AndConstraints_AndDefaultValue_IsParsedCorrectly()
    {
        // Arrange
        var constraintContent = "regex(^(/[^/ ]*)+/?$)";

        // Act
        var parameterPart = ParseParameter($"*path:{constraintContent}=a/b/c");

        // Assert
        Assert.Equal("path", parameterPart.Name);
        Assert.True(parameterPart.IsCatchAll);
        Assert.Equal(RoutePatternParameterKind.CatchAll, parameterPart.ParameterKind);
        var constraintReference = Assert.Single(parameterPart.ParameterPolicies);
        Assert.Equal(constraintContent, constraintReference.Content);
        Assert.NotNull(parameterPart.Default);
        Assert.Equal("a/b/c", parameterPart.Default.ToString());
        Assert.True(parameterPart.EncodeSlashes);
    }

    [Fact]
    public void ParseRouteParameter_WithDoubleAsteriskCatchAll_IsParsedCorrectly()
    {
        // Arrange & Act
        var parameterPart = ParseParameter("**path");

        // Assert
        Assert.Equal("path", parameterPart.Name);
        Assert.True(parameterPart.IsCatchAll);
        Assert.False(parameterPart.EncodeSlashes);
    }

    [Fact]
    public void ParseRouteParameter_WithDoubleAsteriskCatchAll_AndDefaultValue_IsParsedCorrectly()
    {
        // Arrange & Act
        var parameterPart = ParseParameter("**path=a/b/c");

        // Assert
        Assert.Equal("path", parameterPart.Name);
        Assert.True(parameterPart.IsCatchAll);
        Assert.NotNull(parameterPart.Default);
        Assert.Equal("a/b/c", parameterPart.Default.ToString());
        Assert.False(parameterPart.EncodeSlashes);
    }

    [Fact]
    public void ParseRouteParameter_WithDoubleAsteriskCatchAll_AndConstraints_IsParsedCorrectly()
    {
        // Arrange
        var constraintContent = "regex(^(/[^/ ]*)+/?$)";

        // Act
        var parameterPart = ParseParameter($"**path:{constraintContent}");

        // Assert
        Assert.Equal("path", parameterPart.Name);
        Assert.True(parameterPart.IsCatchAll);
        Assert.False(parameterPart.EncodeSlashes);
        var constraintReference = Assert.Single(parameterPart.ParameterPolicies);
        Assert.Equal(constraintContent, constraintReference.Content);
    }

    [Fact]
    public void ParseRouteParameter_WithDoubleAsteriskCatchAll_AndConstraints_AndDefaultValue_IsParsedCorrectly()
    {
        // Arrange
        var constraintContent = "regex(^(/[^/ ]*)+/?$)";

        // Act
        var parameterPart = ParseParameter($"**path:{constraintContent}=a/b/c");

        // Assert
        Assert.Equal("path", parameterPart.Name);
        Assert.True(parameterPart.IsCatchAll);
        Assert.False(parameterPart.EncodeSlashes);
        var constraintReference = Assert.Single(parameterPart.ParameterPolicies);
        Assert.Equal(constraintContent, constraintReference.Content);
        Assert.NotNull(parameterPart.Default);
        Assert.Equal("a/b/c", parameterPart.Default.ToString());
    }

    private RoutePatternParameterPart ParseParameter(string routeParameter)
    {
        // See: #475 - these tests don't pass the 'whole' text.
        var templatePart = RouteParameterParser.ParseRouteParameter(routeParameter);
        return templatePart;
    }
}
