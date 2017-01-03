// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Evolution.Intermediate;
using Microsoft.AspNetCore.Razor.Evolution.Legacy;

namespace Microsoft.AspNetCore.Razor.Evolution
{
    internal abstract class RazorCSharpLoweringPhaseBase : RazorEnginePhaseBase, IRazorCSharpLoweringPhase
    {
        protected static void RenderExpressionInline(RazorIRNode node, CSharpRenderingContext context)
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

        protected static int CalculateExpressionPadding(SourceSpan sourceRange, CSharpRenderingContext context)
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

        protected static string BuildOffsetPadding(int generatedOffset, SourceSpan sourceRange, CSharpRenderingContext context)
        {
            var basePadding = CalculateExpressionPadding(sourceRange, context);
            var resolvedPadding = Math.Max(basePadding - generatedOffset, 0);

            if (context.Options.IsIndentingWithTabs)
            {
                var spaces = resolvedPadding % context.Options.TabSize;
                var tabs = resolvedPadding / context.Options.TabSize;

                return new string('\t', tabs) + new string(' ', spaces);
            }
            else
            {
                return new string(' ', resolvedPadding);
            }
        }

        protected static string GetTagHelperVariableName(string tagHelperTypeName) => "__" + tagHelperTypeName.Replace('.', '_');

        protected static string GetTagHelperPropertyAccessor(
            string tagHelperVariableName,
            string attributeName,
            TagHelperAttributeDescriptor descriptor)
        {
            var propertyAccessor = $"{tagHelperVariableName}.{descriptor.PropertyName}";

            if (descriptor.IsIndexer)
            {
                var dictionaryKey = attributeName.Substring(descriptor.Name.Length);
                propertyAccessor += $"[\"{dictionaryKey}\"]";
            }

            return propertyAccessor;
        }

        protected class LinePragmaWriter : IDisposable
        {
            private readonly CSharpCodeWriter _writer;
            private readonly int _startIndent;

            public LinePragmaWriter(CSharpCodeWriter writer, SourceSpan documentLocation)
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

        protected class CSharpRedirectRenderingConventions : CSharpRenderingConventions
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

        protected class CSharpRenderingConventions
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

        protected class TagHelperHtmlAttributeRenderingConventions : CSharpRenderingConventions
        {
            public TagHelperHtmlAttributeRenderingConventions(CSharpCodeWriter writer) : base(writer)
            {
            }

            public override string StartWriteAttributeValueMethod => "AddHtmlAttributeValue(" /* ORIGINAL: AddHtmlAttributeValueMethodName */;
        }

        protected class CSharpLiteralCodeConventions : CSharpRenderingConventions
        {
            public CSharpLiteralCodeConventions(CSharpCodeWriter writer) : base(writer)
            {
            }

            public override string StartWriteMethod => StartWriteLiteralMethod;
        }

        protected class CSharpRenderingContext
        {
            private CSharpRenderingConventions _renderingConventions;

            public ICollection<DirectiveDescriptor> Directives { get; set; }

            public List<LineMapping> LineMappings { get; } = new List<LineMapping>();

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

            public ErrorSink ErrorSink { get; } = new ErrorSink();

            public RazorSourceDocument SourceDocument { get; set; }

            public RazorParserOptions Options { get; set; }

            public TagHelperRenderingContext TagHelperRenderingContext { get; set; }
        }

        protected class TagHelperRenderingContext
        {
            private Dictionary<string, string> _renderedBoundAttributes;
            private HashSet<string> _verifiedPropertyDictionaries;

            public Dictionary<string, string> RenderedBoundAttributes
            {
                get
                {
                    if (_renderedBoundAttributes == null)
                    {
                        _renderedBoundAttributes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    }

                    return _renderedBoundAttributes;
                }
            }

            public HashSet<string> VerifiedPropertyDictionaries
            {
                get
                {
                    if (_verifiedPropertyDictionaries == null)
                    {
                        _verifiedPropertyDictionaries = new HashSet<string>(StringComparer.Ordinal);
                    }

                    return _verifiedPropertyDictionaries;
                }
            }
        }

        protected class PageStructureCSharpRenderer : RazorIRNodeWalker
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
                Context.Writer.WriteLine("#pragma warning disable 1998");

                Context.Writer
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
    }
}
