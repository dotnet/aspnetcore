// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Extensions;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions
{
    public class ViewComponentTagHelperPassTest
    {
        [Fact]
        public void ViewComponentTagHelperPass_Execute_IgnoresRegularTagHelper()
        {
            // Arrange
            var codeDocument = CreateDocument(@"
@addTagHelper TestTagHelper, TestAssembly
<p foo=""17"">");

            var tagHelpers = new[]
            {
                TagHelperDescriptorBuilder.Create("TestTagHelper", "TestAssembly")
                    .TypeName("TestTagHelper")
                    .BoundAttributeDescriptor(attribute => attribute
                        .Name("Foo")
                        .TypeName("System.Int32"))
                    .TagMatchingRuleDescriptor(rule => rule.RequireTagName("p"))
                    .Build()
            };

            var engine = CreateEngine(tagHelpers);
            var pass = new ViewComponentTagHelperPass()
            {
                Engine = engine,
            };

            var irDocument = CreateIRDocument(engine, codeDocument);

            // Act
            pass.Execute(codeDocument, irDocument);

            // Assert
            var @class = FindClassNode(irDocument);
            Assert.Equal(3, @class.Children.Count); // No class node created for a VCTH
            for (var i = 0; i < @class.Children.Count; i++)
            {
                Assert.IsNotType<CSharpCodeIntermediateNode>(@class.Children[i]);
            }
        }

        [Fact]
        public void ViewComponentTagHelperPass_Execute_CreatesViewComponentTagHelper()
        {
            // Arrange
            var codeDocument = CreateDocument(@"
@addTagHelper TestTagHelper, TestAssembly
<tagcloud foo=""17"">");

            var tagHelpers = new[]
            {
                TagHelperDescriptorBuilder.Create(ViewComponentTagHelperConventions.Kind, "TestTagHelper", "TestAssembly")
                    .TypeName("__Generated__TagCloudViewComponentTagHelper")
                    .BoundAttributeDescriptor(attribute => attribute
                        .Name("Foo")
                        .TypeName("System.Int32")
                        .PropertyName("Foo"))
                    .TagMatchingRuleDescriptor(rule => rule.RequireTagName("tagcloud"))
                    .AddMetadata(ViewComponentTagHelperMetadata.Name, "TagCloud")
                    .Build()
            };

            var engine = CreateEngine(tagHelpers);
            var pass = new ViewComponentTagHelperPass()
            {
                Engine = engine,
            };

            var irDocument = CreateIRDocument(engine, codeDocument);

            var vcthFullName = "AspNetCore.test_cshtml.__Generated__TagCloudViewComponentTagHelper";

            // Act
            pass.Execute(codeDocument, irDocument);

            // Assert
            var tagHelper = FindTagHelperNode(irDocument);
            Assert.Equal(vcthFullName, Assert.IsType<DefaultTagHelperCreateIntermediateNode>(tagHelper.Children[1]).TypeName);
            Assert.Equal("Foo", Assert.IsType<DefaultTagHelperPropertyIntermediateNode>(tagHelper.Children[2]).PropertyName);


            var @class = FindClassNode(irDocument);
            Assert.Equal(4, @class.Children.Count);

            var vcthClass = Assert.IsType<CSharpCodeIntermediateNode>(@class.Children.Last());
            var tokenNode = vcthClass.Children[0] as IntermediateToken;
            Assert.Equal(
                @"[Microsoft.AspNetCore.Razor.TagHelpers.HtmlTargetElementAttribute(""tagcloud"")]
public class __Generated__TagCloudViewComponentTagHelper : Microsoft.AspNetCore.Razor.TagHelpers.TagHelper
{
    private readonly global::Microsoft.AspNetCore.Mvc.IViewComponentHelper _helper = null;
    public __Generated__TagCloudViewComponentTagHelper(global::Microsoft.AspNetCore.Mvc.IViewComponentHelper helper)
    {
        _helper = helper;
    }
    [Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeNotBoundAttribute, global::Microsoft.AspNetCore.Mvc.ViewFeatures.ViewContextAttribute]
    public global::Microsoft.AspNetCore.Mvc.Rendering.ViewContext ViewContext { get; set; }
    public System.Int32 Foo { get; set; }
    public override async global::System.Threading.Tasks.Task ProcessAsync(Microsoft.AspNetCore.Razor.TagHelpers.TagHelperContext context, Microsoft.AspNetCore.Razor.TagHelpers.TagHelperOutput output)
    {
        (_helper as global::Microsoft.AspNetCore.Mvc.ViewFeatures.IViewContextAware)?.Contextualize(ViewContext);
        var content = await _helper.InvokeAsync(""TagCloud"", new { Foo });
        output.TagName = null;
        output.Content.SetHtmlContent(content);
    }
}
",
                tokenNode.Content,
                ignoreLineEndingDifferences: true);
            Assert.Equal(TokenKind.CSharp, tokenNode.Kind);
        }

        [Fact]
        public void ViewComponentTagHelperPass_Execute_CreatesViewComponentTagHelper_WithIndexer()
        {
            // Arrange
            var codeDocument = CreateDocument(@"
@addTagHelper TestTagHelper, TestAssembly
<tagcloud tag-foo=""17"">");

            var tagHelpers = new[]
            {
                TagHelperDescriptorBuilder.Create(ViewComponentTagHelperConventions.Kind, "TestTagHelper", "TestAssembly")
                    .TypeName("__Generated__TagCloudViewComponentTagHelper")
                    .BoundAttributeDescriptor(attribute => attribute
                        .Name("Foo")
                        .TypeName("System.Collections.Generic.Dictionary<System.String, System.Int32>")
                        .PropertyName("Tags")
                        .AsDictionaryAttribute("foo-", "System.Int32"))
                    .TagMatchingRuleDescriptor(rule => rule.RequireTagName("tagcloud"))
                    .AddMetadata(ViewComponentTagHelperMetadata.Name, "TagCloud")
                    .Build()
            };

            var engine = CreateEngine(tagHelpers);
            var pass = new ViewComponentTagHelperPass()
            {
                Engine = engine,
            };

            var irDocument = CreateIRDocument(engine, codeDocument);

            var vcthFullName = "AspNetCore.test_cshtml.__Generated__TagCloudViewComponentTagHelper";

            // Act
            pass.Execute(codeDocument, irDocument);

            // Assert
            var tagHelper = FindTagHelperNode(irDocument);
            Assert.Equal(vcthFullName, Assert.IsType<DefaultTagHelperCreateIntermediateNode>(tagHelper.Children[1]).TypeName);
            Assert.IsType<DefaultTagHelperHtmlAttributeIntermediateNode>(tagHelper.Children[2]);

            var @class = FindClassNode(irDocument);
            Assert.Equal(4, @class.Children.Count);

            var vcthClass = Assert.IsType<CSharpCodeIntermediateNode>(@class.Children[3]);
            var tokenNode = vcthClass.Children[0] as IntermediateToken;
            Assert.Equal(
                @"[Microsoft.AspNetCore.Razor.TagHelpers.HtmlTargetElementAttribute(""tagcloud"")]
public class __Generated__TagCloudViewComponentTagHelper : Microsoft.AspNetCore.Razor.TagHelpers.TagHelper
{
    private readonly global::Microsoft.AspNetCore.Mvc.IViewComponentHelper _helper = null;
    public __Generated__TagCloudViewComponentTagHelper(global::Microsoft.AspNetCore.Mvc.IViewComponentHelper helper)
    {
        _helper = helper;
    }
    [Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeNotBoundAttribute, global::Microsoft.AspNetCore.Mvc.ViewFeatures.ViewContextAttribute]
    public global::Microsoft.AspNetCore.Mvc.Rendering.ViewContext ViewContext { get; set; }
    public System.Collections.Generic.Dictionary<System.String, System.Int32> Tags { get; set; }
     = new System.Collections.Generic.Dictionary<System.String, System.Int32>();
    public override async global::System.Threading.Tasks.Task ProcessAsync(Microsoft.AspNetCore.Razor.TagHelpers.TagHelperContext context, Microsoft.AspNetCore.Razor.TagHelpers.TagHelperOutput output)
    {
        (_helper as global::Microsoft.AspNetCore.Mvc.ViewFeatures.IViewContextAware)?.Contextualize(ViewContext);
        var content = await _helper.InvokeAsync(""TagCloud"", new { Tags });
        output.TagName = null;
        output.Content.SetHtmlContent(content);
    }
}
",
                tokenNode.Content,
                ignoreLineEndingDifferences: true);
            Assert.Equal(TokenKind.CSharp, tokenNode.Kind);
        }

        [Fact]
        public void ViewComponentTagHelperPass_Execute_CreatesViewComponentTagHelper_Nested()
        {
            // Arrange
            var codeDocument = CreateDocument(@"
@addTagHelper *, TestAssembly
<p foo=""17""><tagcloud foo=""17""></p>");

            var tagHelpers = new[]
            {
                TagHelperDescriptorBuilder.Create("PTestTagHelper", "TestAssembly")
                    .TypeName("PTestTagHelper")
                    .BoundAttributeDescriptor(attribute => attribute
                        .PropertyName("Foo")
                        .Name("Foo")
                        .TypeName("System.Int32"))
                    .TagMatchingRuleDescriptor(rule => rule.RequireTagName("p"))
                    .Build(),
                TagHelperDescriptorBuilder.Create(ViewComponentTagHelperConventions.Kind, "TestTagHelper", "TestAssembly")
                    .TypeName("__Generated__TagCloudViewComponentTagHelper")
                    .BoundAttributeDescriptor(attribute => attribute
                        .PropertyName("Foo")
                        .Name("Foo")
                        .TypeName("System.Int32"))
                    .TagMatchingRuleDescriptor(rule => rule.RequireTagName("tagcloud"))
                    .AddMetadata(ViewComponentTagHelperMetadata.Name, "TagCloud")
                    .Build()
            };

            var engine = CreateEngine(tagHelpers);
            var pass = new ViewComponentTagHelperPass()
            {
                Engine = engine,
            };

            var irDocument = CreateIRDocument(engine, codeDocument);
            
            var vcthFullName = "AspNetCore.test_cshtml.__Generated__TagCloudViewComponentTagHelper";

            // Act
            pass.Execute(codeDocument, irDocument);

            // Assert
            var outerTagHelper = FindTagHelperNode(irDocument);
            Assert.Equal("PTestTagHelper", Assert.IsType<DefaultTagHelperCreateIntermediateNode>(outerTagHelper.Children[1]).TypeName);
            Assert.Equal("Foo", Assert.IsType<DefaultTagHelperPropertyIntermediateNode>(outerTagHelper.Children[2]).PropertyName);

            var vcth = FindTagHelperNode(outerTagHelper.Children[0]);
            Assert.Equal(vcthFullName, Assert.IsType<DefaultTagHelperCreateIntermediateNode>(vcth.Children[1]).TypeName);
            Assert.Equal("Foo", Assert.IsType<DefaultTagHelperPropertyIntermediateNode>(vcth.Children[2]).PropertyName);


            var @class = FindClassNode(irDocument);
            Assert.Equal(5, @class.Children.Count);

            var vcthClass = Assert.IsType<CSharpCodeIntermediateNode>(@class.Children.Last());
            var tokenNode = vcthClass.Children[0] as IntermediateToken;
            Assert.Equal(
                @"[Microsoft.AspNetCore.Razor.TagHelpers.HtmlTargetElementAttribute(""tagcloud"")]
public class __Generated__TagCloudViewComponentTagHelper : Microsoft.AspNetCore.Razor.TagHelpers.TagHelper
{
    private readonly global::Microsoft.AspNetCore.Mvc.IViewComponentHelper _helper = null;
    public __Generated__TagCloudViewComponentTagHelper(global::Microsoft.AspNetCore.Mvc.IViewComponentHelper helper)
    {
        _helper = helper;
    }
    [Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeNotBoundAttribute, global::Microsoft.AspNetCore.Mvc.ViewFeatures.ViewContextAttribute]
    public global::Microsoft.AspNetCore.Mvc.Rendering.ViewContext ViewContext { get; set; }
    public System.Int32 Foo { get; set; }
    public override async global::System.Threading.Tasks.Task ProcessAsync(Microsoft.AspNetCore.Razor.TagHelpers.TagHelperContext context, Microsoft.AspNetCore.Razor.TagHelpers.TagHelperOutput output)
    {
        (_helper as global::Microsoft.AspNetCore.Mvc.ViewFeatures.IViewContextAware)?.Contextualize(ViewContext);
        var content = await _helper.InvokeAsync(""TagCloud"", new { Foo });
        output.TagName = null;
        output.Content.SetHtmlContent(content);
    }
}
",
                tokenNode.Content,
                ignoreLineEndingDifferences: true);
            Assert.Equal(TokenKind.CSharp, tokenNode.Kind);
        }

        private RazorCodeDocument CreateDocument(string content)
        {
            var source = RazorSourceDocument.Create(content, "test.cshtml");
            return RazorCodeDocument.Create(source);
        }

        private RazorEngine CreateEngine(params TagHelperDescriptor[] tagHelpers)
        {
            return RazorEngine.Create(b =>
            {
                b.Features.Add(new MvcViewDocumentClassifierPass());

                b.Features.Add(new TestTagHelperFeature(tagHelpers));
            });
        }

        private DocumentIntermediateNode CreateIRDocument(RazorEngine engine, RazorCodeDocument codeDocument)
        {
            for (var i = 0; i < engine.Phases.Count; i++)
            {
                var phase = engine.Phases[i];
                phase.Execute(codeDocument);

                if (phase is IRazorDirectiveClassifierPhase)
                {
                    break;
                }
            }

            // We also expect the default tag helper pass to run first.
            var documentNode = codeDocument.GetDocumentIntermediateNode();

            var defaultTagHelperPass = engine.Features.OfType<DefaultTagHelperOptimizationPass>().Single();
            defaultTagHelperPass.Execute(codeDocument, documentNode);

            return codeDocument.GetDocumentIntermediateNode();
        }

        private ClassDeclarationIntermediateNode FindClassNode(IntermediateNode node)
        {
            var visitor = new ClassDeclarationNodeVisitor();
            visitor.Visit(node);
            return visitor.Node;
        }

        private TagHelperIntermediateNode FindTagHelperNode(IntermediateNode node)
        {
            var visitor = new TagHelperNodeVisitor();
            visitor.Visit(node);
            return visitor.Node;
        }

        private string GetCSharpContent(IntermediateNode node)
        {
            var builder = new StringBuilder();
            for (var i = 0; i < node.Children.Count; i++)
            {
                var child = node.Children[i] as IntermediateToken;
                if (child.Kind == TokenKind.CSharp)
                {
                    builder.Append(child.Content);
                }
            }

            return builder.ToString();
        }

        private class ClassDeclarationNodeVisitor : IntermediateNodeWalker
        {
            public ClassDeclarationIntermediateNode Node { get; set; }

            public override void VisitClassDeclaration(ClassDeclarationIntermediateNode node)
            {
                Node = node;
            }
        }

        private class TagHelperNodeVisitor : IntermediateNodeWalker
        {
            public TagHelperIntermediateNode Node { get; set; }

            public override void VisitTagHelper(TagHelperIntermediateNode node)
            {
                Node = node;
            }
        }
    }
}
