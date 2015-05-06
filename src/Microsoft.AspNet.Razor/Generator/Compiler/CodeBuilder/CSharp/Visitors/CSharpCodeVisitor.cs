// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Globalization;
using System.Linq;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Razor.Generator.Compiler.CSharp
{
    public class CSharpCodeVisitor : CodeVisitor<CSharpCodeWriter>
    {
        private const string ItemParameterName = "item";
        private const string ValueWriterName = "__razor_attribute_value_writer";
        private const string TemplateWriterName = "__razor_template_writer";

        private CSharpPaddingBuilder _paddingBuilder;
        private CSharpTagHelperCodeRenderer _tagHelperCodeRenderer;

        public CSharpCodeVisitor([NotNull] CSharpCodeWriter writer, [NotNull] CodeBuilderContext context)
            : base(writer, context)
        {
            _paddingBuilder = new CSharpPaddingBuilder(context.Host);
        }

        public CSharpTagHelperCodeRenderer TagHelperRenderer
        {
            get
            {
                if (_tagHelperCodeRenderer == null)
                {
                    _tagHelperCodeRenderer = new CSharpTagHelperCodeRenderer(this, Writer, Context);
                }

                return _tagHelperCodeRenderer;
            }
            [param: NotNull]
            set
            {
                _tagHelperCodeRenderer = value;
            }
        }

        /// <summary>
        /// Method used to write an <see cref="object"/> to the current output.
        /// </summary>
        /// <remarks>Default is to HTML encode all but a few types.</remarks>
        protected virtual string WriteMethodName
        {
            get
            {
                return Context.Host.GeneratedClassContext.WriteMethodName;
            }
        }

        /// <summary>
        /// Method used to write an <see cref="object"/> to a specified <see cref="System.IO.TextWriter"/>.
        /// </summary>
        /// <remarks>Default is to HTML encode all but a few types.</remarks>
        protected virtual string WriteToMethodName
        {
            get
            {
                return Context.Host.GeneratedClassContext.WriteToMethodName;
            }
        }

        protected override void Visit(TagHelperChunk chunk)
        {
            TagHelperRenderer.RenderTagHelper(chunk);
        }

        protected override void Visit(ChunkBlock chunk)
        {
            Accept(chunk.Children);
        }

        protected override void Visit(TemplateChunk chunk)
        {
            Writer.Write(ItemParameterName).Write(" => ")
                   .WriteStartNewObject(Context.Host.GeneratedClassContext.TemplateTypeName);

            var currentTargetWriterName = Context.TargetWriterName;
            Context.TargetWriterName = TemplateWriterName;

            using (Writer.BuildLambda(endLine: false, parameterNames: TemplateWriterName))
            {
                Accept(chunk.Children);
            }

            Context.TargetWriterName = currentTargetWriterName;

            Writer.WriteEndMethodInvocation(false).WriteLine();
        }

        protected override void Visit(ResolveUrlChunk chunk)
        {
            if (!Context.Host.DesignTimeMode && string.IsNullOrEmpty(chunk.Url))
            {
                return;
            }

            var generateInstrumentation = ShouldGenerateInstrumentationForExpressions();

            if (generateInstrumentation)
            {
                // Add a non-literal context call (non-literal because the expanded URL will not match the source
                // character-by-character)
                Writer.WriteStartInstrumentationContext(Context, chunk.Association, isLiteral: false);
            }

            if (!string.IsNullOrEmpty(chunk.Url) && !Context.Host.DesignTimeMode)
            {
                if (Context.ExpressionRenderingMode == ExpressionRenderingMode.WriteToOutput)
                {
                    RenderPreWriteStart();
                }

                Writer.WriteStartMethodInvocation(Context.Host.GeneratedClassContext.ResolveUrlMethodName)
                      .WriteStringLiteral(chunk.Url)
                      .WriteEndMethodInvocation(endLine: false);

                if (Context.ExpressionRenderingMode == ExpressionRenderingMode.WriteToOutput)
                {
                    Writer.WriteEndMethodInvocation();
                }
            }

            if (generateInstrumentation)
            {
                Writer.WriteEndInstrumentationContext(Context);
            }
        }

        protected override void Visit(LiteralChunk chunk)
        {
            if (Context.Host.DesignTimeMode || string.IsNullOrEmpty(chunk.Text))
            {
                // Skip generating the chunk if we're in design time or if the chunk is empty.
                return;
            }

            if (Context.Host.EnableInstrumentation)
            {
                Writer.WriteStartInstrumentationContext(Context, chunk.Association, isLiteral: true);
            }

            if (Context.ExpressionRenderingMode == ExpressionRenderingMode.WriteToOutput)
            {
                RenderPreWriteStart();
            }

            Writer.WriteStringLiteral(chunk.Text);

            if (Context.ExpressionRenderingMode == ExpressionRenderingMode.WriteToOutput)
            {
                Writer.WriteEndMethodInvocation();
            }

            if (Context.Host.EnableInstrumentation)
            {
                Writer.WriteEndInstrumentationContext(Context);
            }
        }

        protected override void Visit(ExpressionBlockChunk chunk)
        {
            if (Context.Host.DesignTimeMode)
            {
                RenderDesignTimeExpressionBlockChunk(chunk);
            }
            else
            {
                RenderRuntimeExpressionBlockChunk(chunk);
            }
        }

        protected override void Visit(ExpressionChunk chunk)
        {
            Writer.Write(chunk.Code);
        }

        protected override void Visit(StatementChunk chunk)
        {
            CreateStatementCodeMapping(chunk.Code, chunk);
            Writer.WriteLine();
        }

        protected override void Visit(DynamicCodeAttributeChunk chunk)
        {
            if (Context.Host.DesignTimeMode)
            {
                // Render the children as is without wrapping them in calls to WriteAttribute
                Accept(chunk.Children);
                return;
            }

            var currentRenderingMode = Context.ExpressionRenderingMode;
            var currentTargetWriterName = Context.TargetWriterName;

            Context.TargetWriterName = ValueWriterName;

            Writer.WriteParameterSeparator()
                   .WriteLine();

            var code = chunk.Children.FirstOrDefault();
            if (code is ExpressionChunk || code is ExpressionBlockChunk)
            {
                Writer.WriteStartMethodInvocation("Tuple.Create")
                        .WriteLocationTaggedString(chunk.Prefix)
                        .WriteParameterSeparator()
                        .WriteStartMethodInvocation("Tuple.Create", new string[] { "System.Object", "System.Int32" });

                Context.ExpressionRenderingMode = ExpressionRenderingMode.InjectCode;

                Accept(code);

                Writer.WriteParameterSeparator()
                       .Write(chunk.Start.AbsoluteIndex.ToString(CultureInfo.CurrentCulture))
                       .WriteEndMethodInvocation(endLine: false)
                       .WriteParameterSeparator()
                       .WriteBooleanLiteral(value: false)
                       .WriteEndMethodInvocation(endLine: false);
            }
            else
            {
                Writer.WriteStartMethodInvocation("Tuple.Create")
                       .WriteLocationTaggedString(chunk.Prefix)
                       .WriteParameterSeparator()
                       .WriteStartMethodInvocation("Tuple.Create", new string[] { "System.Object", "System.Int32" })
                       .WriteStartNewObject(Context.Host.GeneratedClassContext.TemplateTypeName);

                using (Writer.BuildLambda(endLine: false, parameterNames: ValueWriterName))
                {
                    Accept(chunk.Children);
                }

                Writer.WriteEndMethodInvocation(false)
                       .WriteParameterSeparator()
                       .Write(chunk.Start.AbsoluteIndex.ToString(CultureInfo.CurrentCulture))
                       .WriteEndMethodInvocation(endLine: false)
                       .WriteParameterSeparator()
                       .WriteBooleanLiteral(false)
                       .WriteEndMethodInvocation(false);
            }

            Context.TargetWriterName = currentTargetWriterName;
            Context.ExpressionRenderingMode = currentRenderingMode;
        }

        protected override void Visit(LiteralCodeAttributeChunk chunk)
        {
            var visitChildren = chunk.Value == null;

            if (Context.Host.DesignTimeMode)
            {
                // Render the attribute without wrapping it in a call to WriteAttribute
                if (visitChildren)
                {
                    Accept(chunk.Children);
                }

                return;
            }

            Writer.WriteParameterSeparator()
                   .WriteStartMethodInvocation("Tuple.Create")
                   .WriteLocationTaggedString(chunk.Prefix)
                   .WriteParameterSeparator();

            if (visitChildren)
            {
                Writer.WriteStartMethodInvocation("Tuple.Create", new string[] { "System.Object", "System.Int32" });

                var currentRenderingMode = Context.ExpressionRenderingMode;
                Context.ExpressionRenderingMode = ExpressionRenderingMode.InjectCode;

                Accept(chunk.Children);

                Context.ExpressionRenderingMode = currentRenderingMode;

                Writer.WriteParameterSeparator()
                       .Write(chunk.ValueLocation.AbsoluteIndex.ToString(CultureInfo.CurrentCulture))
                       .WriteEndMethodInvocation(false)
                       .WriteParameterSeparator()
                       .WriteBooleanLiteral(false)
                       .WriteEndMethodInvocation(false);
            }
            else
            {
                Writer.WriteLocationTaggedString(chunk.Value)
                       .WriteParameterSeparator()
                       .WriteBooleanLiteral(true)
                       .WriteEndMethodInvocation(false);
            }
        }

        protected override void Visit(CodeAttributeChunk chunk)
        {
            if (Context.Host.DesignTimeMode)
            {
                // Render the attribute without wrapping it in a "WriteAttribute" invocation
                Accept(chunk.Children);

                return;
            }

            if (!string.IsNullOrEmpty(Context.TargetWriterName))
            {
                Writer.WriteStartMethodInvocation(Context.Host.GeneratedClassContext.WriteAttributeToMethodName)
                       .Write(Context.TargetWriterName)
                       .WriteParameterSeparator();
            }
            else
            {
                Writer.WriteStartMethodInvocation(Context.Host.GeneratedClassContext.WriteAttributeMethodName);
            }

            Writer.WriteStringLiteral(chunk.Attribute)
                   .WriteParameterSeparator()
                   .WriteLocationTaggedString(chunk.Prefix)
                   .WriteParameterSeparator()
                   .WriteLocationTaggedString(chunk.Suffix);

            Accept(chunk.Children);

            Writer.WriteEndMethodInvocation();
        }

        protected override void Visit(SectionChunk chunk)
        {
            Writer.WriteStartMethodInvocation(Context.Host.GeneratedClassContext.DefineSectionMethodName)
                   .WriteStringLiteral(chunk.Name)
                   .WriteParameterSeparator();

            var currentTargetWriterName = Context.TargetWriterName;
            Context.TargetWriterName = TemplateWriterName;

            using (Writer.BuildAsyncLambda(endLine: false, parameterNames: TemplateWriterName))
            {
                Accept(chunk.Children);
            }
            Context.TargetWriterName = currentTargetWriterName;
            Writer.WriteEndMethodInvocation();
        }

        public void RenderDesignTimeExpressionBlockChunk(ExpressionBlockChunk chunk)
        {
            var firstChild = (ExpressionChunk)chunk.Children.FirstOrDefault();

            if (firstChild != null)
            {
                var currentIndent = Writer.CurrentIndent;
                var designTimeAssignment = "__o = ";
                Writer.ResetIndent();

                var documentLocation = firstChild.Association.Start;
                // This is only here to enable accurate formatting by the C# editor.
                Writer.WriteLineNumberDirective(documentLocation, Context.SourceFile);

                // We build the padding with an offset of the design time assignment statement.
                Writer.Write(_paddingBuilder.BuildExpressionPadding((Span)firstChild.Association, designTimeAssignment.Length))
                      .Write(designTimeAssignment);

                // We map the first line of code but do not write the line pragmas associated with it.
                CreateRawCodeMapping(firstChild.Code, documentLocation);

                // Render all but the first child.
                // The reason why we render the other children differently is because when formatting the C# code
                // the formatter expects the start line to have the assignment statement on it.
                Accept(chunk.Children.Skip(1).ToList());

                Writer.WriteLine(";")
                      .WriteLine()
                      .WriteLineDefaultDirective()
                      .WriteLineHiddenDirective()
                      .SetIndent(currentIndent);
            }
        }

        public void RenderRuntimeExpressionBlockChunk(ExpressionBlockChunk chunk)
        {
            // For expression chunks, such as @value, @(value) etc, pick the first Code or Markup span
            // from the expression (in this case "value") and use that to calculate the length. This works
            // accurately for most parts. The scenarios that don't work are
            // (a) Expressions with inline comments (e.g. @(a @* comment *@ b)) - these have multiple code spans
            // (b) Expressions with inline templates (e.g. @Foo(@<p>Hello world</p>)).
            // Tracked via https://github.com/aspnet/Razor/issues/153

            var block = (Block)chunk.Association;
            var contentSpan = block.Children
                               .OfType<Span>()
                               .FirstOrDefault(s => s.Kind == SpanKind.Code || s.Kind == SpanKind.Markup);

            if (Context.ExpressionRenderingMode == ExpressionRenderingMode.InjectCode)
            {
                Accept(chunk.Children);
            }
            else if (Context.ExpressionRenderingMode == ExpressionRenderingMode.WriteToOutput)
            {
                if (contentSpan != null)
                {
                    RenderRuntimeExpressionBlockChunkWithContentSpan(chunk, contentSpan);
                }
                else
                {
                    if (!string.IsNullOrEmpty(Context.TargetWriterName))
                    {
                        Writer
                            .WriteStartMethodInvocation(WriteToMethodName)
                            .Write(Context.TargetWriterName)
                            .WriteParameterSeparator();
                    }
                    else
                    {
                        Writer.WriteStartMethodInvocation(WriteMethodName);
                    }

                    Accept(chunk.Children);

                    Writer.WriteEndMethodInvocation()
                          .WriteLine();
                }
            }
        }

        private void RenderRuntimeExpressionBlockChunkWithContentSpan(ExpressionBlockChunk chunk, Span contentSpan)
        {
            var generateInstrumentation = ShouldGenerateInstrumentationForExpressions();

            if (generateInstrumentation)
            {
                Writer.WriteStartInstrumentationContext(Context, contentSpan, isLiteral: false);
            }

            using (var mappingWriter = new CSharpLineMappingWriter(Writer, chunk.Start, Context.SourceFile))
            {
                if (!string.IsNullOrEmpty(Context.TargetWriterName))
                {
                    var generatedStart =
                        WriteToMethodName.Length +
                        Context.TargetWriterName.Length +
                        3; // 1 for the opening '(' and 2 for ', '

                    var padding = _paddingBuilder.BuildExpressionPadding(contentSpan, generatedStart);

                    Writer
                        .Write(padding)
                        .WriteStartMethodInvocation(WriteToMethodName)
                        .Write(Context.TargetWriterName)
                        .WriteParameterSeparator();
                }
                else
                {
                    var generatedStart =
                        WriteMethodName.Length +
                         1; // for the opening '('

                    var padding = _paddingBuilder.BuildExpressionPadding(contentSpan, generatedStart);

                    Writer
                        .Write(padding)
                        .WriteStartMethodInvocation(WriteMethodName);
                }

                Accept(chunk.Children);

                Writer.WriteEndMethodInvocation();
            }

            if (generateInstrumentation)
            {
                Writer.WriteEndInstrumentationContext(Context);
            }
        }

        public void CreateStatementCodeMapping(string code, Chunk chunk)
        {
            CreateCodeMapping(_paddingBuilder.BuildStatementPadding((Span)chunk.Association), code, chunk);
        }

        public void CreateExpressionCodeMapping(string code, Chunk chunk)
        {
            CreateCodeMapping(_paddingBuilder.BuildExpressionPadding((Span)chunk.Association), code, chunk);
        }

        public void CreateCodeMapping(string padding, string code, Chunk chunk)
        {
            using (CSharpLineMappingWriter mappingWriter = Writer.BuildLineMapping(chunk.Start, code.Length, Context.SourceFile))
            {
                Writer.Write(padding);

                mappingWriter.MarkLineMappingStart();
                Writer.Write(code);
                mappingWriter.MarkLineMappingEnd();
            }
        }

        // Raw CodeMapping's do not write out line pragmas, they just map code.
        public void CreateRawCodeMapping(string code, SourceLocation documentLocation)
        {
            using (new CSharpLineMappingWriter(Writer, documentLocation, code.Length))
            {
                Writer.Write(code);
            }
        }

        private bool ShouldGenerateInstrumentationForExpressions()
        {
            // Only generate instrumentation for expression blocks if instrumentation is enabled and we're generating a
            // "Write(<expression>)" statement.
            return Context.Host.EnableInstrumentation &&
                   Context.ExpressionRenderingMode == ExpressionRenderingMode.WriteToOutput;
        }

        private CSharpCodeWriter RenderPreWriteStart()
        {
            return RenderPreWriteStart(Writer, Context);
        }

        public static CSharpCodeWriter RenderPreWriteStart(CSharpCodeWriter writer, CodeBuilderContext context)
        {
            if (!string.IsNullOrEmpty(context.TargetWriterName))
            {
                writer.WriteStartMethodInvocation(context.Host.GeneratedClassContext.WriteLiteralToMethodName)
                      .Write(context.TargetWriterName)
                      .WriteParameterSeparator();
            }
            else
            {
                writer.WriteStartMethodInvocation(context.Host.GeneratedClassContext.WriteLiteralMethodName);
            }

            return writer;
        }
    }
}
