// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Razor.Evolution.Intermediate;
using Microsoft.AspNetCore.Razor.Evolution.Legacy;

namespace Microsoft.AspNetCore.Razor.Evolution
{
    internal class DefaultRazorCSharpLoweringPhase : RazorEnginePhaseBase, IRazorCSharpLoweringPhase
    {
        private IRazorConfigureParserFeature[] _parserOptionsCallbacks;

        protected override void OnIntialized()
        {
            _parserOptionsCallbacks = Engine.Features.OfType<IRazorConfigureParserFeature>().ToArray();
        }

        protected override void ExecuteCore(RazorCodeDocument codeDocument)
        {
            var irDocument = codeDocument.GetIRDocument();
            ThrowForMissingDependency(irDocument);

            var syntaxTree = codeDocument.GetSyntaxTree();
            ThrowForMissingDependency(syntaxTree);

            var renderingContext = new CSharpRenderingContext()
            {
                Writer = new CSharpCodeWriter(),
                SourceDocument = codeDocument.Source,
                Options = syntaxTree.Options,
            };
            var visitor = new CSharpRenderer(renderingContext);
            visitor.VisitDefault(irDocument);
            var csharpDocument = new RazorCSharpDocument()
            {
                GeneratedCode = renderingContext.Writer.GenerateCode(),
                LineMappings = renderingContext.Writer.LineMappingManager.Mappings,
            };

            codeDocument.SetCSharpDocument(csharpDocument);
        }

        public class CSharpRedirectRenderingConventions : CSharpRenderingConventions
        {
            private readonly string _redirectWriter;

            public CSharpRedirectRenderingConventions(string redirectWriter, CSharpCodeWriter writer) : base(writer)
            {
                _redirectWriter = redirectWriter;
            }

            public override string StartWriteMethod => "WriteTo(" + _redirectWriter + ", " /* ORIGINAL: WriteToMethodName */;

            public override string StartWriteLiteralMethod => "WriteLiteralTo(" + _redirectWriter + ", " /* ORIGINAL: WriteLiteralToMethodName */;


            public override string StartBeginWriteAttributeMethod => "BeginWriteAttributeTo(" + _redirectWriter + ", " /* ORIGINAL: BeginWriteAttributeToMethodName */;


            public override string StartWriteAttributeValueMethod => "WriteAttributeValueTo(" + _redirectWriter + ", " /* ORIGINAL: WriteAttributeValueToMethodName */;


            public override string StartEndWriteAttributeMethod => "EndWriteAttributeTo(" + _redirectWriter /* ORIGINAL: EndWriteAttributeToMethodName */;
        }

        public class CSharpRenderingConventions
        {
            public CSharpRenderingConventions(CSharpCodeWriter writer)
            {
                Writer = writer;
            }

            protected CSharpCodeWriter Writer { get; }

            public virtual string StartWriteMethod => "Write(" /* ORIGINAL: WriteMethodName */;

            public virtual string StartWriteLiteralMethod => "WriteLiteral(" /* ORIGINAL: WriteLiteralMethodName */;

            public virtual string StartBeginWriteAttributeMethod => "BeginWriteAttribute(" /* ORIGINAL: BeginWriteAttributeMethodName */;

            public virtual string StartWriteAttributeValueMethod => "WriteAttributeValue(" /* ORIGINAL: WriteAttributeValueMethodName */;

            public virtual string StartEndWriteAttributeMethod => "EndWriteAttribute(" /* ORIGINAL: EndWriteAttributeMethodName */;
        }

        public class CSharpRenderingContext
        {
            private CSharpRenderingConventions _renderingConventions;

            public ICollection<DirectiveDescriptor> Directives { get; set; }

            public CSharpCodeWriter Writer { get; set; }

            public CSharpRenderingConventions RenderingConventions
            {
                get
                {
                    if (_renderingConventions == null)
                    {
                        _renderingConventions = new CSharpRenderingConventions(Writer);
                    }

                    return _renderingConventions;
                }
                set
                {
                    _renderingConventions = value;
                }
            }

            public ICollection<RazorError> Errors { get; } = new List<RazorError>();

            public RazorSourceDocument SourceDocument { get; set; }

            public RazorParserOptions Options { get; set; }
        }

        public class LinePragmaWriter : IDisposable
        {
            private readonly CSharpCodeWriter _writer;
            private readonly int _startIndent;

            public LinePragmaWriter(CSharpCodeWriter writer, MappingLocation documentLocation)
            {
                if (writer == null)
                {
                    throw new ArgumentNullException(nameof(writer));
                }

                _writer = writer;
                _startIndent = _writer.CurrentIndent;
                _writer.ResetIndent();
                _writer.WriteLineNumberDirective(documentLocation, documentLocation.FilePath);
            }

            public void Dispose()
            {
                // Need to add an additional line at the end IF there wasn't one already written.
                // This is needed to work with the C# editor's handling of #line ...
                var builder = _writer.Builder;
                var endsWithNewline = builder.Length > 0 && builder[builder.Length - 1] == '\n';

                // Always write at least 1 empty line to potentially separate code from pragmas.
                _writer.WriteLine();

                // Check if the previous empty line wasn't enough to separate code from pragmas.
                if (!endsWithNewline)
                {
                    _writer.WriteLine();
                }

                _writer
                    .WriteLineDefaultDirective()
                    .WriteLineHiddenDirective()
                    .SetIndent(_startIndent);
            }
        }

        public class PageStructureCSharpRenderer : RazorIRNodeWalker
        {
            protected readonly CSharpRenderingContext Context;

            public PageStructureCSharpRenderer(CSharpRenderingContext context)
            {
                Context = context;
            }

            public override void VisitNamespace(NamespaceDeclarationIRNode node)
            {
                Context.Writer
                    .Write("namespace ")
                    .WriteLine(node.Content);

                using (Context.Writer.BuildScope())
                {
                    Context.Writer.WriteLineHiddenDirective();
                    VisitDefault(node);
                }
            }

            public override void VisitRazorMethodDeclaration(RazorMethodDeclarationIRNode node)
            {
                Context.Writer
                    .WriteLine("#pragma warning disable 1998")
                    .Write(node.AccessModifier)
                    .Write(" ");

                if (node.Modifiers != null)
                {
                    for (var i = 0; i < node.Modifiers.Count; i++)
                    {
                        Context.Writer.Write(node.Modifiers[i]);

                        if (i + 1 < node.Modifiers.Count)
                        {
                            Context.Writer.Write(" ");
                        }
                    }
                }

                Context.Writer
                    .Write(" ")
                    .Write(node.ReturnType)
                    .Write(" ")
                    .Write(node.Name)
                    .WriteLine("()");

                using (Context.Writer.BuildScope())
                {
                    VisitDefault(node);
                }

                Context.Writer.WriteLine("#pragma warning restore 1998");
            }

            public override void VisitClass(ClassDeclarationIRNode node)
            {
                Context.Writer
                    .Write(node.AccessModifier)
                    .Write(" class ")
                    .Write(node.Name);

                if (node.BaseType != null || node.Interfaces != null)
                {
                    Context.Writer.Write(" : ");
                }

                if (node.BaseType != null)
                {
                    Context.Writer.Write(node.BaseType);

                    if (node.Interfaces != null)
                    {
                        Context.Writer.WriteParameterSeparator();
                    }
                }

                if (node.Interfaces != null)
                {
                    for (var i = 0; i < node.Interfaces.Count; i++)
                    {
                        Context.Writer.Write(node.Interfaces[i]);

                        if (i + 1 < node.Interfaces.Count)
                        {
                            Context.Writer.WriteParameterSeparator();
                        }
                    }
                }

                Context.Writer.WriteLine();

                using (Context.Writer.BuildScope())
                {
                    VisitDefault(node);
                }
            }
        }

        public class CSharpRenderer : PageStructureCSharpRenderer
        {
            public CSharpRenderer(CSharpRenderingContext context) : base(context)
            {
            }

            public override void VisitChecksum(ChecksumIRNode node)
            {
                if (!string.IsNullOrEmpty(node.Bytes))
                {
                    Context.Writer
                    .Write("#pragma checksum \"")
                    .Write(node.Filename)
                    .Write("\" \"")
                    .Write(node.Guid)
                    .Write("\" \"")
                    .Write(node.Bytes)
                    .WriteLine("\"");
                }
            }

            public override void VisitCSharpToken(CSharpTokenIRNode node)
            {
                Context.Writer.Write(node.Content);
            }

            public override void VisitHtml(HtmlContentIRNode node)
            {
                const int MaxStringLiteralLength = 1024;

                var charactersConsumed = 0;

                // Render the string in pieces to avoid Roslyn OOM exceptions at compile time: https://github.com/aspnet/External/issues/54
                while (charactersConsumed < node.Content.Length)
                {
                    string textToRender;
                    if (node.Content.Length <= MaxStringLiteralLength)
                    {
                        textToRender = node.Content;
                    }
                    else
                    {
                        var charactersToSubstring = Math.Min(MaxStringLiteralLength, node.Content.Length - charactersConsumed);
                        textToRender = node.Content.Substring(charactersConsumed, charactersToSubstring);
                    }

                    Context.Writer
                        .Write(Context.RenderingConventions.StartWriteLiteralMethod)
                        .WriteStringLiteral(textToRender)
                        .WriteEndMethodInvocation();

                    charactersConsumed += textToRender.Length;
                }
            }

            public override void VisitCSharpExpression(CSharpExpressionIRNode node)
            {
                IDisposable linePragmaScope = null;
                if (node.SourceRange != null)
                {
                    linePragmaScope = new LinePragmaWriter(Context.Writer, node.SourceRange);
                }

                var padding = BuildOffsetPadding(Context.RenderingConventions.StartWriteMethod.Length, node.SourceRange);
                Context.Writer
                    .Write(padding)
                    .Write(Context.RenderingConventions.StartWriteMethod);

                VisitDefault(node);

                Context.Writer.WriteEndMethodInvocation();

                linePragmaScope?.Dispose();
            }

            public override void VisitUsingStatement(UsingStatementIRNode node)
            {
                Context.Writer.WriteUsing(node.Content);
            }

            public override void VisitHtmlAttribute(HtmlAttributeIRNode node)
            {
                var valuePieceCount = node
                    .Children
                    .Count(child => child is HtmlAttributeValueIRNode || child is CSharpAttributeValueIRNode);
                var prefixLocation = node.SourceRange.AbsoluteIndex;
                var suffixLocation = node.SourceRange.AbsoluteIndex + node.SourceRange.ContentLength - node.Suffix.Length;
                Context.Writer
                    .Write(Context.RenderingConventions.StartBeginWriteAttributeMethod)
                    .WriteStringLiteral(node.Name)
                    .WriteParameterSeparator()
                    .WriteStringLiteral(node.Prefix)
                    .WriteParameterSeparator()
                    .Write(prefixLocation.ToString(CultureInfo.InvariantCulture))
                    .WriteParameterSeparator()
                    .WriteStringLiteral(node.Suffix)
                    .WriteParameterSeparator()
                    .Write(suffixLocation.ToString(CultureInfo.InvariantCulture))
                    .WriteParameterSeparator()
                    .Write(valuePieceCount.ToString(CultureInfo.InvariantCulture))
                    .WriteEndMethodInvocation();

                VisitDefault(node);

                Context.Writer
                    .Write(Context.RenderingConventions.StartEndWriteAttributeMethod)
                    .WriteEndMethodInvocation();
            }

            public override void VisitHtmlAttributeValue(HtmlAttributeValueIRNode node)
            {
                var prefixLocation = node.SourceRange.AbsoluteIndex;
                var valueLocation = node.SourceRange.AbsoluteIndex + node.Prefix.Length;
                var valueLength = node.SourceRange.ContentLength;
                Context.Writer
                    .Write(Context.RenderingConventions.StartWriteAttributeValueMethod)
                    .WriteStringLiteral(node.Prefix)
                    .WriteParameterSeparator()
                    .Write(prefixLocation.ToString(CultureInfo.InvariantCulture))
                    .WriteParameterSeparator()
                    .WriteStringLiteral(node.Content)
                    .WriteParameterSeparator()
                    .Write(valueLocation.ToString(CultureInfo.InvariantCulture))
                    .WriteParameterSeparator()
                    .Write(valueLength.ToString(CultureInfo.InvariantCulture))
                    .WriteParameterSeparator()
                    .WriteBooleanLiteral(true)
                    .WriteEndMethodInvocation();
            }

            public override void VisitCSharpAttributeValue(CSharpAttributeValueIRNode node)
            {
                const string ValueWriterName = "__razor_attribute_value_writer";

                var expressionValue = node.Children.First() as CSharpExpressionIRNode;
                var linePragma = expressionValue != null ? new LinePragmaWriter(Context.Writer, node.SourceRange) : null;
                var prefixLocation = node.SourceRange.AbsoluteIndex;
                var valueLocation = node.SourceRange.AbsoluteIndex + node.Prefix.Length;
                var valueLength = node.SourceRange.ContentLength - node.Prefix.Length;
                Context.Writer
                    .Write(Context.RenderingConventions.StartWriteAttributeValueMethod)
                    .WriteStringLiteral(node.Prefix)
                    .WriteParameterSeparator()
                    .Write(prefixLocation.ToString(CultureInfo.InvariantCulture))
                    .WriteParameterSeparator();

                if (expressionValue != null)
                {
                    Debug.Assert(node.Children.Count == 1);

                    RenderExpressionInline(expressionValue, Context);
                }
                else
                {
                    // Not an expression; need to buffer the result.
                    Context.Writer.WriteStartNewObject("HelperResult" /* ORIGINAL: TemplateTypeName */);

                    var initialRenderingConventions = Context.RenderingConventions;
                    var redirectConventions = new CSharpRedirectRenderingConventions(ValueWriterName, Context.Writer);
                    Context.RenderingConventions = redirectConventions;
                    using (Context.Writer.BuildAsyncLambda(endLine: false, parameterNames: ValueWriterName))
                    {
                        VisitDefault(node);
                    }
                    Context.RenderingConventions = initialRenderingConventions;

                    Context.Writer.WriteEndMethodInvocation(false);
                }

                Context.Writer
                    .WriteParameterSeparator()
                    .Write(valueLocation.ToString(CultureInfo.InvariantCulture))
                    .WriteParameterSeparator()
                    .Write(valueLength.ToString(CultureInfo.InvariantCulture))
                    .WriteParameterSeparator()
                    .WriteBooleanLiteral(false)
                    .WriteEndMethodInvocation();

                linePragma?.Dispose();
            }

            public override void VisitDirective(DirectiveIRNode node)
            {
                if (string.Equals(node.Name, CSharpCodeParser.SectionDirectiveDescriptor.Name, StringComparison.Ordinal))
                {
                    const string SectionWriterName = "__razor_section_writer";

                    Context.Writer
                        .WriteStartMethodInvocation("DefineSection" /* ORIGINAL: DefineSectionMethodName */)
                        .WriteStringLiteral(node.Tokens.FirstOrDefault()?.Content)
                        .WriteParameterSeparator();

                    var initialRenderingConventions = Context.RenderingConventions;
                    var redirectConventions = new CSharpRedirectRenderingConventions(SectionWriterName, Context.Writer);
                    Context.RenderingConventions = redirectConventions;
                    using (Context.Writer.BuildAsyncLambda(endLine: false, parameterNames: SectionWriterName))
                    {
                        VisitDefault(node);
                    }
                    Context.RenderingConventions = initialRenderingConventions;

                    Context.Writer.WriteEndMethodInvocation();
                }
            }

            public override void VisitCSharpStatement(CSharpStatementIRNode node)
            {
                if (string.IsNullOrWhiteSpace(node.Content))
                {
                    return;
                }

                if (node.SourceRange != null)
                {
                    using (new LinePragmaWriter(Context.Writer, node.SourceRange))
                    {
                        var padding = BuildOffsetPadding(0, node.SourceRange);
                        Context.Writer
                            .Write(padding)
                            .WriteLine(node.Content);
                    }
                }
                else
                {
                    Context.Writer.WriteLine(node.Content);
                }
            }

            public override void VisitTemplate(TemplateIRNode node)
            {
                const string ItemParameterName = "item";
                const string TemplateWriterName = "__razor_template_writer";

                Context.Writer
                    .Write(ItemParameterName).Write(" => ")
                    .WriteStartNewObject("HelperResult" /* ORIGINAL: TemplateTypeName */);

                var initialRenderingConventions = Context.RenderingConventions;
                var redirectConventions = new CSharpRedirectRenderingConventions(TemplateWriterName, Context.Writer);
                Context.RenderingConventions = redirectConventions;
                using (Context.Writer.BuildAsyncLambda(endLine: false, parameterNames: TemplateWriterName))
                {
                    VisitDefault(node);
                }
                Context.RenderingConventions = initialRenderingConventions;

                Context.Writer.WriteEndMethodInvocation(endLine: false);
            }

            private static int CalculateExpressionPadding(MappingLocation sourceRange, CSharpRenderingContext context)
            {
                var spaceCount = 0;
                for (var i = sourceRange.AbsoluteIndex - 1; i >= 0; i--)
                {
                    var @char = context.SourceDocument[i];
                    if (@char == '\n' || @char == '\r')
                    {
                        break;
                    }
                    else if (@char == '\t')
                    {
                        spaceCount += context.Options.TabSize;
                    }
                    else
                    {
                        spaceCount++;
                    }
                }

                return spaceCount;
            }

            private string BuildOffsetPadding(int generatedOffset, MappingLocation sourceRange)
            {
                var basePadding = CalculateExpressionPadding(sourceRange, Context);
                var resolvedPadding = Math.Max(basePadding - generatedOffset, 0);

                if (Context.Options.IsIndentingWithTabs)
                {
                    var spaces = resolvedPadding % Context.Options.TabSize;
                    var tabs = resolvedPadding / Context.Options.TabSize;

                    return new string('\t', tabs) + new string(' ', spaces);
                }
                else
                {
                    return new string(' ', resolvedPadding);
                }
            }

            private static void RenderExpressionInline(RazorIRNode node, CSharpRenderingContext context)
            {
                if (node is CSharpTokenIRNode)
                {
                    context.Writer.Write(((CSharpTokenIRNode)node).Content);
                }
                else
                {
                    for (var i = 0; i < node.Children.Count; i++)
                    {
                        RenderExpressionInline(node.Children[i], context);
                    }
                }
            }
        }
    }
}
