using System;
using System.Globalization;
using System.Linq;

namespace Microsoft.AspNet.Razor.Generator.Compiler.CSharp
{
    public class CSharpCodeVisitor : CodeVisitor<CSharpCodeWriter>
    {
        private const string ValueWriterName = "__razor_attribute_value_writer";

        public CSharpCodeVisitor(CSharpCodeWriter writer, CodeGeneratorContext context)
            : base(writer, context) { }



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
            Writer.Write(TemplateBlockCodeGenerator.ItemParameterName).Write(" => ")
                   .WriteStartNewObject(Context.Host.GeneratedClassContext.TemplateTypeName);

            using (Writer.BuildLambda(endLine: false, parameterNames: TemplateBlockCodeGenerator.TemplateWriterName))
            {
                Visit((ChunkBlock)chunk);
            }

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
                if (chunk.RenderingMode == ExpressionRenderingMode.WriteToOutput)
                {
                    if (!String.IsNullOrEmpty(chunk.WriterName))
                    {
                        Writer.WriteStartMethodInvocation(Context.Host.GeneratedClassContext.WriteLiteralToMethodName)
                               .Write(chunk.WriterName)
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

                if (chunk.RenderingMode == ExpressionRenderingMode.WriteToOutput)
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
                if (!String.IsNullOrEmpty(chunk.WriterName))
                {
                    Writer.WriteStartMethodInvocation(Context.Host.GeneratedClassContext.WriteLiteralToMethodName)
                           .Write(chunk.WriterName)
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
            // TODO: Refactor

            if (!Context.Host.DesignTimeMode && chunk.RenderingMode == ExpressionRenderingMode.InjectCode)
            {
                Visit((ChunkBlock)chunk);
            }
            else
            {
                if (Context.Host.DesignTimeMode)
                {
                    Writer.WriteStartAssignment("__o");
                }
                else if (chunk.RenderingMode == ExpressionRenderingMode.WriteToOutput)
                {
                    if (!String.IsNullOrEmpty(chunk.WriterName))
                    {
                        Writer.WriteStartMethodInvocation(Context.Host.GeneratedClassContext.WriteToMethodName)
                               .Write(chunk.WriterName)
                               .WriteParameterSeparator();
                    }
                    else
                    {
                        Writer.WriteStartMethodInvocation(Context.Host.GeneratedClassContext.WriteMethodName);
                    }
                }

                Visit((ChunkBlock)chunk);

                if (Context.Host.DesignTimeMode)
                {
                    Writer.WriteLine(";");
                }
                else if (chunk.RenderingMode == ExpressionRenderingMode.WriteToOutput)
                {
                    Writer.WriteEndMethodInvocation();
                }
            }
        }

        protected override void Visit(ExpressionChunk chunk)
        {
            using (Writer.BuildLineMapping(chunk.Start, chunk.Code.Length, Context.SourceFile))
            {
                Writer.Indent(chunk.Start.CharacterIndex)
                        .Write(chunk.Code);
            }
        }

        protected override void Visit(StatementChunk chunk)
        {
            using (Writer.BuildLineMapping(chunk.Start, chunk.Code.Length, Context.SourceFile))
            {
                Writer.Indent(chunk.Start.CharacterIndex);
                Writer.WriteLine(chunk.Code);
            }
        }

        protected override void Visit(DynamicCodeAttributeChunk chunk)
        {
            if (Context.Host.DesignTimeMode)
            {
                return; // Don't generate anything!
            }

            Chunk code = chunk.Children.FirstOrDefault();

            Writer.WriteParameterSeparator()
                   .WriteLine();

            if (code is ExpressionChunk || code is ExpressionBlockChunk)
            {
                Writer.WriteStartMethodInvocation("Tuple.Create")
                        .WriteLocationTaggedString(chunk.Prefix)
                        .WriteParameterSeparator()
                        .WriteStartMethodInvocation("Tuple.Create", new string[] { "System.Object", "System.Int32" });

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
                    Visit((ChunkBlock)chunk);
                }

                Writer.WriteEndMethodInvocation(false)
                       .WriteParameterSeparator()
                       .Write(chunk.Start.AbsoluteIndex.ToString(CultureInfo.CurrentCulture))
                       .WriteEndMethodInvocation(endLine: false)
                       .WriteParameterSeparator()
                       .WriteBooleanLiteral(false)
                       .WriteEndMethodInvocation(false);
            }
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

                Visit((ChunkBlock)chunk);

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

            if (!String.IsNullOrEmpty(chunk.WriterName))
            {
                Writer.WriteStartMethodInvocation(Context.Host.GeneratedClassContext.WriteAttributeToMethodName)
                       .Write(chunk.WriterName)
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

            Visit((ChunkBlock)chunk);

            Writer.WriteEndMethodInvocation();
        }

        protected override void Visit(SectionChunk chunk)
        {
            Writer.WriteStartMethodInvocation(Context.Host.GeneratedClassContext.DefineSectionMethodName)
                   .WriteStringLiteral(chunk.Name)
                   .WriteParameterSeparator();

            using (Writer.BuildLambda(false))
            {
                Visit((ChunkBlock)chunk);
            }

            Writer.WriteEndMethodInvocation();
        }
    }
}
