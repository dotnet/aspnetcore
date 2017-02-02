// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.AspNetCore.Razor.Evolution;
using Microsoft.AspNetCore.Razor.Evolution.Intermediate;
using Microsoft.AspNetCore.Razor.Evolution.Legacy;
using Xunit;
using ErrorSink = Microsoft.AspNetCore.Razor.Evolution.Legacy.ErrorSink;

namespace Microsoft.AspNetCore.Mvc.Razor.Host
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
                new TagHelperDescriptor()
                {
                    AssemblyName = "TestAssembly",
                    TypeName = "TestTagHelper",
                    TagName = "p",
                    Attributes = new TagHelperAttributeDescriptor[]
                    {
                        new TagHelperAttributeDescriptor()
                        {
                            TypeName = "System.Int32",
                            Name = "Foo",
                        }
                    }
                }
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
            Assert.Equal(2, @class.Children.Count); // No class node created for a VCTH
            for (var i = 0; i < @class.Children.Count; i++)
            {
                Assert.IsNotType<CSharpStatementIRNode>(@class.Children[i]);
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
                new TagHelperDescriptor()
                {
                    AssemblyName = "TestAssembly",
                    TypeName = "TestTagHelper",
                    TagName = "tagcloud",
                    Attributes = new TagHelperAttributeDescriptor[]
                    {
                        new TagHelperAttributeDescriptor()
                        {
                            TypeName = "System.Int32",
                            Name = "Foo",
                            PropertyName = "Foo",
                        }
                    }
                }
            };

            tagHelpers[0].PropertyBag.Add(ViewComponentTagHelperDescriptorConventions.ViewComponentNameKey, "TagCloud");

            var engine = CreateEngine(tagHelpers);
            var pass = new ViewComponentTagHelperPass()
            {
                Engine = engine,
            };

            var irDocument = CreateIRDocument(engine, codeDocument);

            var expectedVCTHName = "AspNetCore.Generated_test.__Generated__TagCloudViewComponentTagHelper";

            // Act
            pass.Execute(codeDocument, irDocument);

            // Assert
            var tagHelper = FindTagHelperNode(irDocument);
            Assert.Equal(expectedVCTHName, Assert.IsType<CreateTagHelperIRNode>(tagHelper.Children[1]).TagHelperTypeName);
            Assert.Equal(expectedVCTHName, Assert.IsType<SetTagHelperPropertyIRNode>(tagHelper.Children[2]).TagHelperTypeName);


            var @class = FindClassNode(irDocument);
            Assert.Equal(3, @class.Children.Count);

            var vcthClass = Assert.IsType<CSharpStatementIRNode>(@class.Children[2]);
            Assert.Equal(@"[Microsoft.AspNetCore.Razor.TagHelpers.HtmlTargetElementAttribute(""tagcloud"")]
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
", vcthClass.Content);
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
                new TagHelperDescriptor()
                {
                    AssemblyName = "TestAssembly",
                    TypeName = "TestTagHelper",
                    TagName = "tagcloud",
                    Attributes = new TagHelperAttributeDescriptor[]
                    {
                        new TagHelperAttributeDescriptor()
                        {
                            TypeName = "System.Collections.Generic.Dictionary<System.String, System.Int32>",
                            Name = "foo",
                            PropertyName = "Tags",
                            IsIndexer = false,
                        },
                        new TagHelperAttributeDescriptor()
                        {
                            TypeName = "System.Collections.Generic.Dictionary<System.String, System.Int32>",
                            Name = "foo-",
                            PropertyName = "Tags",
                            IsIndexer = true,
                        }
                    }
                }
            };

            tagHelpers[0].PropertyBag.Add(ViewComponentTagHelperDescriptorConventions.ViewComponentNameKey, "TagCloud");

            var engine = CreateEngine(tagHelpers);
            var pass = new ViewComponentTagHelperPass()
            {
                Engine = engine,
            };

            var irDocument = CreateIRDocument(engine, codeDocument);

            var expectedVCTHName = "AspNetCore.Generated_test.__Generated__TagCloudViewComponentTagHelper";

            // Act
            pass.Execute(codeDocument, irDocument);

            // Assert
            var tagHelper = FindTagHelperNode(irDocument);
            Assert.Equal(expectedVCTHName, Assert.IsType<CreateTagHelperIRNode>(tagHelper.Children[1]).TagHelperTypeName);
            Assert.IsType<AddPreallocatedTagHelperHtmlAttributeIRNode>(tagHelper.Children[2]);

            var @class = FindClassNode(irDocument);
            Assert.Equal(4, @class.Children.Count);

            var vcthClass = Assert.IsType<CSharpStatementIRNode>(@class.Children[3]);
            Assert.Equal(@"[Microsoft.AspNetCore.Razor.TagHelpers.HtmlTargetElementAttribute(""tagcloud"")]
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
", vcthClass.Content);
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
                new TagHelperDescriptor()
                {
                    AssemblyName = "TestAssembly",
                    TypeName = "PTestTagHelper",
                    TagName = "p",
                    Attributes = new TagHelperAttributeDescriptor[]
                    {
                        new TagHelperAttributeDescriptor()
                        {
                            TypeName = "System.Int32",
                            Name = "Foo",
                        }
                    }
                },
                new TagHelperDescriptor()
                {
                    AssemblyName = "TestAssembly",
                    TypeName = "TestTagHelper",
                    TagName = "tagcloud",
                    Attributes = new TagHelperAttributeDescriptor[]
                    {
                        new TagHelperAttributeDescriptor()
                        {
                            TypeName = "System.Int32",
                            Name = "Foo",
                            PropertyName = "Foo",
                        }
                    }
                }
            };

            tagHelpers[1].PropertyBag.Add(ViewComponentTagHelperDescriptorConventions.ViewComponentNameKey, "TagCloud");

            var engine = CreateEngine(tagHelpers);
            var pass = new ViewComponentTagHelperPass()
            {
                Engine = engine,
            };

            var irDocument = CreateIRDocument(engine, codeDocument);

            var expectedTagHelperName = "PTestTagHelper";
            var expectedVCTHName = "AspNetCore.Generated_test.__Generated__TagCloudViewComponentTagHelper";

            // Act
            pass.Execute(codeDocument, irDocument);

            // Assert
            var outerTagHelper = FindTagHelperNode(irDocument);
            Assert.Equal(expectedTagHelperName, Assert.IsType<CreateTagHelperIRNode>(outerTagHelper.Children[1]).TagHelperTypeName);
            Assert.Equal(expectedTagHelperName, Assert.IsType<SetTagHelperPropertyIRNode>(outerTagHelper.Children[2]).TagHelperTypeName);

            var vcth = FindTagHelperNode(outerTagHelper.Children[0]);
            Assert.Equal(expectedVCTHName, Assert.IsType<CreateTagHelperIRNode>(vcth.Children[1]).TagHelperTypeName);
            Assert.Equal(expectedVCTHName, Assert.IsType<SetTagHelperPropertyIRNode>(vcth.Children[2]).TagHelperTypeName);


            var @class = FindClassNode(irDocument);
            Assert.Equal(3, @class.Children.Count);

            var vcthClass = Assert.IsType<CSharpStatementIRNode>(@class.Children[2]);
            Assert.Equal(@"[Microsoft.AspNetCore.Razor.TagHelpers.HtmlTargetElementAttribute(""tagcloud"")]
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
", vcthClass.Content);
        }

        private RazorCodeDocument CreateDocument(string content)
        {
            using (var stream = new MemoryStream())
            {
                var bytes = Encoding.UTF8.GetBytes(content);
                stream.Write(bytes, 0, bytes.Length);
                stream.Seek(0L, SeekOrigin.Begin);

                var source = RazorSourceDocument.ReadFrom(stream, "test.cshtml");
                return RazorCodeDocument.Create(source);
            }
        }

        private RazorEngine CreateEngine(params TagHelperDescriptor[] tagHelpers)
        {
            return RazorEngine.Create(b =>
            {
                b.Features.Add(new MvcViewDocumentClassifierPass());

                b.Features.Add(new TagHelperFeature(tagHelpers));
            });
        }

        private DocumentIRNode CreateIRDocument(RazorEngine engine, RazorCodeDocument codeDocument)
        {
            for (var i = 0; i < engine.Phases.Count; i++)
            {
                var phase = engine.Phases[i];
                phase.Execute(codeDocument);

                if (phase is IRazorIRPhase)
                {
                    break;
                }
            }

            return codeDocument.GetIRDocument();
        }

        private ClassDeclarationIRNode FindClassNode(RazorIRNode node)
        {
            var visitor = new ClassDeclarationNodeVisitor();
            visitor.Visit(node);
            return visitor.Node;
        }

        private TagHelperIRNode FindTagHelperNode(RazorIRNode node)
        {
            var visitor = new TagHelperNodeVisitor();
            visitor.Visit(node);
            return visitor.Node;
        }

        private string GetCSharpContent(RazorIRNode node)
        {
            var builder = new StringBuilder();
            for (var i = 0; i < node.Children.Count; i++)
            {
                var child = node.Children[i] as CSharpTokenIRNode;
                builder.Append(child.Content);
            }

            return builder.ToString();
        }

        private class ClassDeclarationNodeVisitor : RazorIRNodeWalker
        {
            public ClassDeclarationIRNode Node { get; set; }

            public override void VisitClass(ClassDeclarationIRNode node)
            {
                Node = node;
            }
        }

        private class TagHelperNodeVisitor : RazorIRNodeWalker
        {
            public TagHelperIRNode Node { get; set; }

            public override void VisitTagHelper(TagHelperIRNode node)
            {
                Node = node;
            }
        }

        private class TagHelperFeature : ITagHelperFeature
        {
            public TagHelperFeature(TagHelperDescriptor[] tagHelpers)
            {
                Resolver = new TagHelperDescriptorResolver(tagHelpers);
            }

            public RazorEngine Engine { get; set; }

            public ITagHelperDescriptorResolver Resolver { get; }
        }

        private class TagHelperDescriptorResolver : ITagHelperDescriptorResolver
        {
            public TagHelperDescriptorResolver(TagHelperDescriptor[] tagHelpers)
            {
                TagHelpers = tagHelpers;
            }

            public TagHelperDescriptor[] TagHelpers { get; }

            public IEnumerable<TagHelperDescriptor> Resolve(ErrorSink errorSink)
            {
                return TagHelpers;
            }
        }
    }
}
