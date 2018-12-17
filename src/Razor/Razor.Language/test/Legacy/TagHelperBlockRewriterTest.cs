// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    public class TagHelperBlockRewriterTest : TagHelperRewritingTestBase
    {
        public static TagHelperDescriptor[] SymbolBoundAttributes_Descriptors = new[]
        {
            TagHelperDescriptorBuilder.Create("CatchAllTagHelper", "SomeAssembly")
                .TagMatchingRuleDescriptor(rule =>
                    rule
                    .RequireTagName("*")
                    .RequireAttributeDescriptor(attribute => attribute.Name("bound")))
                .BoundAttributeDescriptor(attribute =>
                    attribute
                    .Name("[item]")
                    .PropertyName("ListItems")
                    .TypeName(typeof(List<string>).Namespace + "List<System.String>"))
                .BoundAttributeDescriptor(attribute =>
                    attribute
                    .Name("[(item)]")
                    .PropertyName("ArrayItems")
                    .TypeName(typeof(string[]).Namespace + "System.String[]"))
                .BoundAttributeDescriptor(attribute =>
                    attribute
                    .Name("(click)")
                    .PropertyName("Event1")
                    .TypeName(typeof(Action).FullName))
                .BoundAttributeDescriptor(attribute =>
                    attribute
                    .Name("(^click)")
                    .PropertyName("Event2")
                    .TypeName(typeof(Action).FullName))
                .BoundAttributeDescriptor(attribute =>
                    attribute
                    .Name("*something")
                    .PropertyName("StringProperty1")
                    .TypeName(typeof(string).FullName))
                .BoundAttributeDescriptor(attribute =>
                    attribute
                    .Name("#local")
                    .PropertyName("StringProperty2")
                    .TypeName(typeof(string).FullName))
                .Build()
        };

        [Fact]
        public void CanHandleSymbolBoundAttributes1()
        {
            EvaluateData(SymbolBoundAttributes_Descriptors, "<ul bound [item]='items'></ul>");
        }

        [Fact]
        public void CanHandleSymbolBoundAttributes2()
        {
            EvaluateData(SymbolBoundAttributes_Descriptors, "<ul bound [(item)]='items'></ul>");
        }

        [Fact]
        public void CanHandleSymbolBoundAttributes3()
        {
            EvaluateData(SymbolBoundAttributes_Descriptors, "<button bound (click)='doSomething()'>Click Me</button>");
        }

        [Fact]
        public void CanHandleSymbolBoundAttributes4()
        {
            EvaluateData(SymbolBoundAttributes_Descriptors, "<button bound (^click)='doSomething()'>Click Me</button>");
        }

        [Fact]
        public void CanHandleSymbolBoundAttributes5()
        {
            EvaluateData(SymbolBoundAttributes_Descriptors, "<template bound *something='value'></template>");
        }

        [Fact]
        public void CanHandleSymbolBoundAttributes6()
        {
            EvaluateData(SymbolBoundAttributes_Descriptors, "<div bound #localminimized></div>");
        }

        [Fact]
        public void CanHandleSymbolBoundAttributes7()
        {
            EvaluateData(SymbolBoundAttributes_Descriptors, "<div bound #local='value'></div>");
        }

        public static TagHelperDescriptor[] WithoutEndTag_Descriptors = new TagHelperDescriptor[]
        {
            TagHelperDescriptorBuilder.Create("InputTagHelper", "SomeAssembly")
                .TagMatchingRuleDescriptor(rule =>
                    rule
                    .RequireTagName("input")
                    .RequireTagStructure(TagStructure.WithoutEndTag))
                .Build()
        };

        [Fact]
        public void CanHandleWithoutEndTagTagStructure1()
        {
            EvaluateData(WithoutEndTag_Descriptors, "<input>");
        }

        [Fact]
        public void CanHandleWithoutEndTagTagStructure2()
        {
            EvaluateData(WithoutEndTag_Descriptors, "<input type='text'>");
        }

        [Fact]
        public void CanHandleWithoutEndTagTagStructure3()
        {
            EvaluateData(WithoutEndTag_Descriptors, "<input><input>");
        }

        [Fact]
        public void CanHandleWithoutEndTagTagStructure4()
        {
            EvaluateData(WithoutEndTag_Descriptors, "<input type='text'><input>");
        }

        [Fact]
        public void CanHandleWithoutEndTagTagStructure5()
        {
            EvaluateData(WithoutEndTag_Descriptors, "<div><input><input></div>");
        }

        public static TagHelperDescriptor[] GetTagStructureCompatibilityDescriptors(TagStructure structure1, TagStructure structure2)
        {
            var descriptors = new TagHelperDescriptor[]
            {
                TagHelperDescriptorBuilder.Create("InputTagHelper1", "SomeAssembly")
                    .TagMatchingRuleDescriptor(rule =>
                        rule
                        .RequireTagName("input")
                        .RequireTagStructure(structure1))
                    .Build(),
                TagHelperDescriptorBuilder.Create("InputTagHelper2", "SomeAssembly")
                    .TagMatchingRuleDescriptor(rule =>
                        rule
                        .RequireTagName("input")
                        .RequireTagStructure(structure2))
                    .Build()
            };

            return descriptors;
        }

        [Fact]
        public void AllowsCompatibleTagStructures1()
        {
            // Arrange
            var descriptors = GetTagStructureCompatibilityDescriptors(TagStructure.Unspecified, TagStructure.Unspecified);

            // Act & Assert
            EvaluateData(descriptors, "<input></input>");
        }

        [Fact]
        public void AllowsCompatibleTagStructures2()
        {
            // Arrange
            var descriptors = GetTagStructureCompatibilityDescriptors(TagStructure.Unspecified, TagStructure.Unspecified);

            // Act & Assert
            EvaluateData(descriptors, "<input />");
        }

        [Fact]
        public void AllowsCompatibleTagStructures3()
        {
            // Arrange
            var descriptors = GetTagStructureCompatibilityDescriptors(TagStructure.Unspecified, TagStructure.WithoutEndTag);

            // Act & Assert
            EvaluateData(descriptors, "<input type='text'>");
        }

        [Fact]
        public void AllowsCompatibleTagStructures4()
        {
            // Arrange
            var descriptors = GetTagStructureCompatibilityDescriptors(TagStructure.WithoutEndTag, TagStructure.WithoutEndTag);

            // Act & Assert
            EvaluateData(descriptors, "<input><input>");
        }

        [Fact]
        public void AllowsCompatibleTagStructures5()
        {
            // Arrange
            var descriptors = GetTagStructureCompatibilityDescriptors(TagStructure.Unspecified, TagStructure.NormalOrSelfClosing);

            // Act & Assert
            EvaluateData(descriptors, "<input type='text'></input>");
        }

        [Fact]
        public void AllowsCompatibleTagStructures6()
        {
            // Arrange
            var descriptors = GetTagStructureCompatibilityDescriptors(TagStructure.Unspecified, TagStructure.WithoutEndTag);

            // Act & Assert
            EvaluateData(descriptors, "<input />");
        }

        [Fact]
        public void AllowsCompatibleTagStructures7()
        {
            // Arrange
            var descriptors = GetTagStructureCompatibilityDescriptors(TagStructure.NormalOrSelfClosing, TagStructure.Unspecified);

            // Act & Assert
            EvaluateData(descriptors, "<input />");
        }

        [Fact]
        public void CreatesErrorForMalformedTagHelpersWithAttributes1()
        {
            RunParseTreeRewriterTest("<p class='", "strong", "p");
        }

        [Fact]
        public void CreatesErrorForMalformedTagHelpersWithAttributes2()
        {
            RunParseTreeRewriterTest("<p bar=\"false\"\" <strong>", "strong", "p");
        }

        [Fact]
        public void CreatesErrorForMalformedTagHelpersWithAttributes3()
        {
            RunParseTreeRewriterTest("<p bar='false  <strong>", "strong", "p");
        }

        [Fact]
        public void CreatesErrorForMalformedTagHelpersWithAttributes4()
        {
            RunParseTreeRewriterTest("<p bar='false  <strong'", "strong", "p");
        }

        [Fact]
        public void CreatesErrorForMalformedTagHelpersWithAttributes5()
        {
            RunParseTreeRewriterTest("<p bar=false'", "strong", "p");
        }

        [Fact]
        public void CreatesErrorForMalformedTagHelpersWithAttributes6()
        {
            RunParseTreeRewriterTest("<p bar=\"false'", "strong", "p");
        }

        [Fact]
        public void CreatesErrorForMalformedTagHelpersWithAttributes7()
        {
            RunParseTreeRewriterTest("<p bar=\"false' ></p>", "strong", "p");
        }

        [Fact]
        public void CreatesErrorForMalformedTagHelpersWithAttributes8()
        {
            RunParseTreeRewriterTest("<p foo bar<strong>", "strong", "p");
        }

        [Fact]
        public void CreatesErrorForMalformedTagHelpersWithAttributes9()
        {
            RunParseTreeRewriterTest("<p class=btn\" bar<strong>", "strong", "p");
        }

        [Fact]
        public void CreatesErrorForMalformedTagHelpersWithAttributes10()
        {
            RunParseTreeRewriterTest("<p class=btn\" bar=\"foo\"<strong>", "strong", "p");
        }

        [Fact]
        public void CreatesErrorForMalformedTagHelpersWithAttributes11()
        {
            RunParseTreeRewriterTest("<p class=\"btn bar=\"foo\"<strong>", "strong", "p");
        }

        [Fact]
        public void CreatesErrorForMalformedTagHelpersWithAttributes12()
        {
            RunParseTreeRewriterTest("<p class=\"btn bar=\"foo\"></p>", "strong", "p");
        }

        [Fact]
        public void CreatesErrorForMalformedTagHelpersWithAttributes13()
        {
            RunParseTreeRewriterTest("<p @DateTime.Now class=\"btn\"></p>", "strong", "p");
        }

        [Fact]
        public void CreatesErrorForMalformedTagHelpersWithAttributes14()
        {
            RunParseTreeRewriterTest("<p @DateTime.Now=\"btn\"></p>", "strong", "p");
        }

        [Fact]
        public void CreatesErrorForMalformedTagHelpersWithAttributes15()
        {
            RunParseTreeRewriterTest("<p class=@DateTime.Now\"></p>", "strong", "p");
        }

        [Fact]
        public void CreatesErrorForMalformedTagHelpersWithAttributes16()
        {
            RunParseTreeRewriterTest("<p class=\"@do {", "strong", "p");
        }

        [Fact]
        public void CreatesErrorForMalformedTagHelpersWithAttributes17()
        {
            RunParseTreeRewriterTest("<p class=\"@do {\"></p>", "strong", "p");
        }

        [Fact]
        public void CreatesErrorForMalformedTagHelpersWithAttributes18()
        {
            RunParseTreeRewriterTest("<p @do { someattribute=\"btn\"></p>", "strong", "p");
        }

        [Fact]
        public void CreatesErrorForMalformedTagHelpersWithAttributes19()
        {
            RunParseTreeRewriterTest("<p class=some=thing attr=\"@value\"></p>", "strong", "p");
        }

        [Fact]
        public void CreatesErrorForMalformedTagHelper1()
        {
            RunParseTreeRewriterTest("<p", "strong", "p");
        }

        [Fact]
        public void CreatesErrorForMalformedTagHelper2()
        {
            RunParseTreeRewriterTest("<p></p", "strong", "p");
        }

        [Fact]
        public void CreatesErrorForMalformedTagHelper3()
        {
            RunParseTreeRewriterTest("<p><strong", "strong", "p");
        }

        [Fact]
        public void CreatesErrorForMalformedTagHelper4()
        {
            RunParseTreeRewriterTest("<strong <p>", "strong", "p");
        }

        [Fact]
        public void CreatesErrorForMalformedTagHelper5()
        {
            RunParseTreeRewriterTest("<strong </strong", "strong", "p");
        }

        [Fact]
        public void CreatesErrorForMalformedTagHelper6()
        {
            RunParseTreeRewriterTest("<<</strong> <<p>", "strong", "p");
        }

        [Fact]
        public void CreatesErrorForMalformedTagHelper7()
        {
            RunParseTreeRewriterTest("<<<strong>> <<>>", "strong", "p");
        }

        [Fact]
        public void CreatesErrorForMalformedTagHelper8()
        {
            RunParseTreeRewriterTest("<str<strong></p></strong>", "strong", "p");
        }

        public static TagHelperDescriptor[] CodeTagHelperAttributes_Descriptors = new TagHelperDescriptor[]
        {
            TagHelperDescriptorBuilder.Create("PersonTagHelper", "personAssembly")
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("person"))
                .BoundAttributeDescriptor(attribute =>
                    attribute
                    .Name("age")
                    .PropertyName("Age")
                    .TypeName(typeof(int).FullName))
                .BoundAttributeDescriptor(attribute =>
                    attribute
                    .Name("birthday")
                    .PropertyName("BirthDay")
                    .TypeName(typeof(DateTime).FullName))
                .BoundAttributeDescriptor(attribute =>
                    attribute
                    .Name("name")
                    .PropertyName("Name")
                    .TypeName(typeof(string).FullName))
                .Build()
        };

        [Fact]
        public void CreatesMarkupCodeSpansForNonStringTagHelperAttributes1()
        {
            EvaluateData(CodeTagHelperAttributes_Descriptors, "<person age=\"12\" />");
        }

        [Fact]
        public void CreatesMarkupCodeSpansForNonStringTagHelperAttributes2()
        {
            EvaluateData(CodeTagHelperAttributes_Descriptors, "<person birthday=\"DateTime.Now\" />");
        }

        [Fact]
        public void CreatesMarkupCodeSpansForNonStringTagHelperAttributes3()
        {
            EvaluateData(CodeTagHelperAttributes_Descriptors, "<person age=\"@DateTime.Now.Year\" />");
        }

        [Fact]
        public void CreatesMarkupCodeSpansForNonStringTagHelperAttributes4()
        {
            EvaluateData(CodeTagHelperAttributes_Descriptors, "<person age=\" @DateTime.Now.Year\" />");
        }

        [Fact]
        public void CreatesMarkupCodeSpansForNonStringTagHelperAttributes5()
        {
            EvaluateData(CodeTagHelperAttributes_Descriptors, "<person name=\"John\" />");
        }

        [Fact]
        public void CreatesMarkupCodeSpansForNonStringTagHelperAttributes6()
        {
            EvaluateData(CodeTagHelperAttributes_Descriptors, "<person name=\"Time: @DateTime.Now\" />");
        }

        [Fact]
        public void CreatesMarkupCodeSpansForNonStringTagHelperAttributes7()
        {
            EvaluateData(CodeTagHelperAttributes_Descriptors, "<person age=\"1 + @value + 2\" birthday='(bool)@Bag[\"val\"] ? @@DateTime : @DateTime.Now'/>");
        }

        [Fact]
        public void CreatesMarkupCodeSpansForNonStringTagHelperAttributes8()
        {
            EvaluateData(CodeTagHelperAttributes_Descriptors, "<person age=\"12\" birthday=\"DateTime.Now\" name=\"Time: @DateTime.Now\" />");
        }

        [Fact]
        public void CreatesMarkupCodeSpansForNonStringTagHelperAttributes9()
        {
            EvaluateData(CodeTagHelperAttributes_Descriptors, "<person age=\"12\" birthday=\"DateTime.Now\" name=\"Time: @@ @DateTime.Now\" />");
        }

        [Fact]
        public void CreatesMarkupCodeSpansForNonStringTagHelperAttributes10()
        {
            EvaluateData(CodeTagHelperAttributes_Descriptors, "<person age=\"12\" birthday=\"DateTime.Now\" name=\"@@BoundStringAttribute\" />");
        }

        [Fact]
        public void CreatesMarkupCodeSpansForNonStringTagHelperAttributes11()
        {
            EvaluateData(CodeTagHelperAttributes_Descriptors, "<person age=\"@@@(11+1)\" birthday=\"DateTime.Now\" name=\"Time: @DateTime.Now\" />");
        }

        [Fact]
        public void TagHelperParseTreeRewriter_CreatesErrorForIncompleteTagHelper1()
        {
            RunParseTreeRewriterTest("<p class=foo dynamic=@DateTime.Now style=color:red;><strong></p></strong>", "strong", "p");
        }

        [Fact]
        public void TagHelperParseTreeRewriter_CreatesErrorForIncompleteTagHelper2()
        {
            RunParseTreeRewriterTest("<div><p>Hello <strong>World</strong></div>", "strong", "p");
        }

        [Fact]
        public void TagHelperParseTreeRewriter_CreatesErrorForIncompleteTagHelper3()
        {
            RunParseTreeRewriterTest("<div><p>Hello <strong>World</div>", "strong", "p");
        }

        [Fact]
        public void TagHelperParseTreeRewriter_CreatesErrorForIncompleteTagHelper4()
        {
            RunParseTreeRewriterTest("<p class=\"foo\">Hello <p style=\"color:red;\">World</p>", "strong", "p");
        }

        [Fact]
        public void TagHelperParseTreeRewriter_RewritesOddlySpacedTagHelperTagBlocks1()
        {
            RunParseTreeRewriterTest("<p      class=\"     foo\"    style=\"   color :  red  ;   \"    ></p>", "p");
        }

        [Fact]
        public void TagHelperParseTreeRewriter_RewritesOddlySpacedTagHelperTagBlocks2()
        {
            RunParseTreeRewriterTest("<p      class=\"     foo\"    style=\"   color :  red  ;   \"    >Hello World</p>", "p");
        }

        [Fact]
        public void TagHelperParseTreeRewriter_RewritesOddlySpacedTagHelperTagBlocks3()
        {
            RunParseTreeRewriterTest("<p     class=\"   foo  \" >Hello</p> <p    style=\"  color:red; \" >World</p>", "p");
        }

        [Fact]
        public void TagHelperParseTreeRewriter_RewritesComplexAttributeTagHelperTagBlocks1()
        {
            // Arrange
            var dateTimeNowString = "@DateTime.Now";
            var currentFormattedString = "<p class=\"{0}\" style='{0}'></p>";
            var document = string.Format(currentFormattedString, dateTimeNowString);

            // Act & Assert
            RunParseTreeRewriterTest(document, "p");
        }

        [Fact]
        public void TagHelperParseTreeRewriter_RewritesComplexAttributeTagHelperTagBlocks2()
        {
            // Arrange
            var doWhileString = "@do { var foo = bar; <text>Foo</text> foo++; } while (foo<bar>);";
            var currentFormattedString = "<p class=\"{0}\" style='{0}'></p>";
            var document = string.Format(currentFormattedString, doWhileString);

            // Act & Assert
            RunParseTreeRewriterTest(document, "p");
        }

        [Fact]
        public void TagHelperParseTreeRewriter_RewritesComplexAttributeTagHelperTagBlocks3()
        {
            // Arrange
            var dateTimeNowString = "@DateTime.Now";
            var currentFormattedString = "<p class=\"{0}\" style='{0}'>Hello World</p>";
            var document = string.Format(currentFormattedString, dateTimeNowString);

            // Act & Assert
            RunParseTreeRewriterTest(document, "p");
        }

        [Fact]
        public void TagHelperParseTreeRewriter_RewritesComplexAttributeTagHelperTagBlocks4()
        {
            // Arrange
            var doWhileString = "@do { var foo = bar; <text>Foo</text> foo++; } while (foo<bar>);";
            var currentFormattedString = "<p class=\"{0}\" style='{0}'>Hello World</p>";
            var document = string.Format(currentFormattedString, doWhileString);

            // Act & Assert
            RunParseTreeRewriterTest(document, "p");
        }

        [Fact]
        public void TagHelperParseTreeRewriter_RewritesComplexAttributeTagHelperTagBlocks5()
        {
            // Arrange
            var dateTimeNowString = "@DateTime.Now";
            var currentFormattedString = "<p class=\"{0}\">Hello</p> <p style='{0}'>World</p>";
            var document = string.Format(currentFormattedString, dateTimeNowString);

            // Act & Assert
            RunParseTreeRewriterTest(document, "p");
        }

        [Fact]
        public void TagHelperParseTreeRewriter_RewritesComplexAttributeTagHelperTagBlocks6()
        {
            // Arrange
            var doWhileString = "@do { var foo = bar; <text>Foo</text> foo++; } while (foo<bar>);";
            var currentFormattedString = "<p class=\"{0}\">Hello</p> <p style='{0}'>World</p>";
            var document = string.Format(currentFormattedString, doWhileString);

            // Act & Assert
            RunParseTreeRewriterTest(document, "p");
        }

        [Fact]
        public void TagHelperParseTreeRewriter_RewritesComplexAttributeTagHelperTagBlocks7()
        {
            // Arrange
            var dateTimeNowString = "@DateTime.Now";
            var currentFormattedString = "<p class=\"{0}\" style='{0}'>Hello World <strong class=\"{0}\">inside of strong tag</strong></p>";
            var document = string.Format(currentFormattedString, dateTimeNowString);

            // Act & Assert
            RunParseTreeRewriterTest(document, "p");
        }

        [Fact]
        public void TagHelperParseTreeRewriter_RewritesComplexTagHelperTagBlocks1()
        {
            // Arrange
            var dateTimeNowString = "@DateTime.Now";
            var currentFormattedString = "<p>{0}</p>";
            var document = string.Format(currentFormattedString, dateTimeNowString);

            // Act & Assert
            RunParseTreeRewriterTest(document, "p");
        }

        [Fact]
        public void TagHelperParseTreeRewriter_RewritesComplexTagHelperTagBlocks2()
        {
            // Arrange
            var doWhileString = "@do { var foo = bar; <p>Foo</p> foo++; } while (foo<bar>);";
            var currentFormattedString = "<p>{0}</p>";
            var document = string.Format(currentFormattedString, doWhileString);

            // Act & Assert
            RunParseTreeRewriterTest(document, "p");
        }

        [Fact]
        public void TagHelperParseTreeRewriter_RewritesComplexTagHelperTagBlocks3()
        {
            // Arrange
            var dateTimeNowString = "@DateTime.Now";
            var currentFormattedString = "<p>Hello World {0}</p>";
            var document = string.Format(currentFormattedString, dateTimeNowString);

            // Act & Assert
            RunParseTreeRewriterTest(document, "p");
        }

        [Fact]
        public void TagHelperParseTreeRewriter_RewritesComplexTagHelperTagBlocks4()
        {
            // Arrange
            var doWhileString = "@do { var foo = bar; <p>Foo</p> foo++; } while (foo<bar>);";
            var currentFormattedString = "<p>Hello World {0}</p>";
            var document = string.Format(currentFormattedString, doWhileString);

            // Act & Assert
            RunParseTreeRewriterTest(document, "p");
        }

        [Fact]
        public void TagHelperParseTreeRewriter_RewritesComplexTagHelperTagBlocks5()
        {
            // Arrange
            var dateTimeNowString = "@DateTime.Now";
            var currentFormattedString = "<p>{0}</p> <p>{0}</p>";
            var document = string.Format(currentFormattedString, dateTimeNowString);

            // Act & Assert
            RunParseTreeRewriterTest(document, "p");
        }

        [Fact]
        public void TagHelperParseTreeRewriter_RewritesComplexTagHelperTagBlocks6()
        {
            // Arrange
            var doWhileString = "@do { var foo = bar; <p>Foo</p> foo++; } while (foo<bar>);";
            var currentFormattedString = "<p>{0}</p> <p>{0}</p>";
            var document = string.Format(currentFormattedString, doWhileString);

            // Act & Assert
            RunParseTreeRewriterTest(document, "p");
        }

        [Fact]
        public void TagHelperParseTreeRewriter_RewritesComplexTagHelperTagBlocks7()
        {
            // Arrange
            var dateTimeNowString = "@DateTime.Now";
            var currentFormattedString = "<p>Hello {0}<strong>inside of {0} strong tag</strong></p>";
            var document = string.Format(currentFormattedString, dateTimeNowString);

            // Act & Assert
            RunParseTreeRewriterTest(document, "p");
        }

        [Fact]
        public void TagHelperParseTreeRewriter_RewritesComplexTagHelperTagBlocks8()
        {
            // Arrange
            var doWhileString = "@do { var foo = bar; <p>Foo</p> foo++; } while (foo<bar>);";
            var currentFormattedString = "<p>Hello {0}<strong>inside of {0} strong tag</strong></p>";
            var document = string.Format(currentFormattedString, doWhileString);

            // Act & Assert
            RunParseTreeRewriterTest(document, "p");
        }

        [Fact]
        public void TagHelperParseTreeRewriter_AllowsInvalidHtml1()
        {
            RunParseTreeRewriterTest("<<<p>>></p>", "p");
        }

        [Fact]
        public void TagHelperParseTreeRewriter_AllowsInvalidHtml2()
        {
            RunParseTreeRewriterTest("<<p />", "p");
        }

        [Fact]
        public void TagHelperParseTreeRewriter_AllowsInvalidHtml3()
        {
            RunParseTreeRewriterTest("< p />", "p");
        }

        [Fact]
        public void TagHelperParseTreeRewriter_AllowsInvalidHtml4()
        {
            RunParseTreeRewriterTest("<input <p />", "p");
        }

        [Fact]
        public void TagHelperParseTreeRewriter_AllowsInvalidHtml5()
        {
            RunParseTreeRewriterTest("< class=\"foo\" <p />", "p");
        }

        [Fact]
        public void TagHelperParseTreeRewriter_AllowsInvalidHtml6()
        {
            RunParseTreeRewriterTest("</<<p>/></p>>", "p");
        }

        [Fact]
        public void TagHelperParseTreeRewriter_AllowsInvalidHtml7()
        {
            RunParseTreeRewriterTest("</<<p>/><strong></p>>", "p");
        }

        [Fact]
        public void TagHelperParseTreeRewriter_AllowsInvalidHtml8()
        {
            RunParseTreeRewriterTest("</<<p>@DateTime.Now/><strong></p>>", "p");
        }

        [Fact]
        public void TagHelperParseTreeRewriter_AllowsInvalidHtml9()
        {
            RunParseTreeRewriterTest("</  /<  ><p>@DateTime.Now / ><strong></p></        >", "p");
        }

        [Fact]
        public void TagHelperParseTreeRewriter_AllowsInvalidHtml10()
        {
            RunParseTreeRewriterTest("<p>< @DateTime.Now ></ @DateTime.Now ></p>", "p");
        }

        [Fact]
        public void UnderstandsEmptyAttributeTagHelpers1()
        {
            RunParseTreeRewriterTest("<p class=\"\"></p>", "p");
        }

        [Fact]
        public void UnderstandsEmptyAttributeTagHelpers2()
        {
            RunParseTreeRewriterTest("<p class=''></p>", "p");
        }

        [Fact]
        public void UnderstandsEmptyAttributeTagHelpers3()
        {
            RunParseTreeRewriterTest("<p class=></p>", "p");
        }

        [Fact]
        public void UnderstandsEmptyAttributeTagHelpers4()
        {
            RunParseTreeRewriterTest("<p class1='' class2= class3=\"\" />", "p");
        }

        [Fact]
        public void UnderstandsEmptyAttributeTagHelpers5()
        {
            RunParseTreeRewriterTest("<p class1=''class2=\"\"class3= />", "p");
        }

        public static TagHelperDescriptor[] EmptyTagHelperBoundAttribute_Descriptors = new TagHelperDescriptor[]
        {
            TagHelperDescriptorBuilder.Create("mythTagHelper", "SomeAssembly")
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("myth"))
                .BoundAttributeDescriptor(attribute =>
                    attribute
                    .Name("bound")
                    .PropertyName("Bound")
                    .TypeName(typeof(bool).FullName))
                .BoundAttributeDescriptor(attribute =>
                    attribute
                    .Name("name")
                    .PropertyName("Name")
                    .TypeName(typeof(string).FullName))
                .Build()
        };

        [Fact]
        public void CreatesErrorForEmptyTagHelperBoundAttributes1()
        {
            EvaluateData(EmptyTagHelperBoundAttribute_Descriptors, "<myth bound='' />");
        }

        [Fact]
        public void CreatesErrorForEmptyTagHelperBoundAttributes2()
        {
            EvaluateData(EmptyTagHelperBoundAttribute_Descriptors, "<myth bound='    true' />");
        }

        [Fact]
        public void CreatesErrorForEmptyTagHelperBoundAttributes3()
        {
            EvaluateData(EmptyTagHelperBoundAttribute_Descriptors, "<myth bound='    ' />");
        }

        [Fact]
        public void CreatesErrorForEmptyTagHelperBoundAttributes4()
        {
            EvaluateData(EmptyTagHelperBoundAttribute_Descriptors, "<myth bound=''  bound=\"\" />");
        }

        [Fact]
        public void CreatesErrorForEmptyTagHelperBoundAttributes5()
        {
            EvaluateData(EmptyTagHelperBoundAttribute_Descriptors, "<myth bound=' '  bound=\"  \" />");
        }

        [Fact]
        public void CreatesErrorForEmptyTagHelperBoundAttributes6()
        {
            EvaluateData(EmptyTagHelperBoundAttribute_Descriptors, "<myth bound='true' bound=  />");
        }

        [Fact]
        public void CreatesErrorForEmptyTagHelperBoundAttributes7()
        {
            EvaluateData(EmptyTagHelperBoundAttribute_Descriptors, "<myth bound= name='' />");
        }

        [Fact]
        public void CreatesErrorForEmptyTagHelperBoundAttributes8()
        {
            EvaluateData(EmptyTagHelperBoundAttribute_Descriptors, "<myth bound= name='  ' />");
        }

        [Fact]
        public void CreatesErrorForEmptyTagHelperBoundAttributes9()
        {
            EvaluateData(EmptyTagHelperBoundAttribute_Descriptors, "<myth bound='true' name='john' bound= name= />");
        }

        [Fact]
        public void CreatesErrorForEmptyTagHelperBoundAttributes10()
        {
            EvaluateData(EmptyTagHelperBoundAttribute_Descriptors, "<myth BouND='' />");
        }

        [Fact]
        public void CreatesErrorForEmptyTagHelperBoundAttributes11()
        {
            EvaluateData(EmptyTagHelperBoundAttribute_Descriptors, "<myth BOUND=''    bOUnd=\"\" />");
        }

        [Fact]
        public void CreatesErrorForEmptyTagHelperBoundAttributes12()
        {
            EvaluateData(EmptyTagHelperBoundAttribute_Descriptors, "<myth BOUND= nAMe='john'></myth>");
        }

        [Fact]
        public void CreatesErrorForEmptyTagHelperBoundAttributes13()
        {
            EvaluateData(EmptyTagHelperBoundAttribute_Descriptors, "<myth bound='    @true  ' />");
        }

        [Fact]
        public void CreatesErrorForEmptyTagHelperBoundAttributes14()
        {
            EvaluateData(EmptyTagHelperBoundAttribute_Descriptors, "<myth bound='    @(true)  ' />");
        }

        [Fact]
        public void TagHelperParseTreeRewriter_RewritesScriptTagHelpers1()
        {
            RunParseTreeRewriterTest("<script><script></foo></script>", "p", "div", "script");
        }

        [Fact]
        public void TagHelperParseTreeRewriter_RewritesScriptTagHelpers2()
        {
            RunParseTreeRewriterTest("<script>Hello World <div></div></script>", "p", "div", "script");
        }

        [Fact]
        public void TagHelperParseTreeRewriter_RewritesScriptTagHelpers3()
        {
            RunParseTreeRewriterTest("<script>Hel<p>lo</p></script> <p><div>World</div></p>", "p", "div", "script");
        }

        [Fact]
        public void TagHelperParseTreeRewriter_RewritesScriptTagHelpers4()
        {
            RunParseTreeRewriterTest("<script>Hel<strong>lo</strong></script> <script><span>World</span></script>", "p", "div", "script");
        }

        [Fact]
        public void TagHelperParseTreeRewriter_RewritesScriptTagHelpers5()
        {
            RunParseTreeRewriterTest("<script class=\"foo\" style=\"color:red;\" />", "p", "div", "script");
        }

        [Fact]
        public void TagHelperParseTreeRewriter_RewritesScriptTagHelpers6()
        {
            RunParseTreeRewriterTest("<p>Hello <script class=\"foo\" style=\"color:red;\"></script> World</p>", "p", "div", "script");
        }

        [Fact]
        public void TagHelperParseTreeRewriter_RewritesScriptTagHelpers7()
        {
            RunParseTreeRewriterTest("<p>Hello <script class=\"@@foo@bar.com\" style=\"color:red;\"></script> World</p>", "p", "div", "script");
        }

        [Fact]
        public void TagHelperParseTreeRewriter_RewritesSelfClosingTagHelpers1()
        {
            RunParseTreeRewriterTest("<p class=\"foo\" style=\"color:red;\" />", "p");
        }


        [Fact]
        public void TagHelperParseTreeRewriter_RewritesSelfClosingTagHelpers2()
        {
            RunParseTreeRewriterTest("<p>Hello <p class=\"foo\" style=\"color:red;\" /> World</p>", "p");
        }


        [Fact]
        public void TagHelperParseTreeRewriter_RewritesSelfClosingTagHelpers3()
        {
            RunParseTreeRewriterTest("Hello<p class=\"foo\" /> <p style=\"color:red;\" />World", "p");
        }

        [Fact]
        public void TagHelperParseTreeRewriter_RewritesTagHelpersWithQuotelessAttributes1()
        {
            RunParseTreeRewriterTest("<p class=foo dynamic=@DateTime.Now style=color:red;></p>", "p");
        }

        [Fact]
        public void TagHelperParseTreeRewriter_RewritesTagHelpersWithQuotelessAttributes2()
        {
            RunParseTreeRewriterTest("<p class=foo dynamic=@DateTime.Now style=color:red;>Hello World</p>", "p");
        }

        [Fact]
        public void TagHelperParseTreeRewriter_RewritesTagHelpersWithQuotelessAttributes3()
        {
            RunParseTreeRewriterTest("<p class=foo dynamic=@DateTime.Now style=color@@:red;>Hello World</p>", "p");
        }

        [Fact]
        public void TagHelperParseTreeRewriter_RewritesTagHelpersWithQuotelessAttributes4()
        {
            RunParseTreeRewriterTest("<p class=foo dynamic=@DateTime.Now>Hello</p> <p style=color:red; dynamic=@DateTime.Now>World</p>", "p");
        }

        [Fact]
        public void TagHelperParseTreeRewriter_RewritesTagHelpersWithQuotelessAttributes5()
        {
            RunParseTreeRewriterTest("<p class=foo dynamic=@DateTime.Now style=color:red;>Hello World <strong class=\"foo\">inside of strong tag</strong></p>", "p");
        }

        [Fact]
        public void TagHelperParseTreeRewriter_RewritesTagHelpersWithPlainAttributes1()
        {
            RunParseTreeRewriterTest("<p class=\"foo\" style=\"color:red;\"></p>", "p");
        }

        [Fact]
        public void TagHelperParseTreeRewriter_RewritesTagHelpersWithPlainAttributes2()
        {
            RunParseTreeRewriterTest("<p class=\"foo\" style=\"color:red;\">Hello World</p>", "p");
        }

        [Fact]
        public void TagHelperParseTreeRewriter_RewritesTagHelpersWithPlainAttributes3()
        {
            RunParseTreeRewriterTest("<p class=\"foo\">Hello</p> <p style=\"color:red;\">World</p>", "p");
        }

        [Fact]
        public void TagHelperParseTreeRewriter_RewritesTagHelpersWithPlainAttributes4()
        {
            RunParseTreeRewriterTest("<p class=\"foo\" style=\"color:red;\">Hello World <strong class=\"foo\">inside of strong tag</strong></p>", "p");
        }

        [Fact]
        public void TagHelperParseTreeRewriter_RewritesPlainTagHelperTagBlocks1()
        {
            RunParseTreeRewriterTest("<p></p>", "p");
        }

        [Fact]
        public void TagHelperParseTreeRewriter_RewritesPlainTagHelperTagBlocks2()
        {
            RunParseTreeRewriterTest("<p>Hello World</p>", "p");
        }

        [Fact]
        public void TagHelperParseTreeRewriter_RewritesPlainTagHelperTagBlocks3()
        {
            RunParseTreeRewriterTest("<p>Hello</p> <p>World</p>", "p");
        }

        [Fact]
        public void TagHelperParseTreeRewriter_RewritesPlainTagHelperTagBlocks4()
        {
            RunParseTreeRewriterTest("<p>Hello World <strong>inside of strong tag</strong></p>", "p");
        }

        [Fact]
        public void GeneratesExpectedOutputForUnboundDataDashAttributes_Document1()
        {
            // Arrange
            var dateTimeNowString = "@DateTime.Now";
            var document = $"<input data-required='{dateTimeNowString}' />";

            // Act & Assert
            RunParseTreeRewriterTest(document, "input");
        }

        [Fact]
        public void GeneratesExpectedOutputForUnboundDataDashAttributes_Document2()
        {
            // Arrange
            var document = "<input data-required='value' />";

            // Act & Assert
            RunParseTreeRewriterTest(document, "input");
        }

        [Fact]
        public void GeneratesExpectedOutputForUnboundDataDashAttributes_Document3()
        {
            // Arrange
            var dateTimeNowString = "@DateTime.Now";
            var document = $"<input data-required='prefix {dateTimeNowString}' />";

            // Act & Assert
            RunParseTreeRewriterTest(document, "input");
        }

        [Fact]
        public void GeneratesExpectedOutputForUnboundDataDashAttributes_Document4()
        {
            // Arrange
            var dateTimeNowString = "@DateTime.Now";
            var document = $"<input data-required='{dateTimeNowString} suffix' />";

            // Act & Assert
            RunParseTreeRewriterTest(document, "input");
        }

        [Fact]
        public void GeneratesExpectedOutputForUnboundDataDashAttributes_Document5()
        {
            // Arrange
            var dateTimeNowString = "@DateTime.Now";
            var document = $"<input data-required='prefix {dateTimeNowString} suffix' />";

            // Act & Assert
            RunParseTreeRewriterTest(document, "input");
        }

        [Fact]
        public void GeneratesExpectedOutputForUnboundDataDashAttributes_Document6()
        {
            // Arrange
            var dateTimeNowString = "@DateTime.Now";
            var document = $"<input pre-attribute data-required='prefix {dateTimeNowString} suffix' post-attribute />";

            // Act & Assert
            RunParseTreeRewriterTest(document, "input");
        }

        [Fact]
        public void GeneratesExpectedOutputForUnboundDataDashAttributes_Document7()
        {
            // Arrange
            var dateTimeNowString = "@DateTime.Now";
            var document = $"<input data-required='{dateTimeNowString} middle {dateTimeNowString}' />";

            // Act & Assert
            RunParseTreeRewriterTest(document, "input");
        }

        [Fact]
        public void GeneratesExpectedOutputForUnboundDataDashAttributes_Block1()
        {
            // Arrange
            var dateTimeNowString = "@DateTime.Now";
            var document = $"<input data-required='{dateTimeNowString}' />";

            // Wrap in a CSharp block
            document = $"@{{{document}}}";

            // Act & Assert
            RunParseTreeRewriterTest(document, "input");
        }

        [Fact]
        public void GeneratesExpectedOutputForUnboundDataDashAttributes_Block2()
        {
            // Arrange
            var document = "<input data-required='value' />";

            // Wrap in a CSharp block
            document = $"@{{{document}}}";

            // Act & Assert
            RunParseTreeRewriterTest(document, "input");
        }

        [Fact]
        public void GeneratesExpectedOutputForUnboundDataDashAttributes_Block3()
        {
            // Arrange
            var dateTimeNowString = "@DateTime.Now";
            var document = $"<input data-required='prefix {dateTimeNowString}' />";

            // Wrap in a CSharp block
            document = $"@{{{document}}}";

            // Act & Assert
            RunParseTreeRewriterTest(document, "input");
        }

        [Fact]
        public void GeneratesExpectedOutputForUnboundDataDashAttributes_Block4()
        {
            // Arrange
            var dateTimeNowString = "@DateTime.Now";
            var document = $"<input data-required='{dateTimeNowString} suffix' />";

            // Wrap in a CSharp block
            document = $"@{{{document}}}";

            // Act & Assert
            RunParseTreeRewriterTest(document, "input");
        }

        [Fact]
        public void GeneratesExpectedOutputForUnboundDataDashAttributes_Block5()
        {
            // Arrange
            var dateTimeNowString = "@DateTime.Now";
            var document = $"<input data-required='prefix {dateTimeNowString} suffix' />";

            // Wrap in a CSharp block
            document = $"@{{{document}}}";

            // Act & Assert
            RunParseTreeRewriterTest(document, "input");
        }

        [Fact]
        public void GeneratesExpectedOutputForUnboundDataDashAttributes_Block6()
        {
            // Arrange
            var dateTimeNowString = "@DateTime.Now";
            var document = $"<input pre-attribute data-required='prefix {dateTimeNowString} suffix' post-attribute />";

            // Wrap in a CSharp block
            document = $"@{{{document}}}";

            // Act & Assert
            RunParseTreeRewriterTest(document, "input");
        }

        [Fact]
        public void GeneratesExpectedOutputForUnboundDataDashAttributes_Block7()
        {
            // Arrange
            var dateTimeNowString = "@DateTime.Now";
            var document = $"<input data-required='{dateTimeNowString} middle {dateTimeNowString}' />";

            // Wrap in a CSharp block
            document = $"@{{{document}}}";

            // Act & Assert
            RunParseTreeRewriterTest(document, "input");
        }

        public static TagHelperDescriptor[] MinimizedAttribute_Descriptors = new TagHelperDescriptor[]
        {
            TagHelperDescriptorBuilder.Create("InputTagHelper1", "SomeAssembly")
                .TagMatchingRuleDescriptor(rule =>
                    rule
                    .RequireTagName("input")
                    .RequireAttributeDescriptor(attribute => attribute.Name("unbound-required")))
                .TagMatchingRuleDescriptor(rule =>
                    rule
                    .RequireTagName("input")
                    .RequireAttributeDescriptor(attribute => attribute.Name("bound-required-string")))
                .BoundAttributeDescriptor(attribute =>
                    attribute
                    .Name("bound-required-string")
                    .PropertyName("BoundRequiredString")
                    .TypeName(typeof(string).FullName))
                .Build(),
            TagHelperDescriptorBuilder.Create("InputTagHelper2", "SomeAssembly")
                .TagMatchingRuleDescriptor(rule =>
                    rule
                    .RequireTagName("input")
                    .RequireAttributeDescriptor(attribute => attribute.Name("bound-required-int")))
                .BoundAttributeDescriptor(attribute =>
                    attribute
                    .Name("bound-required-int")
                    .PropertyName("BoundRequiredInt")
                    .TypeName(typeof(int).FullName))
                .Build(),
            TagHelperDescriptorBuilder.Create("InputTagHelper3", "SomeAssembly")
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("input"))
                .BoundAttributeDescriptor(attribute =>
                    attribute
                    .Name("int-dictionary")
                    .PropertyName("DictionaryOfIntProperty")
                    .TypeName(typeof(IDictionary<string, int>).Namespace + ".IDictionary<System.String, System.Int32>")
                    .AsDictionaryAttribute("int-prefix-", typeof(int).FullName))
                .BoundAttributeDescriptor(attribute =>
                    attribute
                    .Name("string-dictionary")
                    .PropertyName("DictionaryOfStringProperty")
                    .TypeName(typeof(IDictionary<string, string>).Namespace + ".IDictionary<System.String, System.String>")
                    .AsDictionaryAttribute("string-prefix-", typeof(string).FullName))
                .Build(),
            TagHelperDescriptorBuilder.Create("PTagHelper", "SomeAssembly")
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("p"))
                .BoundAttributeDescriptor(attribute =>
                    attribute
                    .Name("bound-string")
                    .PropertyName("BoundRequiredString")
                    .TypeName(typeof(string).FullName))
                .BoundAttributeDescriptor(attribute =>
                    attribute
                    .Name("bound-int")
                    .PropertyName("BoundRequiredString")
                    .TypeName(typeof(int).FullName))
                .Build(),
        };

        [Fact]
        public void UnderstandsMinimizedAttributes_Document1()
        {
            // Arrange
            var document = "<input unbound-required />";

            // Act & Assert
            EvaluateData(MinimizedAttribute_Descriptors, document);
        }

        [Fact]
        public void UnderstandsMinimizedAttributes_Document2()
        {
            // Arrange
            var document = "<p bound-string></p>";

            // Act & Assert
            EvaluateData(MinimizedAttribute_Descriptors, document);
        }

        [Fact]
        public void UnderstandsMinimizedAttributes_Document3()
        {
            // Arrange
            var document = "<input bound-required-string />";

            // Act & Assert
            EvaluateData(MinimizedAttribute_Descriptors, document);
        }

        [Fact]
        public void UnderstandsMinimizedAttributes_Document4()
        {
            // Arrange
            var document = "<input bound-required-int />";

            // Act & Assert
            EvaluateData(MinimizedAttribute_Descriptors, document);
        }

        [Fact]
        public void UnderstandsMinimizedAttributes_Document5()
        {
            // Arrange
            var document = "<p bound-int></p>";

            // Act & Assert
            EvaluateData(MinimizedAttribute_Descriptors, document);
        }

        [Fact]
        public void UnderstandsMinimizedAttributes_Document6()
        {
            // Arrange
            var document = "<input int-dictionary/>";

            // Act & Assert
            EvaluateData(MinimizedAttribute_Descriptors, document);
        }

        [Fact]
        public void UnderstandsMinimizedAttributes_Document7()
        {
            // Arrange
            var document = "<input string-dictionary />";

            // Act & Assert
            EvaluateData(MinimizedAttribute_Descriptors, document);
        }

        [Fact]
        public void UnderstandsMinimizedAttributes_Document8()
        {
            // Arrange
            var document = "<input int-prefix- />";

            // Act & Assert
            EvaluateData(MinimizedAttribute_Descriptors, document);
        }

        [Fact]
        public void UnderstandsMinimizedAttributes_Document9()
        {
            // Arrange
            var document = "<input string-prefix-/>";

            // Act & Assert
            EvaluateData(MinimizedAttribute_Descriptors, document);
        }

        [Fact]
        public void UnderstandsMinimizedAttributes_Document10()
        {
            // Arrange
            var document = "<input int-prefix-value/>";

            // Act & Assert
            EvaluateData(MinimizedAttribute_Descriptors, document);
        }

        [Fact]
        public void UnderstandsMinimizedAttributes_Document11()
        {
            // Arrange
            var document = "<input string-prefix-value />";

            // Act & Assert
            EvaluateData(MinimizedAttribute_Descriptors, document);
        }

        [Fact]
        public void UnderstandsMinimizedAttributes_Document12()
        {
            // Arrange
            var document = "<input int-prefix-value='' />";

            // Act & Assert
            EvaluateData(MinimizedAttribute_Descriptors, document);
        }

        [Fact]
        public void UnderstandsMinimizedAttributes_Document13()
        {
            // Arrange
            var document = "<input string-prefix-value=''/>";

            // Act & Assert
            EvaluateData(MinimizedAttribute_Descriptors, document);
        }

        [Fact]
        public void UnderstandsMinimizedAttributes_Document14()
        {
            // Arrange
            var document = "<input int-prefix-value='3'/>";

            // Act & Assert
            EvaluateData(MinimizedAttribute_Descriptors, document);
        }

        [Fact]
        public void UnderstandsMinimizedAttributes_Document15()
        {
            // Arrange
            var document = "<input string-prefix-value='some string' />";

            // Act & Assert
            EvaluateData(MinimizedAttribute_Descriptors, document);
        }

        [Fact]
        public void UnderstandsMinimizedAttributes_Document16()
        {
            // Arrange
            var document = "<input unbound-required bound-required-string />";

            // Act & Assert
            EvaluateData(MinimizedAttribute_Descriptors, document);
        }

        [Fact]
        public void UnderstandsMinimizedAttributes_Document17()
        {
            // Arrange
            var document = "<p bound-int bound-string></p>";

            // Act & Assert
            EvaluateData(MinimizedAttribute_Descriptors, document);
        }

        [Fact]
        public void UnderstandsMinimizedAttributes_Document18()
        {
            // Arrange
            var document = "<input bound-required-int unbound-required bound-required-string />";

            // Act & Assert
            EvaluateData(MinimizedAttribute_Descriptors, document);
        }

        [Fact]
        public void UnderstandsMinimizedAttributes_Document19()
        {
            // Arrange
            var document = "<p bound-int bound-string bound-string></p>";

            // Act & Assert
            EvaluateData(MinimizedAttribute_Descriptors, document);
        }

        [Fact]
        public void UnderstandsMinimizedAttributes_Document20()
        {
            // Arrange
            var document = "<input unbound-required class='btn' />";

            // Act & Assert
            EvaluateData(MinimizedAttribute_Descriptors, document);
        }

        [Fact]
        public void UnderstandsMinimizedAttributes_Document21()
        {
            // Arrange
            var document = "<p bound-string class='btn'></p>";

            // Act & Assert
            EvaluateData(MinimizedAttribute_Descriptors, document);
        }

        [Fact]
        public void UnderstandsMinimizedAttributes_Document22()
        {
            // Arrange
            var document = "<input class='btn' unbound-required />";

            // Act & Assert
            EvaluateData(MinimizedAttribute_Descriptors, document);
        }

        [Fact]
        public void UnderstandsMinimizedAttributes_Document23()
        {
            // Arrange
            var document = "<p class='btn' bound-string></p>";

            // Act & Assert
            EvaluateData(MinimizedAttribute_Descriptors, document);
        }

        [Fact]
        public void UnderstandsMinimizedAttributes_Document24()
        {
            // Arrange
            var document = "<input bound-required-string class='btn' />";

            // Act & Assert
            EvaluateData(MinimizedAttribute_Descriptors, document);
        }

        [Fact]
        public void UnderstandsMinimizedAttributes_Document25()
        {
            // Arrange
            var document = "<input class='btn' bound-required-string />";

            // Act & Assert
            EvaluateData(MinimizedAttribute_Descriptors, document);
        }

        [Fact]
        public void UnderstandsMinimizedAttributes_Document26()
        {
            // Arrange
            var document = "<input bound-required-int class='btn' />";

            // Act & Assert
            EvaluateData(MinimizedAttribute_Descriptors, document);
        }

        [Fact]
        public void UnderstandsMinimizedAttributes_Document27()
        {
            // Arrange
            var document = "<p bound-int class='btn'></p>";

            // Act & Assert
            EvaluateData(MinimizedAttribute_Descriptors, document);
        }

        [Fact]
        public void UnderstandsMinimizedAttributes_Document28()
        {
            // Arrange
            var document = "<input class='btn' bound-required-int />";

            // Act & Assert
            EvaluateData(MinimizedAttribute_Descriptors, document);
        }

        [Fact]
        public void UnderstandsMinimizedAttributes_Document29()
        {
            // Arrange
            var document = "<p class='btn' bound-int></p>";

            // Act & Assert
            EvaluateData(MinimizedAttribute_Descriptors, document);
        }

        [Fact]
        public void UnderstandsMinimizedAttributes_Document30()
        {
            // Arrange
            var expressionString = "@DateTime.Now + 1";
            var document = $"<input class='{expressionString}' bound-required-int />";

            // Act & Assert
            EvaluateData(MinimizedAttribute_Descriptors, document);
        }

        [Fact]
        public void UnderstandsMinimizedAttributes_Document31()
        {
            // Arrange
            var expressionString = "@DateTime.Now + 1";
            var document = $"<p class='{expressionString}' bound-int></p>";

            // Act & Assert
            EvaluateData(MinimizedAttribute_Descriptors, document);
        }

        [Fact]
        public void UnderstandsMinimizedAttributes_Document32()
        {
            // Arrange
            var expressionString = "@DateTime.Now + 1";
            var document = $"<input    bound-required-int class='{expressionString}'   bound-required-string class='{expressionString}'  unbound-required  />";

            // Act & Assert
            EvaluateData(MinimizedAttribute_Descriptors, document);
        }

        [Fact]
        public void UnderstandsMinimizedAttributes_Document33()
        {
            // Arrange
            var expressionString = "@DateTime.Now + 1";
            var document = $"<p    bound-int class='{expressionString}'   bound-string class='{expressionString}'  bound-string></p>";

            // Act & Assert
            EvaluateData(MinimizedAttribute_Descriptors, document);
        }

        [Fact]
        public void UnderstandsMinimizedAttributes_Block1()
        {
            // Arrange
            var document = "<input unbound-required />";

            // Wrap in a CSharp block
            document = $"@{{{document}}}";

            // Act & Assert
            EvaluateData(MinimizedAttribute_Descriptors, document);
        }

        [Fact]
        public void UnderstandsMinimizedAttributes_Block2()
        {
            // Arrange
            var document = "<p bound-string></p>";

            // Wrap in a CSharp block
            document = $"@{{{document}}}";

            // Act & Assert
            EvaluateData(MinimizedAttribute_Descriptors, document);
        }

        [Fact]
        public void UnderstandsMinimizedAttributes_Block3()
        {
            // Arrange
            var document = "<input bound-required-string />";

            // Wrap in a CSharp block
            document = $"@{{{document}}}";

            // Act & Assert
            EvaluateData(MinimizedAttribute_Descriptors, document);
        }

        [Fact]
        public void UnderstandsMinimizedAttributes_Block4()
        {
            // Arrange
            var document = "<input bound-required-int />";

            // Wrap in a CSharp block
            document = $"@{{{document}}}";

            // Act & Assert
            EvaluateData(MinimizedAttribute_Descriptors, document);
        }

        [Fact]
        public void UnderstandsMinimizedAttributes_Block5()
        {
            // Arrange
            var document = "<p bound-int></p>";

            // Wrap in a CSharp block
            document = $"@{{{document}}}";

            // Act & Assert
            EvaluateData(MinimizedAttribute_Descriptors, document);
        }

        [Fact]
        public void UnderstandsMinimizedAttributes_Block6()
        {
            // Arrange
            var document = "<input int-dictionary/>";

            // Wrap in a CSharp block
            document = $"@{{{document}}}";

            // Act & Assert
            EvaluateData(MinimizedAttribute_Descriptors, document);
        }

        [Fact]
        public void UnderstandsMinimizedAttributes_Block7()
        {
            // Arrange
            var document = "<input string-dictionary />";

            // Wrap in a CSharp block
            document = $"@{{{document}}}";

            // Act & Assert
            EvaluateData(MinimizedAttribute_Descriptors, document);
        }

        [Fact]
        public void UnderstandsMinimizedAttributes_Block8()
        {
            // Arrange
            var document = "<input int-prefix- />";

            // Wrap in a CSharp block
            document = $"@{{{document}}}";

            // Act & Assert
            EvaluateData(MinimizedAttribute_Descriptors, document);
        }

        [Fact]
        public void UnderstandsMinimizedAttributes_Block9()
        {
            // Arrange
            var document = "<input string-prefix-/>";

            // Wrap in a CSharp block
            document = $"@{{{document}}}";

            // Act & Assert
            EvaluateData(MinimizedAttribute_Descriptors, document);
        }

        [Fact]
        public void UnderstandsMinimizedAttributes_Block10()
        {
            // Arrange
            var document = "<input int-prefix-value/>";

            // Wrap in a CSharp block
            document = $"@{{{document}}}";

            // Act & Assert
            EvaluateData(MinimizedAttribute_Descriptors, document);
        }

        [Fact]
        public void UnderstandsMinimizedAttributes_Block11()
        {
            // Arrange
            var document = "<input string-prefix-value />";

            // Wrap in a CSharp block
            document = $"@{{{document}}}";

            // Act & Assert
            EvaluateData(MinimizedAttribute_Descriptors, document);
        }

        [Fact]
        public void UnderstandsMinimizedAttributes_Block12()
        {
            // Arrange
            var document = "<input int-prefix-value='' />";

            // Wrap in a CSharp block
            document = $"@{{{document}}}";

            // Act & Assert
            EvaluateData(MinimizedAttribute_Descriptors, document);
        }

        [Fact]
        public void UnderstandsMinimizedAttributes_Block13()
        {
            // Arrange
            var document = "<input string-prefix-value=''/>";

            // Wrap in a CSharp block
            document = $"@{{{document}}}";

            // Act & Assert
            EvaluateData(MinimizedAttribute_Descriptors, document);
        }

        [Fact]
        public void UnderstandsMinimizedAttributes_Block14()
        {
            // Arrange
            var document = "<input int-prefix-value='3'/>";

            // Wrap in a CSharp block
            document = $"@{{{document}}}";

            // Act & Assert
            EvaluateData(MinimizedAttribute_Descriptors, document);
        }

        [Fact]
        public void UnderstandsMinimizedAttributes_Block15()
        {
            // Arrange
            var document = "<input string-prefix-value='some string' />";

            // Wrap in a CSharp block
            document = $"@{{{document}}}";

            // Act & Assert
            EvaluateData(MinimizedAttribute_Descriptors, document);
        }

        [Fact]
        public void UnderstandsMinimizedAttributes_Block16()
        {
            // Arrange
            var document = "<input unbound-required bound-required-string />";

            // Wrap in a CSharp block
            document = $"@{{{document}}}";

            // Act & Assert
            EvaluateData(MinimizedAttribute_Descriptors, document);
        }

        [Fact]
        public void UnderstandsMinimizedAttributes_Block17()
        {
            // Arrange
            var document = "<p bound-int bound-string></p>";

            // Wrap in a CSharp block
            document = $"@{{{document}}}";

            // Act & Assert
            EvaluateData(MinimizedAttribute_Descriptors, document);
        }

        [Fact]
        public void UnderstandsMinimizedAttributes_Block18()
        {
            // Arrange
            var document = "<input bound-required-int unbound-required bound-required-string />";

            // Wrap in a CSharp block
            document = $"@{{{document}}}";

            // Act & Assert
            EvaluateData(MinimizedAttribute_Descriptors, document);
        }

        [Fact]
        public void UnderstandsMinimizedAttributes_Block19()
        {
            // Arrange
            var document = "<p bound-int bound-string bound-string></p>";

            // Wrap in a CSharp block
            document = $"@{{{document}}}";

            // Act & Assert
            EvaluateData(MinimizedAttribute_Descriptors, document);
        }

        [Fact]
        public void UnderstandsMinimizedAttributes_Block20()
        {
            // Arrange
            var document = "<input unbound-required class='btn' />";

            // Wrap in a CSharp block
            document = $"@{{{document}}}";

            // Act & Assert
            EvaluateData(MinimizedAttribute_Descriptors, document);
        }

        [Fact]
        public void UnderstandsMinimizedAttributes_Block21()
        {
            // Arrange
            var document = "<p bound-string class='btn'></p>";

            // Wrap in a CSharp block
            document = $"@{{{document}}}";

            // Act & Assert
            EvaluateData(MinimizedAttribute_Descriptors, document);
        }

        [Fact]
        public void UnderstandsMinimizedAttributes_Block22()
        {
            // Arrange
            var document = "<input class='btn' unbound-required />";

            // Wrap in a CSharp block
            document = $"@{{{document}}}";

            // Act & Assert
            EvaluateData(MinimizedAttribute_Descriptors, document);
        }

        [Fact]
        public void UnderstandsMinimizedAttributes_Block23()
        {
            // Arrange
            var document = "<p class='btn' bound-string></p>";

            // Wrap in a CSharp block
            document = $"@{{{document}}}";

            // Act & Assert
            EvaluateData(MinimizedAttribute_Descriptors, document);
        }

        [Fact]
        public void UnderstandsMinimizedAttributes_Block24()
        {
            // Arrange
            var document = "<input bound-required-string class='btn' />";

            // Wrap in a CSharp block
            document = $"@{{{document}}}";

            // Act & Assert
            EvaluateData(MinimizedAttribute_Descriptors, document);
        }

        [Fact]
        public void UnderstandsMinimizedAttributes_Block25()
        {
            // Arrange
            var document = "<input class='btn' bound-required-string />";

            // Wrap in a CSharp block
            document = $"@{{{document}}}";

            // Act & Assert
            EvaluateData(MinimizedAttribute_Descriptors, document);
        }

        [Fact]
        public void UnderstandsMinimizedAttributes_Block26()
        {
            // Arrange
            var document = "<input bound-required-int class='btn' />";

            // Wrap in a CSharp block
            document = $"@{{{document}}}";

            // Act & Assert
            EvaluateData(MinimizedAttribute_Descriptors, document);
        }

        [Fact]
        public void UnderstandsMinimizedAttributes_Block27()
        {
            // Arrange
            var document = "<p bound-int class='btn'></p>";

            // Wrap in a CSharp block
            document = $"@{{{document}}}";

            // Act & Assert
            EvaluateData(MinimizedAttribute_Descriptors, document);
        }

        [Fact]
        public void UnderstandsMinimizedAttributes_Block28()
        {
            // Arrange
            var document = "<input class='btn' bound-required-int />";

            // Wrap in a CSharp block
            document = $"@{{{document}}}";

            // Act & Assert
            EvaluateData(MinimizedAttribute_Descriptors, document);
        }

        [Fact]
        public void UnderstandsMinimizedAttributes_Block29()
        {
            // Arrange
            var document = "<p class='btn' bound-int></p>";

            // Wrap in a CSharp block
            document = $"@{{{document}}}";

            // Act & Assert
            EvaluateData(MinimizedAttribute_Descriptors, document);
        }

        [Fact]
        public void UnderstandsMinimizedAttributes_Block30()
        {
            // Arrange
            var expressionString = "@DateTime.Now + 1";
            var document = $"<input class='{expressionString}' bound-required-int />";

            // Wrap in a CSharp block
            document = $"@{{{document}}}";

            // Act & Assert
            EvaluateData(MinimizedAttribute_Descriptors, document);
        }

        [Fact]
        public void UnderstandsMinimizedAttributes_Block31()
        {
            // Arrange
            var expressionString = "@DateTime.Now + 1";
            var document = $"<p class='{expressionString}' bound-int></p>";

            // Wrap in a CSharp block
            document = $"@{{{document}}}";

            // Act & Assert
            EvaluateData(MinimizedAttribute_Descriptors, document);
        }

        [Fact]
        public void UnderstandsMinimizedAttributes_Block32()
        {
            // Arrange
            var expressionString = "@DateTime.Now + 1";
            var document = $"<input    bound-required-int class='{expressionString}'   bound-required-string class='{expressionString}'  unbound-required  />";

            // Wrap in a CSharp block
            document = $"@{{{document}}}";

            // Act & Assert
            EvaluateData(MinimizedAttribute_Descriptors, document);
        }

        [Fact]
        public void UnderstandsMinimizedAttributes_Block33()
        {
            // Arrange
            var expressionString = "@DateTime.Now + 1";
            var document = $"<p    bound-int class='{expressionString}'   bound-string class='{expressionString}'  bound-string></p>";

            // Wrap in a CSharp block
            document = $"@{{{document}}}";

            // Act & Assert
            EvaluateData(MinimizedAttribute_Descriptors, document);
        }

        [Fact]
        public void UnderstandsMinimizedAttributes_PartialTags1()
        {
            EvaluateData(MinimizedAttribute_Descriptors, "<input unbound-required");
        }

        [Fact]
        public void UnderstandsMinimizedAttributes_PartialTags2()
        {
            EvaluateData(MinimizedAttribute_Descriptors, "<input bound-required-string");
        }

        [Fact]
        public void UnderstandsMinimizedAttributes_PartialTags3()
        {
            EvaluateData(MinimizedAttribute_Descriptors, "<input bound-required-int");
        }

        [Fact]
        public void UnderstandsMinimizedAttributes_PartialTags4()
        {
            EvaluateData(MinimizedAttribute_Descriptors, "<input bound-required-int unbound-required bound-required-string");
        }

        [Fact]
        public void UnderstandsMinimizedAttributes_PartialTags5()
        {
            EvaluateData(MinimizedAttribute_Descriptors, "<p bound-string");
        }

        [Fact]
        public void UnderstandsMinimizedAttributes_PartialTags6()
        {
            EvaluateData(MinimizedAttribute_Descriptors, "<p bound-int");
        }

        [Fact]
        public void UnderstandsMinimizedAttributes_PartialTags7()
        {
            EvaluateData(MinimizedAttribute_Descriptors, "<p bound-int bound-string");
        }

        [Fact]
        public void UnderstandsMinimizedAttributes_PartialTags8()
        {
            EvaluateData(MinimizedAttribute_Descriptors, "<input bound-required-int unbound-required bound-required-string<p bound-int bound-string");
        }

        [Fact]
        public void UnderstandsMinimizedBooleanBoundAttributes()
        {
            // Arrange
            var document = "<input boundbool boundbooldict-key />";
            var descriptors = new TagHelperDescriptor[]
            {
                TagHelperDescriptorBuilder.Create("InputTagHelper", "SomeAssembly")
                    .TagMatchingRuleDescriptor(rule =>
                        rule
                        .RequireTagName("input"))
                    .BoundAttributeDescriptor(attribute =>
                        attribute
                        .Name("boundbool")
                        .PropertyName("BoundBoolProp")
                        .TypeName(typeof(bool).FullName))
                    .BoundAttributeDescriptor(attribute =>
                        attribute
                        .Name("boundbooldict")
                        .PropertyName("BoundBoolDictProp")
                        .TypeName("System.Collections.Generic.IDictionary<string, bool>")
                        .AsDictionary("boundbooldict-", typeof(bool).FullName))
                    .Build(),
            };

            // Act & Assert
            EvaluateData(descriptors, document);
        }

        [Fact]
        public void FeatureDisabled_AddsErrorForMinimizedBooleanBoundAttributes()
        {
            // Arrange
            var document = "<input boundbool boundbooldict-key />";
            var descriptors = new TagHelperDescriptor[]
            {
                TagHelperDescriptorBuilder.Create("InputTagHelper", "SomeAssembly")
                    .TagMatchingRuleDescriptor(rule =>
                        rule
                        .RequireTagName("input"))
                    .BoundAttributeDescriptor(attribute =>
                        attribute
                        .Name("boundbool")
                        .PropertyName("BoundBoolProp")
                        .TypeName(typeof(bool).FullName))
                    .BoundAttributeDescriptor(attribute =>
                        attribute
                        .Name("boundbooldict")
                        .PropertyName("BoundBoolDictProp")
                        .TypeName("System.Collections.Generic.IDictionary<string, bool>")
                        .AsDictionary("boundbooldict-", typeof(bool).FullName))
                    .Build(),
            };

            var featureFlags = new TestRazorParserFeatureFlags();

            // Act & Assert
            EvaluateData(descriptors, document, featureFlags: featureFlags);
        }

        private class TestRazorParserFeatureFlags : RazorParserFeatureFlags
        {
            public TestRazorParserFeatureFlags(
                bool allowMinimizedBooleanTagHelperAttributes = false,
                bool allowHtmlCommentsInTagHelper = false,
                bool experimental_AllowConditionalDataDashAttributes = false)
            {
                AllowMinimizedBooleanTagHelperAttributes = allowMinimizedBooleanTagHelperAttributes;
                AllowHtmlCommentsInTagHelpers = allowHtmlCommentsInTagHelper;
                EXPERIMENTAL_AllowConditionalDataDashAttributes = experimental_AllowConditionalDataDashAttributes;
            }

            public override bool AllowMinimizedBooleanTagHelperAttributes { get; }

            public override bool AllowHtmlCommentsInTagHelpers { get; }

            public override bool EXPERIMENTAL_AllowConditionalDataDashAttributes { get; }
        }
    }
}
