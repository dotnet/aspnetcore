// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Linq;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.Text;

namespace Microsoft.AspNet.Razor.Generator.Compiler.CSharp
{
    public class CSharpCodeVisitor : CodeVisitor<CSharpCodeWriter>
    {
        private const string ItemParameterName = "item";
        private const string ValueWriterName = "__razor_attribute_value_writer";
        private const string TemplateWriterName = "__razor_template_writer";

        private CSharpPaddingBuilder _paddingBuilder;

        public CSharpCodeVisitor(CSharpCodeWriter writer, CodeGeneratorContext context)
            : base(writer, context)
        {
            _paddingBuilder = new CSharpPaddingBuilder(context.Host);
        }

        protected override void Visit(SetLayoutChunk chunk)
        {
            if (!Context.Host.DesignTimeMode && !String.IsNullOrEmpty(Context.Host.GeneratedClassContext.LayoutPropertyName))
            {
                Writer.Write(Context.Host.GeneratedClassContext.LayoutPropertyName)
                       .Write(" = ")
                       .WriteStringLiteral(chunk.Layout)
                       .WriteLine(";");
            }
        }

        protected override void Visit(TemplateChunk chunk)
        {
            Writer.Write(ItemParameterName).Write(" => ")
                   .WriteStartNewObject(Context.Host.GeneratedClassContext.TemplateTypeName);

            string currentTargetWriterName = Context.TargetWriterName;
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
            if (!Context.Host.DesignTimeMode && String.IsNullOrEmpty(chunk.Url))
            {
                return;
            }

            // TODO: Add instrumentation

            if (!String.IsNullOrEmpty(chunk.Url) && !Context.Host.DesignTimeMode)
            {
                if (Context.ExpressionRenderingMode == ExpressionRenderingMode.WriteToOutput)
                {
                    if (!String.IsNullOrEmpty(Context.TargetWriterName))
                    {
                        Writer.WriteStartMethodInvocation(Context.Host.GeneratedClassContext.WriteLiteralToMethodName)
                               .Write(Context.TargetWriterName)
                               .WriteParameterSeparator();
                    }
                    else
                    {
                        Writer.WriteStartMethodInvocation(Context.Host.GeneratedClassContext.WriteLiteralMethodName);
                    }
                }

                Writer.WriteStartMethodInvocation(Context.Host.GeneratedClassContext.ResolveUrlMethodName)
                       .WriteStringLiteral(chunk.Url)
                       .WriteEndMethodInvocation(endLine: false);

                if (Context.ExpressionRenderingMode == ExpressionRenderingMode.WriteToOutput)
                {
                    Writer.WriteEndMethodInvocation();
                }
            }
        }

        protected override void Visit(LiteralChunk chunk)
        {
            if (!Context.Host.DesignTimeMode && String.IsNullOrEmpty(chunk.Text))
            {
                return;
            }

            // TODO: Add instrumentation

            if (!String.IsNullOrEmpty(chunk.Text) && !Context.Host.DesignTimeMode)
            {
                if (!String.IsNullOrEmpty(Context.TargetWriterName))
                {
                    Writer.WriteStartMethodInvocation(Context.Host.GeneratedClassContext.WriteLiteralToMethodName)
                           .Write(Context.TargetWriterName)
                           .WriteParameterSeparator();
                }
                else
                {
                    Writer.WriteStartMethodInvocation(Context.Host.GeneratedClassContext.WriteLiteralMethodName);
                }

                Writer.WriteStringLiteral(chunk.Text)
                       .WriteEndMethodInvocation();
            }

            // TODO: Add instrumentation
        }

        protected override void Visit(ExpressionBlockChunk chunk)
        {
            // TODO: Handle instrumentation

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
            CreateExpressionCodeMapping(chunk.Code, chunk);
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
                return; // Don't generate anything!
            }

            Chunk code = chunk.Children.FirstOrDefault();
            ExpressionRenderingMode currentRenderingMode = Context.ExpressionRenderingMode;
            string currentTargetWriterName = Context.TargetWriterName;

            Context.TargetWriterName = ValueWriterName;

            Writer.WriteParameterSeparator()
                   .WriteLine();

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
                       .WriteEndMethodInvocation(false)
                       .WriteParameterSeparator()
                       .WriteBooleanLiteral(false)
                       .WriteEndMethodInvocation(false);
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
            if (Context.Host.DesignTimeMode)
            {
                return; // Don't generate anything!
            }

            Writer.WriteParameterSeparator()
                   .WriteStartMethodInvocation("Tuple.Create")
                   .WriteLocationTaggedString(chunk.Prefix)
                   .WriteParameterSeparator();

            if (chunk.Children.Count > 0 || chunk.Value == null)
            {
                Writer.WriteStartMethodInvocation("Tuple.Create", new string[] { "System.Object", "System.Int32" });

                ExpressionRenderingMode currentRenderingMode = Context.ExpressionRenderingMode;
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
                return; // Don't generate anything!
            }

            if (!String.IsNullOrEmpty(Context.TargetWriterName))
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
                   .WriteParameterSeparator()
                   .WriteStartNewObject(Context.Host.GeneratedClassContext.TemplateTypeName);

            var currentTargetWriterName = Context.TargetWriterName;
            Context.TargetWriterName = TemplateWriterName;
            
            using (Writer.BuildLambda(endLine: false, parameterNames: TemplateWriterName))
            {
                Accept(chunk.Children);
            }
            Context.TargetWriterName = currentTargetWriterName;
            
            Writer.WriteEndMethodInvocation(endLine: false);
            Writer.WriteEndMethodInvocation();
        }

        public void RenderDesignTimeExpressionBlockChunk(ExpressionBlockChunk chunk)
        {
            // TODO: Handle instrumentation

            var firstChild = (ExpressionChunk)chunk.Children.FirstOrDefault();

            if (firstChild != null)
            {
                int currentIndent = Writer.CurrentIndent;
                string designTimeAssignment = "__o = ";
                Writer.ResetIndent();

                var documentLocation = firstChild.Association.Start;
                // This is only here to enable accurate formatting by the C# editor.
                Writer.WriteLineNumberDirective(documentLocation.LineIndex + 1, Context.SourceFile);

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
            // TODO: Handle instrumentation

            if (Context.ExpressionRenderingMode == ExpressionRenderingMode.InjectCode)
            {
                Accept(chunk.Children);
            }
            else if (Context.ExpressionRenderingMode == ExpressionRenderingMode.WriteToOutput)
            {
                if (!String.IsNullOrEmpty(Context.TargetWriterName))
                {
                    Writer.WriteStartMethodInvocation(Context.Host.GeneratedClassContext.WriteToMethodName)
                            .Write(Context.TargetWriterName)
                            .WriteParameterSeparator();
                }
                else
                {
                    Writer.WriteStartMethodInvocation(Context.Host.GeneratedClassContext.WriteMethodName);
                }

                Accept(chunk.Children);

                Writer.WriteEndMethodInvocation()
                      .WriteLine();
            }
        }

        public void CreateExpressionCodeMapping(string code, Chunk chunk)
        {
            CreateCodeMapping(_paddingBuilder.BuildExpressionPadding((Span)chunk.Association), code, chunk);
        }

        public void CreateStatementCodeMapping(string code, Chunk chunk)
        {
            CreateCodeMapping(_paddingBuilder.BuildStatementPadding((Span)chunk.Association), code, chunk);
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
    }
}
