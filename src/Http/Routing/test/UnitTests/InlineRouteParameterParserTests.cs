// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Template;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Routing.Tests;

public class InlineRouteParameterParserTests
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
        Assert.Null(templatePart.DefaultValue);
        Assert.Empty(templatePart.InlineConstraints);
    }

    [Fact]
    public void ParseRouteParameter_WithEmptyDefaultValue()
    {
        // Arrange & Act
        var templatePart = ParseParameter("param=");

        // Assert
        Assert.Equal("param", templatePart.Name);
        Assert.Equal("", templatePart.DefaultValue);
        Assert.Empty(templatePart.InlineConstraints);
    }

    [Fact]
    public void ParseRouteParameter_WithoutAConstraintName()
    {
        // Arrange & Act
        var templatePart = ParseParameter("param:");

        // Assert
        Assert.Equal("param", templatePart.Name);
        Assert.Null(templatePart.DefaultValue);
        var constraint = Assert.Single(templatePart.InlineConstraints);
        Assert.Empty(constraint.Constraint);
    }

    [Fact]
    public void ParseRouteParameter_WithoutAConstraintNameOrParameterName()
    {
        // Arrange & Act
        var templatePart = ParseParameter("param:=");

        // Assert
        Assert.Equal("param", templatePart.Name);
        Assert.Equal("", templatePart.DefaultValue);
        var constraint = Assert.Single(templatePart.InlineConstraints);
        Assert.Empty(constraint.Constraint);
    }

    [Fact]
    public void ParseRouteParameter_WithADefaultValueContainingConstraintSeparator()
    {
        // Arrange & Act
        var templatePart = ParseParameter("param=:");

        // Assert
        Assert.Equal("param", templatePart.Name);
        Assert.Equal(":", templatePart.DefaultValue);
        Assert.Empty(templatePart.InlineConstraints);
    }

    [Fact]
    public void ParseRouteParameter_ConstraintAndDefault_ParsedCorrectly()
    {
        // Arrange & Act
        var templatePart = ParseParameter("param:int=111111");

        // Assert
        Assert.Equal("param", templatePart.Name);
        Assert.Equal("111111", templatePart.DefaultValue);

        var constraint = Assert.Single(templatePart.InlineConstraints);
        Assert.Equal("int", constraint.Constraint);
    }

    [Fact]
    public void ParseRouteParameter_ConstraintWithArgumentsAndDefault_ParsedCorrectly()
    {
        // Arrange & Act
        var templatePart = ParseParameter(@"param:test(\d+)=111111");

        // Assert
        Assert.Equal("param", templatePart.Name);
        Assert.Equal("111111", templatePart.DefaultValue);

        var constraint = Assert.Single(templatePart.InlineConstraints);
        Assert.Equal(@"test(\d+)", constraint.Constraint);
    }

    [Fact]
    public void ParseRouteParameter_ConstraintAndOptional_ParsedCorrectly()
    {
        // Arrange & Act
        var templatePart = ParseParameter(@"param:int?");

        // Assert
        Assert.Equal("param", templatePart.Name);
        Assert.True(templatePart.IsOptional);

        var constraint = Assert.Single(templatePart.InlineConstraints);
        Assert.Equal("int", constraint.Constraint);
    }

    [Fact]
    public void ParseRouteParameter_ConstraintAndOptional_WithDefaultValue_ParsedCorrectly()
    {
        // Arrange & Act
        var templatePart = ParseParameter(@"param:int=12?");

        // Assert
        Assert.Equal("param", templatePart.Name);
        Assert.Equal("12", templatePart.DefaultValue);
        Assert.True(templatePart.IsOptional);

        var constraint = Assert.Single(templatePart.InlineConstraints);
        Assert.Equal("int", constraint.Constraint);
    }

    [Fact]
    public void ParseRouteParameter_ConstraintAndOptional_WithDefaultValueWithQuestionMark_ParsedCorrectly()
    {
        // Arrange & Act
        var templatePart = ParseParameter(@"param:int=12??");

        // Assert
        Assert.Equal("param", templatePart.Name);
        Assert.Equal("12?", templatePart.DefaultValue);
        Assert.True(templatePart.IsOptional);

        var constraint = Assert.Single(templatePart.InlineConstraints);
        Assert.Equal("int", constraint.Constraint);
    }

    [Fact]
    public void ParseRouteParameter_ConstraintWithArgumentsAndOptional_ParsedCorrectly()
    {
        // Arrange & Act
        var templatePart = ParseParameter(@"param:test(\d+)?");

        // Assert
        Assert.Equal("param", templatePart.Name);
        Assert.True(templatePart.IsOptional);

        var constraint = Assert.Single(templatePart.InlineConstraints);
        Assert.Equal(@"test(\d+)", constraint.Constraint);
    }

    [Fact]
    public void ParseRouteParameter_ConstraintWithArgumentsAndOptional_WithDefaultValue_ParsedCorrectly()
    {
        // Arrange & Act
        var templatePart = ParseParameter(@"param:test(\d+)=abc?");

        // Assert
        Assert.Equal("param", templatePart.Name);
        Assert.True(templatePart.IsOptional);

        Assert.Equal("abc", templatePart.DefaultValue);

        var constraint = Assert.Single(templatePart.InlineConstraints);
        Assert.Equal(@"test(\d+)", constraint.Constraint);
    }

    [Fact]
    public void ParseRouteParameter_ChainedConstraints_ParsedCorrectly()
    {
        // Arrange & Act
        var templatePart = ParseParameter(@"param:test(d+):test(w+)");

        // Assert
        Assert.Equal("param", templatePart.Name);

        Assert.Collection(templatePart.InlineConstraints,
            constraint => Assert.Equal(@"test(d+)", constraint.Constraint),
            constraint => Assert.Equal(@"test(w+)", constraint.Constraint));
    }

    [Fact]
    public void ParseRouteParameter_ChainedConstraints_DoubleDelimiters_ParsedCorrectly()
    {
        // Arrange & Act
        var templatePart = ParseParameter(@"param::test(d+)::test(w+)");

        // Assert
        Assert.Equal("param", templatePart.Name);

        Assert.Collection(templatePart.InlineConstraints,
            constraint => Assert.Empty(constraint.Constraint),
            constraint => Assert.Equal(@"test(d+)", constraint.Constraint),
            constraint => Assert.Empty(constraint.Constraint),
            constraint => Assert.Equal(@"test(w+)", constraint.Constraint));
    }

    [Fact]
    public void ParseRouteParameter_ChainedConstraints_ColonInPattern_ParsedCorrectly()
    {
        // Arrange & Act
        var templatePart = ParseParameter(@"param:test(\d+):test(\w:+)");

        // Assert
        Assert.Equal("param", templatePart.Name);

        Assert.Collection(templatePart.InlineConstraints,
            constraint => Assert.Equal(@"test(\d+)", constraint.Constraint),
            constraint => Assert.Equal(@"test(\w:+)", constraint.Constraint));
    }

    [Fact]
    public void ParseRouteParameter_ChainedConstraints_WithDefaultValue_ParsedCorrectly()
    {
        // Arrange & Act
        var templatePart = ParseParameter(@"param:test(\d+):test(\w+)=qwer");

        // Assert
        Assert.Equal("param", templatePart.Name);

        Assert.Equal("qwer", templatePart.DefaultValue);

        Assert.Collection(templatePart.InlineConstraints,
            constraint => Assert.Equal(@"test(\d+)", constraint.Constraint),
            constraint => Assert.Equal(@"test(\w+)", constraint.Constraint));
    }

    [Fact]
    public void ParseRouteParameter_ChainedConstraints_WithDefaultValue_DoubleDelimiters_ParsedCorrectly()
    {
        // Arrange & Act
        var templatePart = ParseParameter(@"param:test(\d+)::test(\w+)==qwer");

        // Assert
        Assert.Equal("param", templatePart.Name);

        Assert.Equal("=qwer", templatePart.DefaultValue);

        Assert.Collection(templatePart.InlineConstraints,
            constraint => Assert.Equal(@"test(\d+)", constraint.Constraint),
            constraint => Assert.Empty(constraint.Constraint),
            constraint => Assert.Equal(@"test(\w+)", constraint.Constraint));
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
        Assert.Equal(defaultValue, templatePart.DefaultValue);

        var constraint = Assert.Single(templatePart.InlineConstraints);
        Assert.Equal("length(6)", constraint.Constraint);
    }

    [Fact]
    public void ParseRouteTemplate_ConstraintsDefaultsAndOptionalsInMultipleSections_ParsedCorrectly()
    {
        // Arrange & Act
        var template = ParseRouteTemplate(@"some/url-{p1:int:test(3)=hello}/{p2=abc}/{p3?}");

        // Assert
        var parameters = template.Parameters.ToArray();

        var param1 = parameters[0];
        Assert.Equal("p1", param1.Name);
        Assert.Equal("hello", param1.DefaultValue);
        Assert.False(param1.IsOptional);

        Assert.Collection(param1.InlineConstraints,
            constraint => Assert.Equal("int", constraint.Constraint),
            constraint => Assert.Equal("test(3)", constraint.Constraint)
        );

        var param2 = parameters[1];
        Assert.Equal("p2", param2.Name);
        Assert.Equal("abc", param2.DefaultValue);
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
        Assert.Equal("world", templatePart.DefaultValue);
    }

    [Fact]
    public void ParseRouteParameter_ConstraintWithClosingBraceInPattern_ClosingBraceIsParsedCorrectly()
    {
        // Arrange & Act
        var templatePart = ParseParameter(@"param:test(\})");

        // Assert
        Assert.Equal("param", templatePart.Name);

        var constraint = Assert.Single(templatePart.InlineConstraints);
        Assert.Equal(@"test(\})", constraint.Constraint);
    }

    [Fact]
    public void ParseRouteParameter_ConstraintWithClosingBraceInPattern_WithDefaultValue_ParsedCorrectly()
    {
        // Arrange & Act
        var templatePart = ParseParameter(@"param:test(\})=wer");

        // Assert
        Assert.Equal("param", templatePart.Name);

        Assert.Equal("wer", templatePart.DefaultValue);

        var constraint = Assert.Single(templatePart.InlineConstraints);
        Assert.Equal(@"test(\})", constraint.Constraint);
    }

    [Fact]
    public void ParseRouteParameter_ConstraintWithClosingParenInPattern_ClosingParenIsParsedCorrectly()
    {
        // Arrange & Act
        var templatePart = ParseParameter(@"param:test(\))");

        // Assert
        Assert.Equal("param", templatePart.Name);

        var constraint = Assert.Single(templatePart.InlineConstraints);
        Assert.Equal(@"test(\))", constraint.Constraint);
    }

    [Fact]
    public void ParseRouteParameter_ConstraintWithClosingParenInPattern_WithDefaultValue_ParsedCorrectly()
    {
        // Arrange & Act
        var templatePart = ParseParameter(@"param:test(\))=fsd");

        // Assert
        Assert.Equal("param", templatePart.Name);

        Assert.Equal("fsd", templatePart.DefaultValue);

        var constraint = Assert.Single(templatePart.InlineConstraints);
        Assert.Equal(@"test(\))", constraint.Constraint);
    }

    [Fact]
    public void ParseRouteParameter_ConstraintWithColonInPattern_ColonIsParsedCorrectly()
    {
        // Arrange & Act
        var templatePart = ParseParameter(@"param:test(:)");

        // Assert
        Assert.Equal("param", templatePart.Name);

        var constraint = Assert.Single(templatePart.InlineConstraints);
        Assert.Equal(@"test(:)", constraint.Constraint);
    }

    [Fact]
    public void ParseRouteParameter_ConstraintWithColonInPattern_WithDefaultValue_ParsedCorrectly()
    {
        // Arrange & Act
        var templatePart = ParseParameter(@"param:test(:)=mnf");

        // Assert
        Assert.Equal("param", templatePart.Name);

        Assert.Equal("mnf", templatePart.DefaultValue);

        var constraint = Assert.Single(templatePart.InlineConstraints);
        Assert.Equal(@"test(:)", constraint.Constraint);
    }

    [Fact]
    public void ParseRouteParameter_ConstraintWithColonsInPattern_ParsedCorrectly()
    {
        // Arrange & Act
        var templatePart = ParseParameter(@"param:test(a:b:c)");

        // Assert
        Assert.Equal("param", templatePart.Name);

        var constraint = Assert.Single(templatePart.InlineConstraints);
        Assert.Equal(@"test(a:b:c)", constraint.Constraint);
    }

    [Fact]
    public void ParseRouteParameter_ConstraintWithColonInParamName_ParsedCorrectly()
    {
        // Arrange & Act
        var templatePart = ParseParameter(@":param:test=12");

        // Assert
        Assert.Equal(":param", templatePart.Name);

        Assert.Equal("12", templatePart.DefaultValue);

        var constraint = Assert.Single(templatePart.InlineConstraints);
        Assert.Equal("test", constraint.Constraint);
    }

    [Fact]
    public void ParseRouteParameter_ConstraintWithTwoColonInParamName_ParsedCorrectly()
    {
        // Arrange & Act
        var templatePart = ParseParameter(@":param::test=12");

        // Assert
        Assert.Equal(":param", templatePart.Name);

        Assert.Equal("12", templatePart.DefaultValue);

        Assert.Collection(templatePart.InlineConstraints,
            constraint => Assert.Empty(constraint.Constraint),
            constraint => Assert.Equal("test", constraint.Constraint));
    }

    [Fact]
    public void ParseRouteParameter_EmptyConstraint_ParsedCorrectly()
    {
        // Arrange & Act
        var templatePart = ParseParameter(@":param:test:");

        // Assert
        Assert.Equal(":param", templatePart.Name);

        Assert.Collection(templatePart.InlineConstraints,
            constraint => Assert.Equal("test", constraint.Constraint),
            constraint => Assert.Empty(constraint.Constraint));
    }

    [Fact]
    public void ParseRouteParameter_ConstraintWithCommaInPattern_PatternIsParsedCorrectly()
    {
        // Arrange & Act
        var templatePart = ParseParameter(@"param:test(\w,\w)");

        // Assert
        Assert.Equal("param", templatePart.Name);

        var constraint = Assert.Single(templatePart.InlineConstraints);
        Assert.Equal(@"test(\w,\w)", constraint.Constraint);
    }

    [Fact]
    public void ParseRouteParameter_ConstraintWithCommaInName_ParsedCorrectly()
    {
        // Arrange & Act
        var templatePart = ParseParameter(@"par,am:test(\w)");

        // Assert
        Assert.Equal("par,am", templatePart.Name);

        var constraint = Assert.Single(templatePart.InlineConstraints);
        Assert.Equal(@"test(\w)", constraint.Constraint);
    }

    [Fact]
    public void ParseRouteParameter_ConstraintWithCommaInPattern_WithDefaultValue_ParsedCorrectly()
    {
        // Arrange & Act
        var templatePart = ParseParameter(@"param:test(\w,\w)=jsd");

        // Assert
        Assert.Equal("param", templatePart.Name);

        Assert.Equal("jsd", templatePart.DefaultValue);

        var constraint = Assert.Single(templatePart.InlineConstraints);
        Assert.Equal(@"test(\w,\w)", constraint.Constraint);
    }

    [Fact]
    public void ParseRouteParameter_ConstraintWithEqualsFollowedByQuestionMark_PatternIsParsedCorrectly()
    {
        // Arrange & Act
        var templatePart = ParseParameter(@"param:int=?");

        // Assert
        Assert.Equal("param", templatePart.Name);
        Assert.Equal("", templatePart.DefaultValue);

        Assert.True(templatePart.IsOptional);

        var constraint = Assert.Single(templatePart.InlineConstraints);
        Assert.Equal("int", constraint.Constraint);
    }

    [Fact]
    public void ParseRouteParameter_ConstraintWithEqualsSignInPattern_PatternIsParsedCorrectly()
    {
        // Arrange & Act
        var templatePart = ParseParameter(@"param:test(=)");

        // Assert
        Assert.Equal("param", templatePart.Name);
        Assert.Null(templatePart.DefaultValue);

        var constraint = Assert.Single(templatePart.InlineConstraints);
        Assert.Equal("test(=)", constraint.Constraint);
    }

    [Fact]
    public void ParseRouteParameter_EqualsSignInDefaultValue_ParsedCorrectly()
    {
        // Arrange & Act
        var templatePart = ParseParameter(@"param=test=bar");

        // Assert
        Assert.Equal("param", templatePart.Name);
        Assert.Equal("test=bar", templatePart.DefaultValue);
    }

    [Fact]
    public void ParseRouteParameter_ConstraintWithEqualEqualSignInPattern_ParsedCorrectly()
    {
        // Arrange & Act
        var templatePart = ParseParameter(@"param:test(a==b)");

        // Assert
        Assert.Equal("param", templatePart.Name);
        Assert.Null(templatePart.DefaultValue);

        var constraint = Assert.Single(templatePart.InlineConstraints);
        Assert.Equal("test(a==b)", constraint.Constraint);
    }

    [Fact]
    public void ParseRouteParameter_ConstraintWithEqualEqualSignInPattern_WithDefaultValue_ParsedCorrectly()
    {
        // Arrange & Act
        var templatePart = ParseParameter(@"param:test(a==b)=dvds");

        // Assert
        Assert.Equal("param", templatePart.Name);
        Assert.Equal("dvds", templatePart.DefaultValue);

        var constraint = Assert.Single(templatePart.InlineConstraints);
        Assert.Equal("test(a==b)", constraint.Constraint);
    }

    [Fact]
    public void ParseRouteParameter_EqualEqualSignInName_WithDefaultValue_ParsedCorrectly()
    {
        // Arrange & Act
        var templatePart = ParseParameter(@"par==am:test=dvds");

        // Assert
        Assert.Equal("par", templatePart.Name);
        Assert.Equal("=am:test=dvds", templatePart.DefaultValue);
    }

    [Fact]
    public void ParseRouteParameter_EqualEqualSignInDefaultValue_ParsedCorrectly()
    {
        // Arrange & Act
        var templatePart = ParseParameter(@"param:test==dvds");

        // Assert
        Assert.Equal("param", templatePart.Name);
        Assert.Equal("=dvds", templatePart.DefaultValue);
    }

    [Fact]
    public void ParseRouteParameter_DefaultValueWithColonAndParens_ParsedCorrectly()
    {
        // Arrange & Act
        var templatePart = ParseParameter(@"par=am:test(asd)");

        // Assert
        Assert.Equal("par", templatePart.Name);
        Assert.Equal("am:test(asd)", templatePart.DefaultValue);
    }

    [Fact]
    public void ParseRouteParameter_DefaultValueWithEqualsSignIn_ParsedCorrectly()
    {
        // Arrange & Act
        var templatePart = ParseParameter(@"par=test(am):est=asd");

        // Assert
        Assert.Equal("par", templatePart.Name);
        Assert.Equal("test(am):est=asd", templatePart.DefaultValue);
    }

    [Fact]
    public void ParseRouteParameter_ConstraintWithEqualsSignInPattern_WithDefaultValue_ParsedCorrectly()
    {
        // Arrange & Act
        var templatePart = ParseParameter(@"param:test(=)=sds");

        // Assert
        Assert.Equal("param", templatePart.Name);
        Assert.Equal("sds", templatePart.DefaultValue);

        var constraint = Assert.Single(templatePart.InlineConstraints);
        Assert.Equal("test(=)", constraint.Constraint);
    }

    [Fact]
    public void ParseRouteParameter_ConstraintWithOpenBraceInPattern_PatternIsParsedCorrectly()
    {
        // Arrange & Act
        var templatePart = ParseParameter(@"param:test(\{)");

        // Assert
        Assert.Equal("param", templatePart.Name);

        var constraint = Assert.Single(templatePart.InlineConstraints);
        Assert.Equal(@"test(\{)", constraint.Constraint);
    }

    [Fact]
    public void ParseRouteParameter_ConstraintWithOpenBraceInName_ParsedCorrectly()
    {
        // Arrange & Act
        var templatePart = ParseParameter(@"par{am:test(\sd)");

        // Assert
        Assert.Equal("par{am", templatePart.Name);

        var constraint = Assert.Single(templatePart.InlineConstraints);
        Assert.Equal(@"test(\sd)", constraint.Constraint);
    }

    [Fact]
    public void ParseRouteParameter_ConstraintWithOpenBraceInPattern_WithDefaultValue_ParsedCorrectly()
    {
        // Arrange & Act
        var templatePart = ParseParameter(@"param:test(\{)=xvc");

        // Assert
        Assert.Equal("param", templatePart.Name);

        Assert.Equal("xvc", templatePart.DefaultValue);

        var constraint = Assert.Single(templatePart.InlineConstraints);
        Assert.Equal(@"test(\{)", constraint.Constraint);
    }

    [Fact]
    public void ParseRouteParameter_ConstraintWithOpenParenInName_ParsedCorrectly()
    {
        // Arrange & Act
        var templatePart = ParseParameter(@"par(am:test(\()");

        // Assert
        Assert.Equal("par(am", templatePart.Name);

        var constraint = Assert.Single(templatePart.InlineConstraints);
        Assert.Equal(@"test(\()", constraint.Constraint);
    }

    [Fact]
    public void ParseRouteParameter_ConstraintWithOpenParenInPattern_ParsedCorrectly()
    {
        // Arrange & Act
        var templatePart = ParseParameter(@"param:test(\()");

        // Assert
        Assert.Equal("param", templatePart.Name);

        var constraint = Assert.Single(templatePart.InlineConstraints);
        Assert.Equal(@"test(\()", constraint.Constraint);
    }

    [Fact]
    public void ParseRouteParameter_ConstraintWithOpenParenNoCloseParen_ParsedCorrectly()
    {
        // Arrange & Act
        var templatePart = ParseParameter(@"param:test(#$%");

        // Assert
        Assert.Equal("param", templatePart.Name);

        var constraint = Assert.Single(templatePart.InlineConstraints);
        Assert.Equal("test(#$%", constraint.Constraint);
    }

    [Fact]
    public void ParseRouteParameter_ConstraintWithOpenParenAndColon_ParsedCorrectly()
    {
        // Arrange & Act
        var templatePart = ParseParameter(@"param:test(#:test1");

        // Assert
        Assert.Equal("param", templatePart.Name);

        Assert.Collection(templatePart.InlineConstraints,
            constraint => Assert.Equal(@"test(#", constraint.Constraint),
            constraint => Assert.Equal(@"test1", constraint.Constraint));
    }

    [Fact]
    public void ParseRouteParameter_ConstraintWithOpenParenAndColonWithDefaultValue_ParsedCorrectly()
    {
        // Arrange & Act
        var templatePart = ParseParameter(@"param:test(abc:somevalue):name(test1:differentname=default-value");

        // Assert
        Assert.Equal("param", templatePart.Name);
        Assert.Equal("default-value", templatePart.DefaultValue);

        Assert.Collection(templatePart.InlineConstraints,
            constraint => Assert.Equal(@"test(abc:somevalue)", constraint.Constraint),
            constraint => Assert.Equal(@"name(test1", constraint.Constraint),
            constraint => Assert.Equal(@"differentname", constraint.Constraint));
    }

    [Fact]
    public void ParseRouteParameter_ConstraintWithOpenParenAndDefaultValue_ParsedCorrectly()
    {
        // Arrange & Act
        var templatePart = ParseParameter(@"param:test(constraintvalue=test1");

        // Assert
        Assert.Equal("param", templatePart.Name);
        Assert.Equal("test1", templatePart.DefaultValue);

        var constraint = Assert.Single(templatePart.InlineConstraints);
        Assert.Equal(@"test(constraintvalue", constraint.Constraint);
    }

    [Fact]
    public void ParseRouteParameter_ConstraintWithOpenParenInPattern_WithDefaultValue_ParsedCorrectly()
    {
        // Arrange & Act
        var templatePart = ParseParameter(@"param:test(\()=djk");

        // Assert
        Assert.Equal("param", templatePart.Name);

        Assert.Equal("djk", templatePart.DefaultValue);

        var constraint = Assert.Single(templatePart.InlineConstraints);
        Assert.Equal(@"test(\()", constraint.Constraint);
    }

    [Fact]
    public void ParseRouteParameter_ConstraintWithQuestionMarkInPattern_PatternIsParsedCorrectly()
    {
        // Arrange & Act
        var templatePart = ParseParameter(@"param:test(\?)");

        // Assert
        Assert.Equal("param", templatePart.Name);
        Assert.Null(templatePart.DefaultValue);
        Assert.False(templatePart.IsOptional);

        var constraint = Assert.Single(templatePart.InlineConstraints);
        Assert.Equal(@"test(\?)", constraint.Constraint);
    }

    [Fact]
    public void ParseRouteParameter_ConstraintWithQuestionMarkInPattern_Optional_PatternIsParsedCorrectly()
    {
        // Arrange & Act
        var templatePart = ParseParameter(@"param:test(\?)?");

        // Assert
        Assert.Equal("param", templatePart.Name);
        Assert.Null(templatePart.DefaultValue);
        Assert.True(templatePart.IsOptional);

        var constraint = Assert.Single(templatePart.InlineConstraints);
        Assert.Equal(@"test(\?)", constraint.Constraint);
    }

    [Fact]
    public void ParseRouteParameter_ConstraintWithQuestionMarkInPattern_WithDefaultValue_ParsedCorrectly()
    {
        // Arrange & Act
        var templatePart = ParseParameter(@"param:test(\?)=sdf");

        // Assert
        Assert.Equal("param", templatePart.Name);
        Assert.Equal("sdf", templatePart.DefaultValue);
        Assert.False(templatePart.IsOptional);

        var constraint = Assert.Single(templatePart.InlineConstraints);
        Assert.Equal(@"test(\?)", constraint.Constraint);
    }

    [Fact]
    public void ParseRouteParameter_ConstraintWithQuestionMarkInPattern_Optional_WithDefaultValue_ParsedCorrectly()
    {
        // Arrange & Act
        var templatePart = ParseParameter(@"param:test(\?)=sdf?");

        // Assert
        Assert.Equal("param", templatePart.Name);
        Assert.Equal("sdf", templatePart.DefaultValue);
        Assert.True(templatePart.IsOptional);

        var constraint = Assert.Single(templatePart.InlineConstraints);
        Assert.Equal(@"test(\?)", constraint.Constraint);
    }

    [Fact]
    public void ParseRouteParameter_ConstraintWithQuestionMarkInName_ParsedCorrectly()
    {
        // Arrange & Act
        var templatePart = ParseParameter(@"par?am:test(\?)");

        // Assert
        Assert.Equal("par?am", templatePart.Name);
        Assert.Null(templatePart.DefaultValue);
        Assert.False(templatePart.IsOptional);

        var constraint = Assert.Single(templatePart.InlineConstraints);
        Assert.Equal(@"test(\?)", constraint.Constraint);
    }

    [Fact]
    public void ParseRouteParameter_ConstraintWithClosedParenAndColonInPattern_ParsedCorrectly()
    {
        // Arrange & Act
        var templatePart = ParseParameter(@"param:test(#):$)");

        // Assert
        Assert.Equal("param", templatePart.Name);
        Assert.Null(templatePart.DefaultValue);
        Assert.False(templatePart.IsOptional);

        Assert.Collection(templatePart.InlineConstraints,
            constraint => Assert.Equal(@"test(#)", constraint.Constraint),
            constraint => Assert.Equal(@"$)", constraint.Constraint));
    }

    [Fact]
    public void ParseRouteParameter_ConstraintWithColonAndClosedParenInPattern_PatternIsParsedCorrectly()
    {
        // Arrange & Act
        var templatePart = ParseParameter(@"param:test(#:)$)");

        // Assert
        Assert.Equal("param", templatePart.Name);
        Assert.Null(templatePart.DefaultValue);
        Assert.False(templatePart.IsOptional);

        var constraint = Assert.Single(templatePart.InlineConstraints);
        Assert.Equal(@"test(#:)$)", constraint.Constraint);
    }

    [Fact]
    public void ParseRouteParameter_ContainingMultipleUnclosedParenthesisInConstraint()
    {
        // Arrange & Act
        var templatePart = ParseParameter(@"foo:regex(\\(\\(\\(\\()");

        // Assert
        Assert.Equal("foo", templatePart.Name);
        Assert.Null(templatePart.DefaultValue);
        Assert.False(templatePart.IsOptional);

        var constraint = Assert.Single(templatePart.InlineConstraints);
        Assert.Equal(@"regex(\\(\\(\\(\\()", constraint.Constraint);
    }

    [Fact]
    public void ParseRouteParameter_ConstraintWithBraces_PatternIsParsedCorrectly()
    {
        // Arrange & Act
        var templatePart = ParseParameter(@"p1:regex(^\d{{3}}-\d{{3}}-\d{{4}}$)"); // ssn

        // Assert
        Assert.Equal("p1", templatePart.Name);
        Assert.Null(templatePart.DefaultValue);
        Assert.False(templatePart.IsOptional);

        var constraint = Assert.Single(templatePart.InlineConstraints);
        Assert.Equal(@"regex(^\d{{3}}-\d{{3}}-\d{{4}}$)", constraint.Constraint);
    }

    [Fact]
    public void ParseRouteParameter_ConstraintWithBraces_WithDefaultValue()
    {
        // Arrange & Act
        var templatePart = ParseParameter(@"p1:regex(^\d{{3}}-\d{{3}}-\d{{4}}$)=123-456-7890"); // ssn

        // Assert
        Assert.Equal("p1", templatePart.Name);
        Assert.Equal("123-456-7890", templatePart.DefaultValue);
        Assert.False(templatePart.IsOptional);

        var constraint = Assert.Single(templatePart.InlineConstraints);
        Assert.Equal(@"regex(^\d{{3}}-\d{{3}}-\d{{4}}$)", constraint.Constraint);
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("?", "")]
    [InlineData("*", "")]
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
        Assert.Empty(templatePart.InlineConstraints);
        Assert.Null(templatePart.DefaultValue);
    }

    private TemplatePart ParseParameter(string routeParameter)
    {
        var _constraintResolver = GetConstraintResolver();
        var templatePart = InlineRouteParameterParser.ParseRouteParameter(routeParameter);
        return templatePart;
    }

    private static RouteTemplate ParseRouteTemplate(string template)
    {
        var _constraintResolver = GetConstraintResolver();
        return TemplateParser.Parse(template);
    }

    private static IInlineConstraintResolver GetConstraintResolver()
    {
        var services = new ServiceCollection().AddOptions();
        services.Configure<RouteOptions>(options =>
                            options
                            .ConstraintMap
                            .Add("test", typeof(TestRouteConstraint)));
        var serviceProvider = services.BuildServiceProvider();
        var accessor = serviceProvider.GetRequiredService<IOptions<RouteOptions>>();
        return new DefaultInlineConstraintResolver(accessor, serviceProvider);
    }

    private class TestRouteConstraint : IRouteConstraint
    {
        public TestRouteConstraint(string pattern)
        {
            Pattern = pattern;
        }

        public string Pattern { get; private set; }
        public bool Match(HttpContext httpContext,
                          IRouter route,
                          string routeKey,
                          RouteValueDictionary values,
                          RouteDirection routeDirection)
        {
            throw new NotImplementedException();
        }
    }
}
