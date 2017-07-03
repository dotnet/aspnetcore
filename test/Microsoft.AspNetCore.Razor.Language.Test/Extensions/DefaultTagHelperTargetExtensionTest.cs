// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Extensions
{
    public class DefaultTagHelperTargetExtensionTest
    {
        private static readonly TagHelperDescriptor StringPropertyTagHelper = CreateTagHelperDescriptor(
            tagName: "input",
            typeName: "InputTagHelper",
            assemblyName: "TestAssembly",
            attributes: new Action<BoundAttributeDescriptorBuilder>[]
            {
                builder => builder
                    .Name("bound")
                    .PropertyName("StringProp")
                    .TypeName("System.String"),
            });

        private static readonly TagHelperDescriptor IntPropertyTagHelper = CreateTagHelperDescriptor(
            tagName: "input",
            typeName: "InputTagHelper",
            assemblyName: "TestAssembly",
            attributes: new Action<BoundAttributeDescriptorBuilder>[]
            {
                builder => builder
                    .Name("bound")
                    .PropertyName("IntProp")
                    .TypeName("System.Int32"),
            });

        private static readonly TagHelperDescriptor StringIndexerTagHelper = CreateTagHelperDescriptor(
            tagName: "input",
            typeName: "InputTagHelper",
            assemblyName: "TestAssembly",
            attributes: new Action<BoundAttributeDescriptorBuilder>[]
            {
                builder => builder
                    .Name("bound")
                    .PropertyName("StringIndexer")
                    .TypeName("System.Collections.Generic.Dictionary<System.String, System.String>")
                    .AsDictionary("foo-", "System.String"),
            });

        private static readonly TagHelperDescriptor IntIndexerTagHelper = CreateTagHelperDescriptor(
            tagName: "input",
            typeName: "InputTagHelper",
            assemblyName: "TestAssembly",
            attributes: new Action<BoundAttributeDescriptorBuilder>[]
            {
                builder => builder
                    .Name("bound")
                    .PropertyName("IntIndexer")
                    .TypeName("System.Collections.Generic.Dictionary<System.String, System.Int32>")
                    .AsDictionary("foo-", "System.Int32"),
            });

        private static readonly SourceSpan Span = new SourceSpan("test.cshtml", 15, 2, 5, 2);

        [Fact]
        public void WriteTagHelperBody_DesignTime_WritesChildren()
        {
            // Arrange
            var extension = new DefaultTagHelperTargetExtension() { DesignTime = true };
            var context = GetDesignTimeCodeRenderingContext();

            var node = new DefaultTagHelperBodyIntermediateNode();

            // Act
            extension.WriteTagHelperBody(context, node);

            // Assert
            var csharp = context.CodeWriter.Builder.ToString();
            Assert.Equal(
@"Render Children
",
                csharp,
                ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void WriteTagHelperBody_Runtime_RendersCorrectly_UsesTagNameAndModeFromContext()
        {
            // Arrange
            var extension = new DefaultTagHelperTargetExtension();
            var context = GetRuntimeCodeRenderingContext();

            var node = new DefaultTagHelperBodyIntermediateNode()
            {
                TagMode = TagMode.SelfClosing,
                TagName = "p",
            };

            // Act
            extension.WriteTagHelperBody(context, node);

            // Assert
            var csharp = context.CodeWriter.Builder.ToString();
            Assert.Equal(
@"__tagHelperExecutionContext = __tagHelperScopeManager.Begin(""p"", global::Microsoft.AspNetCore.Razor.TagHelpers.TagMode.SelfClosing, ""test"", async() => {
    Render Children
}
);
",
                csharp,
                ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void WriteTagHelperCreate_DesignTime_RendersCorrectly_UsesSpecifiedTagHelperType()
        {
            // Arrange
            var extension = new DefaultTagHelperTargetExtension() { DesignTime = true };
            var context = GetDesignTimeCodeRenderingContext();

            var node = new DefaultTagHelperCreateIntermediateNode()
            {
                Field = "__TestNamespace_MyTagHelper",
                Type = "TestNamespace.MyTagHelper",
            };

            // Act
            extension.WriteTagHelperCreate(context, node);

            // Assert
            var csharp = context.CodeWriter.Builder.ToString();
            Assert.Equal(
@"__TestNamespace_MyTagHelper = CreateTagHelper<global::TestNamespace.MyTagHelper>();
",
                csharp,
                ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void WriteTagHelperCreate_Runtime_RendersCorrectly_UsesSpecifiedTagHelperType()
        {
            // Arrange
            var extension = new DefaultTagHelperTargetExtension();
            var context = GetRuntimeCodeRenderingContext();

            var node = new DefaultTagHelperCreateIntermediateNode()
            {
                Field = "__TestNamespace_MyTagHelper",
                Type = "TestNamespace.MyTagHelper",
            };

            // Act
            extension.WriteTagHelperCreate(context, node);

            // Assert
            var csharp = context.CodeWriter.Builder.ToString();
            Assert.Equal(
@"__TestNamespace_MyTagHelper = CreateTagHelper<global::TestNamespace.MyTagHelper>();
__tagHelperExecutionContext.Add(__TestNamespace_MyTagHelper);
",
                csharp,
                ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void WriteTagHelperExecute_DesignTime_WritesNothing()
        {
            // Arrange
            var extension = new DefaultTagHelperTargetExtension() { DesignTime = true };
            var context = GetDesignTimeCodeRenderingContext();

            var node = new DefaultTagHelperExecuteIntermediateNode();

            // Act
            extension.WriteTagHelperExecute(context, node);

            // Assert
            var csharp = context.CodeWriter.Builder.ToString();
            Assert.Equal(
                @"",
                csharp,
                ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void WriteTagHelperExecute_Runtime_RendersCorrectly()
        {
            // Arrange
            var extension = new DefaultTagHelperTargetExtension();
            var context = GetRuntimeCodeRenderingContext();

            var node = new DefaultTagHelperExecuteIntermediateNode();

            // Act
            extension.WriteTagHelperExecute(context, node);

            // Assert
            var csharp = context.CodeWriter.Builder.ToString();
            Assert.Equal(
@"await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
if (!__tagHelperExecutionContext.Output.IsContentModified)
{
    await __tagHelperExecutionContext.SetOutputContentAsync();
}
Write(__tagHelperExecutionContext.Output);
__tagHelperExecutionContext = __tagHelperScopeManager.End();
",
                csharp,
                ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void WriteTagHelperHtmlAttribute_DesignTime_WritesNothing()
        {
            // Arrange
            var extension = new DefaultTagHelperTargetExtension() { DesignTime = true };
            var context = GetDesignTimeCodeRenderingContext();

            var node = new DefaultTagHelperHtmlAttributeIntermediateNode()
            {
                AttributeName = "name",
                AttributeStructure = AttributeStructure.DoubleQuotes,
                Children =
                {
                    new HtmlAttributeValueIntermediateNode()
                    {
                        Children = { new IntermediateToken { Kind = IntermediateToken.TokenKind.Html, Content = "Blah-" } }
                    },
                    new CSharpCodeAttributeValueIntermediateNode()
                    {
                        Children = { new IntermediateToken { Kind = IntermediateToken.TokenKind.CSharp, Content = "\"Foo\"", } },
                    }
                }
            };

            // Act
            extension.WriteTagHelperHtmlAttribute(context, node);

            // Assert
            var csharp = context.CodeWriter.Builder.ToString();
            Assert.Equal(
@"Render Children
",
                csharp,
                ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void WriteTagHelperHtmlAttribute_Runtime_SimpleAttribute_RendersCorrectly()
        {
            // Arrange
            var extension = new DefaultTagHelperTargetExtension();
            var context = GetRuntimeCodeRenderingContext();

            var node = new DefaultTagHelperHtmlAttributeIntermediateNode()
            {
                AttributeName = "name",
                AttributeStructure = AttributeStructure.DoubleQuotes,
                Children =
                {
                    new HtmlAttributeIntermediateNode()
                    {
                        Children = { new IntermediateToken { Kind = IntermediateToken.TokenKind.Html, Content = "\"value\"", } },
                    }
                }
            };

            // Act
            extension.WriteTagHelperHtmlAttribute(context, node);

            // Assert
            var csharp = context.CodeWriter.Builder.ToString();
            Assert.Equal(
@"BeginWriteTagHelperAttribute();
Render Children
__tagHelperStringValueBuffer = EndWriteTagHelperAttribute();
__tagHelperExecutionContext.AddHtmlAttribute(""name"", Html.Raw(__tagHelperStringValueBuffer), global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
",
                csharp,
                ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void WriteTagHelperHtmlAttribute_Runtime_DynamicAttribute_RendersCorrectly()
        {
            // Arrange
            var extension = new DefaultTagHelperTargetExtension();
            var context = GetRuntimeCodeRenderingContext();

            var node = new DefaultTagHelperHtmlAttributeIntermediateNode()
            {
                AttributeName = "name",
                AttributeStructure = AttributeStructure.DoubleQuotes,
                Children =
                {
                    new HtmlAttributeValueIntermediateNode()
                    {
                        Children = { new IntermediateToken { Kind = IntermediateToken.TokenKind.Html, Content = "Blah-" } }
                    },
                    new CSharpCodeAttributeValueIntermediateNode()
                    {
                        Children = { new IntermediateToken { Kind = IntermediateToken.TokenKind.CSharp, Content = "\"Foo\"", } },
                    }
                }
            };

            // Act
            extension.WriteTagHelperHtmlAttribute(context, node);

            // Assert
            var csharp = context.CodeWriter.Builder.ToString();
            Assert.Equal(
@"BeginAddHtmlAttributeValues(__tagHelperExecutionContext, ""name"", 2, global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
Render Children
EndAddHtmlAttributeValues(__tagHelperExecutionContext);
",
                csharp,
                ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void WriteTagHelperProperty_DesignTime_StringProperty_HtmlContent_RendersCorrectly()
        {
            // Arrange
            var extension = new DefaultTagHelperTargetExtension() { DesignTime = true };
            var context = GetDesignTimeCodeRenderingContext();

            var node = new DefaultTagHelperPropertyIntermediateNode()
            {
                AttributeName = "bound",
                AttributeStructure = AttributeStructure.DoubleQuotes,
                BoundAttribute = StringPropertyTagHelper.BoundAttributes.Single(),
                Field = "__InputTagHelper",
                IsIndexerNameMatch = false,
                Property = "StringProp",
                TagHelper = StringPropertyTagHelper,
                Children =
                {
                    new HtmlContentIntermediateNode()
                    {
                        Children = { new IntermediateToken { Kind = IntermediateToken.TokenKind.Html, Content = "value", } },
                    }
                }
            };

            // Act
            extension.WriteTagHelperProperty(context, node);

            // Assert
            var csharp = context.CodeWriter.Builder.ToString();
            Assert.Equal(
@"Render Children
__InputTagHelper.StringProp = ""value"";
",
                csharp,
                ignoreLineEndingDifferences: true);
        }

        [Fact] // We don't actually assign the expression result at design time, we just use string.Empty as a placeholder.
        public void WriteTagHelperProperty_DesignTime_StringProperty_NonHtmlContent_RendersCorrectly()
        {
            // Arrange
            var extension = new DefaultTagHelperTargetExtension() { DesignTime = true };
            var context = GetDesignTimeCodeRenderingContext();

            var node = new DefaultTagHelperPropertyIntermediateNode()
            {
                AttributeName = "bound",
                AttributeStructure = AttributeStructure.DoubleQuotes,
                BoundAttribute = StringPropertyTagHelper.BoundAttributes.Single(),
                Field = "__InputTagHelper",
                IsIndexerNameMatch = false,
                Property = "StringProp",
                TagHelper = StringPropertyTagHelper,
                Children =
                {
                    new CSharpExpressionIntermediateNode()
                    {
                        Children = { new IntermediateToken { Kind = IntermediateToken.TokenKind.CSharp, Content = "\"3+5\"", } },
                    }
                }
            };

            // Act
            extension.WriteTagHelperProperty(context, node);

            // Assert
            var csharp = context.CodeWriter.Builder.ToString();
            Assert.Equal(
@"Render Children
__InputTagHelper.StringProp = string.Empty;
",
                csharp,
                ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void WriteTagHelperProperty_DesignTime_NonStringProperty_RendersCorrectly()
        {
            // Arrange
            var extension = new DefaultTagHelperTargetExtension() { DesignTime = true };
            var context = GetDesignTimeCodeRenderingContext();

            var node = new DefaultTagHelperPropertyIntermediateNode()
            {
                AttributeName = "bound",
                AttributeStructure = AttributeStructure.DoubleQuotes,
                BoundAttribute = IntPropertyTagHelper.BoundAttributes.Single(),
                Field = "__InputTagHelper",
                IsIndexerNameMatch = false,
                Property = "IntProp",
                TagHelper = IntPropertyTagHelper,
                Source = Span,
                Children =
                {
                    new CSharpExpressionIntermediateNode()
                    {
                        Children = { new IntermediateToken { Kind = IntermediateToken.TokenKind.CSharp, Content = "32", } },
                    }
                }
            };

            // Act
            extension.WriteTagHelperProperty(context, node);

            // Assert
            var csharp = context.CodeWriter.Builder.ToString();
            Assert.Equal(
@"#line 3 ""test.cshtml""
__InputTagHelper.IntProp = 32;

#line default
#line hidden
",
                csharp,
                ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void WriteTagHelperProperty_DesignTime_NonStringProperty_RendersCorrectly_WithoutLocation()
        {
            // Arrange
            var extension = new DefaultTagHelperTargetExtension() { DesignTime = true };
            var context = GetDesignTimeCodeRenderingContext();

            var node = new DefaultTagHelperPropertyIntermediateNode()
            {
                AttributeName = "bound",
                AttributeStructure = AttributeStructure.DoubleQuotes,
                BoundAttribute = IntPropertyTagHelper.BoundAttributes.Single(),
                Field = "__InputTagHelper",
                IsIndexerNameMatch = false,
                Property = "IntProp",
                TagHelper = IntPropertyTagHelper,
                Children =
                {
                    new CSharpExpressionIntermediateNode()
                    {
                        Children = { new IntermediateToken { Kind = IntermediateToken.TokenKind.CSharp, Content = "32", } },
                    }
                }
            };

            // Act
            extension.WriteTagHelperProperty(context, node);

            // Assert
            var csharp = context.CodeWriter.Builder.ToString();
            Assert.Equal(
@"__InputTagHelper.IntProp = 32;
",
                csharp,
                ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void WriteTagHelperProperty_DesignTime_NonStringIndexer_RendersCorrectly()
        {
            // Arrange
            var extension = new DefaultTagHelperTargetExtension() { DesignTime = true };
            var context = GetDesignTimeCodeRenderingContext();

            var node = new DefaultTagHelperPropertyIntermediateNode()
            {
                AttributeName = "foo-bound",
                AttributeStructure = AttributeStructure.DoubleQuotes,
                BoundAttribute = IntIndexerTagHelper.BoundAttributes.Single(),
                Field = "__InputTagHelper",
                IsIndexerNameMatch = true,
                Property = "IntIndexer",
                TagHelper = IntIndexerTagHelper,
                Source = Span,
                Children =
                {
                    new CSharpExpressionIntermediateNode()
                    {
                        Children = { new IntermediateToken { Kind = IntermediateToken.TokenKind.CSharp, Content = "32", } },
                    }
                }
            };

            // Act
            extension.WriteTagHelperProperty(context, node);

            // Assert
            var csharp = context.CodeWriter.Builder.ToString();
            Assert.Equal(
@"#line 3 ""test.cshtml""
__InputTagHelper.IntIndexer[""bound""] = 32;

#line default
#line hidden
",
                csharp,
                ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void WriteTagHelperProperty_DesignTime_NonStringIndexer_RendersCorrectly_WithoutLocation()
        {
            // Arrange
            var extension = new DefaultTagHelperTargetExtension() { DesignTime = true };
            var context = GetDesignTimeCodeRenderingContext();

            var node = new DefaultTagHelperPropertyIntermediateNode()
            {
                AttributeName = "foo-bound",
                AttributeStructure = AttributeStructure.DoubleQuotes,
                BoundAttribute = IntIndexerTagHelper.BoundAttributes.Single(),
                Field = "__InputTagHelper",
                IsIndexerNameMatch = true,
                Property = "IntIndexer",
                TagHelper = IntIndexerTagHelper,
                Children =
                {
                    new CSharpExpressionIntermediateNode()
                    {
                        Children = { new IntermediateToken { Kind = IntermediateToken.TokenKind.CSharp, Content = "32", } },
                    }
                }
            };

            // Act
            extension.WriteTagHelperProperty(context, node);

            // Assert
            var csharp = context.CodeWriter.Builder.ToString();
            Assert.Equal(
@"__InputTagHelper.IntIndexer[""bound""] = 32;
",
                csharp,
                ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void WriteTagHelperProperty_Runtime_StringProperty_HtmlContent_RendersCorrectly()
        {
            // Arrange
            var extension = new DefaultTagHelperTargetExtension();
            var context = GetRuntimeCodeRenderingContext();

            var node = new DefaultTagHelperPropertyIntermediateNode()
            {
                AttributeName = "bound",
                AttributeStructure = AttributeStructure.DoubleQuotes,
                BoundAttribute = StringPropertyTagHelper.BoundAttributes.Single(),
                Field = "__InputTagHelper",
                IsIndexerNameMatch = false,
                Property = "StringProp",
                TagHelper = StringPropertyTagHelper,
                Children =
                {
                    new HtmlContentIntermediateNode()
                    {
                        Children = { new IntermediateToken { Kind = IntermediateToken.TokenKind.Html, Content = "\"value\"", } },
                    }
                }
            };

            // Act
            extension.WriteTagHelperProperty(context, node);

            // Assert
            var csharp = context.CodeWriter.Builder.ToString();

            // The attribute value is not rendered inline because we are not using the preallocated writer.
            Assert.Equal(
@"BeginWriteTagHelperAttribute();
Render Children
__tagHelperStringValueBuffer = EndWriteTagHelperAttribute();
__InputTagHelper.StringProp = __tagHelperStringValueBuffer;
__tagHelperExecutionContext.AddTagHelperAttribute(""bound"", __InputTagHelper.StringProp, global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
",
                csharp,
                ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void WriteTagHelperProperty_Runtime_NonStringProperty_RendersCorrectly()
        {
            // Arrange
            var extension = new DefaultTagHelperTargetExtension();
            var context = GetRuntimeCodeRenderingContext();

            var node = new DefaultTagHelperPropertyIntermediateNode()
            {
                AttributeName = "bound",
                AttributeStructure = AttributeStructure.DoubleQuotes,
                BoundAttribute = IntPropertyTagHelper.BoundAttributes.Single(),
                Field = "__InputTagHelper",
                IsIndexerNameMatch = false,
                Property = "IntProp",
                TagHelper = IntPropertyTagHelper,
                Source = Span,
                Children =
                {
                    new CSharpExpressionIntermediateNode()
                    {
                        Children = { new IntermediateToken { Kind = IntermediateToken.TokenKind.CSharp, Content = "32", } },
                    }
                },
            };

            // Act
            extension.WriteTagHelperProperty(context, node);

            // Assert
            var csharp = context.CodeWriter.Builder.ToString();
            Assert.Equal(
@"#line 3 ""test.cshtml""
__InputTagHelper.IntProp = 32;

#line default
#line hidden
__tagHelperExecutionContext.AddTagHelperAttribute(""bound"", __InputTagHelper.IntProp, global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
",
                csharp,
                ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void WriteTagHelperProperty_Runtime_NonStringProperty_RendersCorrectly_WithoutLocation()
        {
            // Arrange
            var extension = new DefaultTagHelperTargetExtension();
            var context = GetRuntimeCodeRenderingContext();

            var node = new DefaultTagHelperPropertyIntermediateNode()
            {
                AttributeName = "bound",
                AttributeStructure = AttributeStructure.DoubleQuotes,
                BoundAttribute = IntPropertyTagHelper.BoundAttributes.Single(),
                Field = "__InputTagHelper",
                IsIndexerNameMatch = false,
                Property = "IntProp",
                TagHelper = IntPropertyTagHelper,
                Children =
                {
                    new CSharpExpressionIntermediateNode()
                    {
                        Children = { new IntermediateToken { Kind = IntermediateToken.TokenKind.CSharp, Content = "32", } },
                    }
                }
            };

            // Act
            extension.WriteTagHelperProperty(context, node);

            // Assert
            var csharp = context.CodeWriter.Builder.ToString();
            Assert.Equal(
@"__InputTagHelper.IntProp = 32;
__tagHelperExecutionContext.AddTagHelperAttribute(""bound"", __InputTagHelper.IntProp, global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
",
                csharp,
                ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void WriteTagHelperProperty_Runtime_NonStringIndexer_RendersCorrectly()
        {
            // Arrange
            var extension = new DefaultTagHelperTargetExtension();
            var context = GetRuntimeCodeRenderingContext();

            var node = new DefaultTagHelperPropertyIntermediateNode()
            {
                AttributeName = "foo-bound",
                AttributeStructure = AttributeStructure.DoubleQuotes,
                BoundAttribute = IntIndexerTagHelper.BoundAttributes.Single(),
                Field = "__InputTagHelper",
                IsIndexerNameMatch = true,
                Property = "IntIndexer",
                TagHelper = IntIndexerTagHelper,
                Source = Span,
                Children =
                {
                    new CSharpExpressionIntermediateNode()
                    {
                        Children = { new IntermediateToken { Kind = IntermediateToken.TokenKind.CSharp, Content = "32", } },
                    }
                }
            };

            // Act
            extension.WriteTagHelperProperty(context, node);

            // Assert
            var csharp = context.CodeWriter.Builder.ToString();
            Assert.Equal(
@"if (__InputTagHelper.IntIndexer == null)
{
    throw new InvalidOperationException(InvalidTagHelperIndexerAssignment(""foo-bound"", ""InputTagHelper"", ""IntIndexer""));
}
#line 3 ""test.cshtml""
__InputTagHelper.IntIndexer[""bound""] = 32;

#line default
#line hidden
__tagHelperExecutionContext.AddTagHelperAttribute(""foo-bound"", __InputTagHelper.IntIndexer[""bound""], global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
",
                csharp,
                ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void WriteTagHelperProperty_Runtime_NonStringIndexer_RendersCorrectly_WithoutLocation()
        {
            // Arrange
            var extension = new DefaultTagHelperTargetExtension();
            var context = GetRuntimeCodeRenderingContext();

            var node = new DefaultTagHelperPropertyIntermediateNode()
            {
                AttributeName = "foo-bound",
                AttributeStructure = AttributeStructure.DoubleQuotes,
                BoundAttribute = IntIndexerTagHelper.BoundAttributes.Single(),
                Field = "__InputTagHelper",
                IsIndexerNameMatch = true,
                Property = "IntIndexer",
                TagHelper = IntIndexerTagHelper,
                Children =
                {
                    new CSharpExpressionIntermediateNode()
                    {
                        Children = { new IntermediateToken { Kind = IntermediateToken.TokenKind.CSharp, Content = "32", } },
                    }
                }
            };

            // Act
            extension.WriteTagHelperProperty(context, node);

            // Assert
            var csharp = context.CodeWriter.Builder.ToString();
            Assert.Equal(
@"if (__InputTagHelper.IntIndexer == null)
{
    throw new InvalidOperationException(InvalidTagHelperIndexerAssignment(""foo-bound"", ""InputTagHelper"", ""IntIndexer""));
}
__InputTagHelper.IntIndexer[""bound""] = 32;
__tagHelperExecutionContext.AddTagHelperAttribute(""foo-bound"", __InputTagHelper.IntIndexer[""bound""], global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
",
                csharp,
                ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void WriteTagHelperRuntime_DesignTime_WritesNothing()
        {
            // Arrange
            var extension = new DefaultTagHelperTargetExtension() { DesignTime = true };
            var context = GetDesignTimeCodeRenderingContext();

            var node = new DefaultTagHelperRuntimeIntermediateNode();

            // Act
            extension.WriteTagHelperRuntime(context, node);

            // Assert
            var csharp = context.CodeWriter.Builder.ToString();
            Assert.Equal(
                @"",
                csharp,
                ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void WriteTagHelperRuntime_Runtime_DeclaresRequiredFields()
        {
            // Arrange
            var extension = new DefaultTagHelperTargetExtension();
            var context = GetRuntimeCodeRenderingContext();

            var node = new DefaultTagHelperRuntimeIntermediateNode();

            // Act
            extension.WriteTagHelperRuntime(context, node);

            // Assert
            var csharp = context.CodeWriter.Builder.ToString();
            Assert.Equal(
@"#line hidden
#pragma warning disable 0169
private string __tagHelperStringValueBuffer;
#pragma warning restore 0169
private global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperExecutionContext __tagHelperExecutionContext;
private global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperRunner __tagHelperRunner = new global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperRunner();
private global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperScopeManager __backed__tagHelperScopeManager = null;
private global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperScopeManager __tagHelperScopeManager
{
    get
    {
        if (__backed__tagHelperScopeManager == null)
        {
            __backed__tagHelperScopeManager = new global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperScopeManager(StartTagHelperWritingScope, EndTagHelperWritingScope);
        }
        return __backed__tagHelperScopeManager;
    }
}
",
                csharp,
                ignoreLineEndingDifferences: true);
        }

        private static CodeRenderingContext GetRuntimeCodeRenderingContext()
        {
            var codeWriter = new CodeWriter();
            var nodeWriter = new RuntimeNodeWriter();
            var options = RazorCodeGenerationOptions.CreateDefault();
            var context = new DefaultCodeRenderingContext(codeWriter, nodeWriter, null, options)
            {
                Items =
                {
                    { CodeRenderingContext.SuppressUniqueIds, "test" },
                },
                TagHelperWriter = new RuntimeTagHelperWriter(),
                TagHelperRenderingContext = new TagHelperRenderingContext()
            };
            context.SetRenderChildren(n =>
            {
                codeWriter.WriteLine("Render Children");
            });

            return context;
        }

        private static CodeRenderingContext GetDesignTimeCodeRenderingContext()
        {
            var codeWriter = new CodeWriter();
            var nodeWriter = new RuntimeNodeWriter();
            var options = RazorCodeGenerationOptions.CreateDesignTimeDefault();
            var context = new DefaultCodeRenderingContext(codeWriter, nodeWriter, null, options)
            {
                Items =
                {
                    { CodeRenderingContext.SuppressUniqueIds, "test" },
                },
                TagHelperWriter = new DesignTimeTagHelperWriter(),
                TagHelperRenderingContext = new TagHelperRenderingContext()
            };
            context.SetRenderChildren(n =>
            {
                codeWriter.WriteLine("Render Children");
            });

            return context;
        }

        private static DocumentIntermediateNode Lower(RazorCodeDocument codeDocument)
        {
            var engine = RazorEngine.Create();
            return Lower(codeDocument, engine);
        }

        private static DocumentIntermediateNode LowerDesignTime(RazorCodeDocument codeDocument)
        {
            var engine = RazorEngine.CreateDesignTime();
            return Lower(codeDocument, engine);
        }

        private static DocumentIntermediateNode Lower(RazorCodeDocument codeDocument, RazorEngine engine)
        {
            for (var i = 0; i < engine.Phases.Count; i++)
            {
                var phase = engine.Phases[i];
                phase.Execute(codeDocument);

                if (phase is IRazorIntermediateNodeLoweringPhase)
                {
                    break;
                }
            }

            var irDocument = codeDocument.GetDocumentIntermediateNode();
            Assert.NotNull(irDocument);

            return irDocument;
        }

        private static TagHelperDescriptor CreateTagHelperDescriptor(
            string tagName,
            string typeName,
            string assemblyName,
            IEnumerable<Action<BoundAttributeDescriptorBuilder>> attributes = null)
        {
            var builder = TagHelperDescriptorBuilder.Create(typeName, assemblyName);
            builder.TypeName(typeName);

            if (attributes != null)
            {
                foreach (var attributeBuilder in attributes)
                {
                    builder.BindAttribute(attributeBuilder);
                }
            }

            builder.TagMatchingRule(ruleBuilder => ruleBuilder.RequireTagName(tagName));

            var descriptor = builder.Build();

            return descriptor;
        }
    }
}
