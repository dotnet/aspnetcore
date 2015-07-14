// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Routing.Template;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.OptionsModel;
using Xunit;

namespace Microsoft.AspNet.Routing.Tests
{
    public class InlineRouteParameterParserTests
    {
        [Fact]
        public void ParseRouteParameter_ConstraintAndDefault_ParsedCorrectly()
        {
            // Arrange & Act
            var templatePart = ParseParameter("param:int=111111");

            // Assert
            Assert.Equal("param", templatePart.Name);
            Assert.Equal("111111", templatePart.DefaultValue);

            Assert.Single(templatePart.InlineConstraints);
            Assert.Single(templatePart.InlineConstraints, c => c.Constraint == "int");
        }

        [Fact]
        public void ParseRouteParameter_ConstraintWithArgumentsAndDefault_ParsedCorrectly()
        {
            // Arrange & Act
            var templatePart = ParseParameter(@"param:test(\d+)=111111");

            // Assert
            Assert.Equal("param", templatePart.Name);
            Assert.Equal("111111", templatePart.DefaultValue);

            Assert.Single(templatePart.InlineConstraints);
            Assert.Single(templatePart.InlineConstraints, c => c.Constraint == @"test(\d+)");
        }

        [Fact]
        public void ParseRouteParameter_ConstraintAndOptional_ParsedCorrectly()
        {
            // Arrange & Act
            var templatePart = ParseParameter(@"param:int?");

            // Assert
            Assert.Equal("param", templatePart.Name);
            Assert.True(templatePart.IsOptional);

            Assert.Single(templatePart.InlineConstraints);
            Assert.Single(templatePart.InlineConstraints, c => c.Constraint == @"int");
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

            Assert.Single(templatePart.InlineConstraints);
            Assert.Single(templatePart.InlineConstraints, c => c.Constraint == @"int");
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

            Assert.Single(templatePart.InlineConstraints);
            Assert.Single(templatePart.InlineConstraints, c => c.Constraint == @"int");
        }

        [Fact]
        public void ParseRouteParameter_ConstraintWithArgumentsAndOptional_ParsedCorrectly()
        {
            // Arrange & Act
            var templatePart = ParseParameter(@"param:test(\d+)?");

            // Assert
            Assert.Equal("param", templatePart.Name);
            Assert.True(templatePart.IsOptional);

            Assert.Single(templatePart.InlineConstraints);
            Assert.Single(templatePart.InlineConstraints, c => c.Constraint == @"test(\d+)");
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

            Assert.Single(templatePart.InlineConstraints);
            Assert.Single(templatePart.InlineConstraints, c => c.Constraint == @"test(\d+)");
        }

        [Fact]
        public void ParseRouteParameter_ChainedConstraints_ParsedCorrectly()
        {
            // Arrange & Act
            var templatePart = ParseParameter(@"param:test(d+):test(w+)");

            // Assert
            Assert.Equal("param", templatePart.Name);

            Assert.Equal(2, templatePart.InlineConstraints.Count());

            Assert.Single(templatePart.InlineConstraints, c => c.Constraint == @"test(d+)");
            Assert.Single(templatePart.InlineConstraints, c => c.Constraint == @"test(w+)");
        }

        [Fact]
        public void ParseRouteParameter_ChainedConstraints_DoubleDelimiters_ParsedCorrectly()
        {
            // Arrange & Act
            var templatePart = ParseParameter(@"param::test(d+)::test(w+)");

            // Assert
            Assert.Equal("param", templatePart.Name);

            Assert.Equal(4, templatePart.InlineConstraints.Count());

            Assert.Single(templatePart.InlineConstraints, c => c.Constraint == @"test(d+)");
            Assert.Single(templatePart.InlineConstraints, c => c.Constraint == @"test(w+)");
            Assert.Equal(2, templatePart.InlineConstraints.Count(c => c.Constraint == string.Empty));
        }

        [Fact]
        public void ParseRouteParameter_ChainedConstraints_ColonInPattern_ParsedCorrectly()
        {
            // Arrange & Act
            var templatePart = ParseParameter(@"param:test(\d+):test(\w:+)");

            // Assert
            Assert.Equal("param", templatePart.Name);

            Assert.Equal(2, templatePart.InlineConstraints.Count());

            Assert.Single(templatePart.InlineConstraints, c => c.Constraint == @"test(\d+)");
            Assert.Single(templatePart.InlineConstraints, c => c.Constraint == @"test(\w:+)");
        }

        [Fact]
        public void ParseRouteParameter_ChainedConstraints_WithDefaultValue_ParsedCorrectly()
        {
            // Arrange & Act
            var templatePart = ParseParameter(@"param:test(\d+):test(\w+)=qwer");

            // Assert
            Assert.Equal("param", templatePart.Name);

            Assert.Equal("qwer", templatePart.DefaultValue);

            Assert.Equal(2, templatePart.InlineConstraints.Count());

            Assert.Single(templatePart.InlineConstraints, c => c.Constraint == @"test(\d+)");
            Assert.Single(templatePart.InlineConstraints, c => c.Constraint == @"test(\w+)");
        }

        [Fact]
        public void ParseRouteParameter_ChainedConstraints_WithDefaultValue_DoubleDelimiters_ParsedCorrectly()
        {
            // Arrange & Act
            var templatePart = ParseParameter(@"param:test(\d+)::test(\w+)==qwer");

            // Assert
            Assert.Equal("param", templatePart.Name);

            Assert.Equal("=qwer", templatePart.DefaultValue);

            Assert.Equal(3, templatePart.InlineConstraints.Count());

            Assert.Single(templatePart.InlineConstraints, c => c.Constraint == @"test(\d+)");
            Assert.Single(templatePart.InlineConstraints, c => c.Constraint == @"test(\w+)");
            Assert.Single(templatePart.InlineConstraints, c => c.Constraint == string.Empty);
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

            Assert.Equal(2, param1.InlineConstraints.Count());
            Assert.Single(param1.InlineConstraints, c => c.Constraint == "int");
            Assert.Single(param1.InlineConstraints, c => c.Constraint == "test(3)");

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

            Assert.Single(templatePart.InlineConstraints);
            Assert.Single(templatePart.InlineConstraints, c => c.Constraint == @"test(\})");
        }

        [Fact]
        public void ParseRouteParameter_ConstraintWithClosingBraceInPattern_WithDefaultValue_ParsedCorrectly()
        {
            // Arrange & Act
            var templatePart = ParseParameter(@"param:test(\})=wer");

            // Assert
            Assert.Equal("param", templatePart.Name);

            Assert.Equal("wer", templatePart.DefaultValue);

            Assert.Single(templatePart.InlineConstraints);
            Assert.Single(templatePart.InlineConstraints, c => c.Constraint == @"test(\})");
        }

        [Fact]
        public void ParseRouteParameter_ConstraintWithClosingParenInPattern_ClosingParenIsParsedCorrectly()
        {
            // Arrange & Act
            var templatePart = ParseParameter(@"param:test(\))");

            // Assert
            Assert.Equal("param", templatePart.Name);

            Assert.Single(templatePart.InlineConstraints);
            Assert.Single(templatePart.InlineConstraints, c => c.Constraint == @"test(\))");
        }

        [Fact]
        public void ParseRouteParameter_ConstraintWithClosingParenInPattern_WithDefaultValue_ParsedCorrectly()
        {
            // Arrange & Act
            var templatePart = ParseParameter(@"param:test(\))=fsd");

            // Assert
            Assert.Equal("param", templatePart.Name);

            Assert.Equal("fsd", templatePart.DefaultValue);

            Assert.Single(templatePart.InlineConstraints);
            Assert.Single(templatePart.InlineConstraints, c => c.Constraint == @"test(\))");
        }

        [Fact]
        public void ParseRouteParameter_ConstraintWithColonInPattern_ColonIsParsedCorrectly()
        {
            // Arrange & Act
            var templatePart = ParseParameter(@"param:test(:)");

            // Assert
            Assert.Equal("param", templatePart.Name);

            Assert.Single(templatePart.InlineConstraints);
            Assert.Single(templatePart.InlineConstraints, c => c.Constraint == @"test(:)");
        }

        [Fact]
        public void ParseRouteParameter_ConstraintWithColonInPattern_WithDefaultValue_ParsedCorrectly()
        {
            // Arrange & Act
            var templatePart = ParseParameter(@"param:test(:)=mnf");

            // Assert
            Assert.Equal("param", templatePart.Name);

            Assert.Equal("mnf", templatePart.DefaultValue);

            Assert.Single(templatePart.InlineConstraints);
            Assert.Single(templatePart.InlineConstraints, c => c.Constraint == @"test(:)");
        }

        [Fact]
        public void ParseRouteParameter_ConstraintWithColonsInPattern_ParsedCorrectly()
        {
            // Arrange & Act
            var templatePart = ParseParameter(@"param:test(a:b:c)");

            // Assert
            Assert.Equal("param", templatePart.Name);

            Assert.Single(templatePart.InlineConstraints);
            Assert.Single(templatePart.InlineConstraints, c => c.Constraint == @"test(a:b:c)");
        }

        [Fact]
        public void ParseRouteParameter_ConstraintWithColonInParamName_ParsedCorrectly()
        {
            // Arrange & Act
            var templatePart = ParseParameter(@":param:test=12");

            // Assert
            Assert.Equal(":param", templatePart.Name);

            Assert.Equal("12", templatePart.DefaultValue);

            Assert.Single(templatePart.InlineConstraints);
            Assert.Single(templatePart.InlineConstraints, c => c.Constraint == "test");
        }

        [Fact]
        public void ParseRouteParameter_ConstraintWithTwoColonInParamName_ParsedCorrectly()
        {
            // Arrange & Act
            var templatePart = ParseParameter(@":param::test=12");

            // Assert
            Assert.Equal(":param", templatePart.Name);

            Assert.Equal("12", templatePart.DefaultValue);

            Assert.Equal(2, templatePart.InlineConstraints.Count());
            Assert.Single(templatePart.InlineConstraints, c => c.Constraint == "test");
            Assert.Single(templatePart.InlineConstraints, c => c.Constraint == "");
        }

        [Fact]
        public void ParseRouteParameter_EmptyConstraint_ParsedCorrectly()
        {
            // Arrange & Act
            var templatePart = ParseParameter(@":param:test:");

            // Assert
            Assert.Equal(":param", templatePart.Name);

            Assert.Equal(templatePart.InlineConstraints.Count(), 2);
            Assert.Single(templatePart.InlineConstraints, c => c.Constraint == "test");
            Assert.Single(templatePart.InlineConstraints, c => c.Constraint == "");
        }

        [Fact]
        public void ParseRouteParameter_ConstraintWithCommaInPattern_PatternIsParsedCorrectly()
        {
            // Arrange & Act
            var templatePart = ParseParameter(@"param:test(\w,\w)");

            // Assert
            Assert.Equal("param", templatePart.Name);

            Assert.Single(templatePart.InlineConstraints);
            Assert.Single(templatePart.InlineConstraints, c => c.Constraint == @"test(\w,\w)");
        }

        [Fact]
        public void ParseRouteParameter_ConstraintWithCommaInName_ParsedCorrectly()
        {
            // Arrange & Act
            var templatePart = ParseParameter(@"par,am:test(\w)");

            // Assert
            Assert.Equal("par,am", templatePart.Name);

            Assert.Single(templatePart.InlineConstraints);
            Assert.Single(templatePart.InlineConstraints, c => c.Constraint == @"test(\w)");
        }

        [Fact]
        public void ParseRouteParameter_ConstraintWithCommaInPattern_WithDefaultValue_ParsedCorrectly()
        {
            // Arrange & Act
            var templatePart = ParseParameter(@"param:test(\w,\w)=jsd");

            // Assert
            Assert.Equal("param", templatePart.Name);

            Assert.Equal("jsd", templatePart.DefaultValue);

            Assert.Single(templatePart.InlineConstraints);
            Assert.Single(templatePart.InlineConstraints, c => c.Constraint == @"test(\w,\w)");
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

            Assert.Single(templatePart.InlineConstraints);
            Assert.Single(templatePart.InlineConstraints, c => c.Constraint == @"int");
        }

        [Fact]
        public void ParseRouteParameter_ConstraintWithEqualsSignInPattern_PatternIsParsedCorrectly()
        {
            // Arrange & Act
            var templatePart = ParseParameter(@"param:test(=)");

            // Assert
            Assert.Equal("param", templatePart.Name);
            Assert.Null(templatePart.DefaultValue);

            Assert.Single(templatePart.InlineConstraints);
            Assert.Single(templatePart.InlineConstraints, c => c.Constraint == @"test(=)");
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

            Assert.Single(templatePart.InlineConstraints);
            Assert.Single(templatePart.InlineConstraints, c => c.Constraint == @"test(a==b)");
        }

        [Fact]
        public void ParseRouteParameter_ConstraintWithEqualEqualSignInPattern_WithDefaultValue_ParsedCorrectly()
        {
            // Arrange & Act
            var templatePart = ParseParameter(@"param:test(a==b)=dvds");

            // Assert
            Assert.Equal("param", templatePart.Name);
            Assert.Equal("dvds", templatePart.DefaultValue);

            Assert.Single(templatePart.InlineConstraints);
            Assert.Single(templatePart.InlineConstraints, c => c.Constraint == @"test(a==b)");
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

            Assert.Single(templatePart.InlineConstraints);
            Assert.Single(templatePart.InlineConstraints, c => c.Constraint == @"test(=)");
        }

        [Fact]
        public void ParseRouteParameter_ConstraintWithOpenBraceInPattern_PatternIsParsedCorrectly()
        {
            // Arrange & Act
            var templatePart = ParseParameter(@"param:test(\{)");

            // Assert
            Assert.Equal("param", templatePart.Name);

            Assert.Single(templatePart.InlineConstraints);
            Assert.Single(templatePart.InlineConstraints, c => c.Constraint == @"test(\{)");
        }

        [Fact]
        public void ParseRouteParameter_ConstraintWithOpenBraceInName_ParsedCorrectly()
        {
            // Arrange & Act
            var templatePart = ParseParameter(@"par{am:test(\sd)");

            // Assert
            Assert.Equal("par{am", templatePart.Name);

            Assert.Single(templatePart.InlineConstraints);
            Assert.Single(templatePart.InlineConstraints, c => c.Constraint == @"test(\sd)");
        }

        [Fact]
        public void ParseRouteParameter_ConstraintWithOpenBraceInPattern_WithDefaultValue_ParsedCorrectly()
        {
            // Arrange & Act
            var templatePart = ParseParameter(@"param:test(\{)=xvc");

            // Assert
            Assert.Equal("param", templatePart.Name);

            Assert.Equal("xvc", templatePart.DefaultValue);

            Assert.Single(templatePart.InlineConstraints);
            Assert.Single(templatePart.InlineConstraints, c => c.Constraint == @"test(\{)");
        }

        [Fact]
        public void ParseRouteParameter_ConstraintWithOpenParenInName_ParsedCorrectly()
        {
            // Arrange & Act
            var templatePart = ParseParameter(@"par(am:test(\()");

            // Assert
            Assert.Equal("par(am", templatePart.Name);

            Assert.Single(templatePart.InlineConstraints);
            Assert.Single(templatePart.InlineConstraints, c => c.Constraint == @"test(\()");
        }

        [Fact]
        public void ParseRouteParameter_ConstraintWithOpenParenInPattern_ParsedCorrectly()
        {
            // Arrange & Act
            var templatePart = ParseParameter(@"param:test(\()");

            // Assert
            Assert.Equal("param", templatePart.Name);

            Assert.Single(templatePart.InlineConstraints);
            Assert.Single(templatePart.InlineConstraints, c => c.Constraint == @"test(\()");
        }

        [Fact]
        public void ParseRouteParameter_ConstraintWithOpenParenNoCloseParen_ParsedCorrectly()
        {
            // Arrange & Act
            var templatePart = ParseParameter(@"param:test(#$%");

            // Assert
            Assert.Equal("param", templatePart.Name);

            Assert.Single(templatePart.InlineConstraints);
            Assert.Single(templatePart.InlineConstraints, c => c.Constraint == @"test(#$%");
        }

        [Fact]
        public void ParseRouteParameter_ConstraintWithOpenParenAndColon_ParsedCorrectly()
        {
            // Arrange & Act
            var templatePart = ParseParameter(@"param:test(#:test1");

            // Assert
            Assert.Equal("param", templatePart.Name);

            Assert.Equal(2, templatePart.InlineConstraints.Count());
            Assert.Single(templatePart.InlineConstraints, c => c.Constraint == @"test(#");
            Assert.Single(templatePart.InlineConstraints, c => c.Constraint == @"test1");
        }

        [Fact]
        public void ParseRouteParameter_ConstraintWithOpenParenInPattern_WithDefaultValue_ParsedCorrectly()
        {
            // Arrange & Act
            var templatePart = ParseParameter(@"param:test(\()=djk");

            // Assert
            Assert.Equal("param", templatePart.Name);

            Assert.Equal("djk", templatePart.DefaultValue);

            Assert.Single(templatePart.InlineConstraints);
            Assert.Single(templatePart.InlineConstraints, c => c.Constraint == @"test(\()");
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

            Assert.Single(templatePart.InlineConstraints);
            Assert.Single(templatePart.InlineConstraints, c => c.Constraint == @"test(\?)");
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

            Assert.Single(templatePart.InlineConstraints);
            Assert.Single(templatePart.InlineConstraints, c => c.Constraint == @"test(\?)");
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
            
            Assert.Single(templatePart.InlineConstraints);
            Assert.Single(templatePart.InlineConstraints, c => c.Constraint == @"test(\?)");
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

            Assert.Single(templatePart.InlineConstraints);
            Assert.Single(templatePart.InlineConstraints, c => c.Constraint == @"test(\?)");
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

            Assert.Single(templatePart.InlineConstraints);
            Assert.Single(templatePart.InlineConstraints, c => c.Constraint == @"test(\?)");
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

            Assert.Equal(templatePart.InlineConstraints.Count(), 2);
            Assert.Single(templatePart.InlineConstraints, c => c.Constraint == @"test(#)");
            Assert.Single(templatePart.InlineConstraints, c => c.Constraint == @"$)");
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

            Assert.Single(templatePart.InlineConstraints);
            Assert.Single(templatePart.InlineConstraints, c => c.Constraint == @"test(#:)$)");
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

            Assert.Single(templatePart.InlineConstraints);
            Assert.Single(templatePart.InlineConstraints, c => c.Constraint == @"regex(^\d{{3}}-\d{{3}}-\d{{4}}$)");
        }

        [Fact]
        public void ParseRouteParameter_ConstraintWithBraces_WithDefaultValue()
        {
            // Arrange & Act
            var templatePart = ParseParameter(@"p1:regex(^\d{{3}}-\d{{3}}-\d{{4}}$)=123-456-7890"); // ssn

            // Assert
            Assert.Equal("p1", templatePart.Name);
            Assert.Equal(templatePart.DefaultValue, "123-456-7890");
            Assert.False(templatePart.IsOptional);

            Assert.Single(templatePart.InlineConstraints);
            Assert.Single(templatePart.InlineConstraints, c => c.Constraint == @"regex(^\d{{3}}-\d{{3}}-\d{{4}}$)");
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
            return new DefaultInlineConstraintResolver(accessor);
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
                              IDictionary<string, object> values,
                              RouteDirection routeDirection)
            {
                throw new NotImplementedException();
            }
        }
    }
}
