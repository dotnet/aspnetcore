using System;
using System.Globalization;
using System.Linq;

namespace Microsoft.AspNet.Razor.Generator.Compiler.CSharp
{
    public class CSharpCodeVisitor : CodeVisitor
    {
        private const string ValueWriterName = "__razor_attribute_value_writer";

        private CSharpCodeWriter _writer;
        // TODO: No need for the entire host
        private RazorEngineHost _host;
        private string _sourceFile;

        public CSharpCodeVisitor(CSharpCodeWriter writer, RazorEngineHost host, string sourceFile)
        {
            _writer = writer;
            _host = host;
            _sourceFile = sourceFile;
        }

        protected override void Visit(SetLayoutChunk chunk)
        {
            if (!_host.DesignTimeMode && !String.IsNullOrEmpty(_host.GeneratedClassContext.LayoutPropertyName))
            {
                _writer.Write(_host.GeneratedClassContext.LayoutPropertyName)
                       .Write(" = ")
                       .WriteStringLiteral(chunk.Layout)
                       .WriteLine(";");
            }
        }

        protected override void Visit(TemplateChunk chunk)
        {
            _writer.Write(TemplateBlockCodeGenerator.ItemParameterName).Write(" => ")
                   .WriteStartNewObject(_host.GeneratedClassContext.TemplateTypeName);

            using (_writer.BuildLambda(endLine: false, parameterNames: TemplateBlockCodeGenerator.TemplateWriterName))
            {
                Visit((ChunkBlock)chunk);
            }

            _writer.WriteEndMethodInvocation(false).WriteLine();
        }

        protected override void Visit(ResolveUrlChunk chunk)
        {
            if (!_host.DesignTimeMode && String.IsNullOrEmpty(chunk.Url))
            {
                return;
            }

            // TODO: Add instrumentation

            if (!String.IsNullOrEmpty(chunk.Url) && !_host.DesignTimeMode)
            {
                if (chunk.RenderingMode == ExpressionRenderingMode.WriteToOutput)
                {
                    if (!String.IsNullOrEmpty(chunk.WriterName))
                    {
                        _writer.WriteStartMethodInvocation(_host.GeneratedClassContext.WriteLiteralToMethodName)
                               .Write(chunk.WriterName)
                               .WriteParameterSeparator();
                    }
                    else
                    {
                        _writer.WriteStartMethodInvocation(_host.GeneratedClassContext.WriteLiteralMethodName);
                    }
                }

                _writer.WriteStartMethodInvocation(_host.GeneratedClassContext.ResolveUrlMethodName)
                       .WriteStringLiteral(chunk.Url)
                       .WriteEndMethodInvocation(endLine: false);

                if (chunk.RenderingMode == ExpressionRenderingMode.WriteToOutput)
                {
                    _writer.WriteEndMethodInvocation();
                }
            }
        }

        protected override void Visit(LiteralChunk chunk)
        {
            if (!_host.DesignTimeMode && String.IsNullOrEmpty(chunk.Text))
            {
                return;
            }

            // TODO: Add instrumentation

            if (!String.IsNullOrEmpty(chunk.Text) && !_host.DesignTimeMode)
            {
                if (!String.IsNullOrEmpty(chunk.WriterName))
                {
                    _writer.WriteStartMethodInvocation(_host.GeneratedClassContext.WriteLiteralToMethodName)
                           .Write(chunk.WriterName)
                           .WriteParameterSeparator();
                }
                else
                {
                    _writer.WriteStartMethodInvocation(_host.GeneratedClassContext.WriteLiteralMethodName);
                }

                _writer.WriteStringLiteral(chunk.Text)
                       .WriteEndMethodInvocation();
            }

            // TODO: Add instrumentation
        }

        protected override void Visit(ExpressionBlockChunk chunk)
        {
            // TODO: Handle instrumentation
            // TODO: Refactor

            if (!_host.DesignTimeMode && chunk.RenderingMode == ExpressionRenderingMode.InjectCode)
            {
                Visit((ChunkBlock)chunk);
            }
            else
            {
                if (_host.DesignTimeMode)
                {
                    _writer.WriteStartAssignment("__o");
                }
                else if (chunk.RenderingMode == ExpressionRenderingMode.WriteToOutput)
                {
                    // TODO: Abstract padding out?

                    if (!String.IsNullOrEmpty(chunk.WriterName))
                    {
                        _writer.WriteStartMethodInvocation(_host.GeneratedClassContext.WriteToMethodName)
                               .Write(chunk.WriterName)
                               .WriteParameterSeparator();
                    }
                    else
                    {
                        _writer.WriteStartMethodInvocation(_host.GeneratedClassContext.WriteMethodName);
                    }
                }

                Visit((ChunkBlock)chunk);

                if (_host.DesignTimeMode)
                {
                    _writer.WriteLine(";");
                }
                else if (chunk.RenderingMode == ExpressionRenderingMode.WriteToOutput)
                {
                    _writer.WriteEndMethodInvocation();
                }
            }
        }

        protected override void Visit(ExpressionChunk chunk)
        {
            using (_writer.BuildLineMapping(chunk.Start, chunk.Code.Value.Length, _sourceFile))
            {
                _writer.Indent(chunk.Start.CharacterIndex)
                        .Write(chunk.Code.Value);
            }
        }

        protected override void Visit(StatementChunk chunk)
        {
            foreach (Snippet snippet in chunk.Code)
            {
                using (_writer.BuildLineMapping(chunk.Start, snippet.Value.Length, _sourceFile))
                {
                    _writer.Indent(chunk.Start.CharacterIndex);
                    _writer.WriteLine(snippet.Value);
                }
            }
        }

        protected override void Visit(DynamicCodeAttributeChunk chunk)
        {
            if (_host.DesignTimeMode)
            {
                return; // Don't generate anything!
            }

            Chunk code = chunk.Children.FirstOrDefault();

            _writer.WriteParameterSeparator()
                   .WriteLine();

            if (code is ExpressionChunk || code is ExpressionBlockChunk)
            {
                _writer.WriteStartMethodInvocation("Tuple.Create")
                        .WriteLocationTaggedString(chunk.Prefix)
                        .WriteParameterSeparator()
                        .WriteStartMethodInvocation("Tuple.Create", new string[] { "System.Object", "System.Int32" });

                Accept(code);

                _writer.WriteParameterSeparator()
                       .Write(chunk.Start.AbsoluteIndex.ToString(CultureInfo.CurrentCulture))
                       .WriteEndMethodInvocation(false)
                       .WriteParameterSeparator()
                       .WriteBooleanLiteral(false)
                       .WriteEndMethodInvocation(false);
            }
            else
            {
                _writer.WriteStartMethodInvocation("Tuple.Create")
                       .WriteLocationTaggedString(chunk.Prefix)
                       .WriteParameterSeparator()
                       .WriteStartMethodInvocation("Tuple.Create", new string[] { "System.Object", "System.Int32" })
                       .WriteStartNewObject(_host.GeneratedClassContext.TemplateTypeName);

                using (_writer.BuildLambda(endLine: false, parameterNames: ValueWriterName))
                {
                    Visit((ChunkBlock)chunk);
                }

                _writer.WriteEndMethodInvocation(false)
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
            if (_host.DesignTimeMode)
            {
                return; // Don't generate anything!
            }

            _writer.WriteParameterSeparator()
                   .WriteStartMethodInvocation("Tuple.Create")
                   .WriteLocationTaggedString(chunk.Prefix)
                   .WriteParameterSeparator();

            if (chunk.Children.Count > 0 || chunk.Value == null)
            {
                _writer.WriteStartMethodInvocation("Tuple.Create", new string[] { "System.Object", "System.Int32" });

                Visit((ChunkBlock)chunk);

                _writer.WriteParameterSeparator()
                       .Write(chunk.ValueLocation.AbsoluteIndex.ToString(CultureInfo.CurrentCulture))
                       .WriteEndMethodInvocation(false)
                       .WriteParameterSeparator()
                       .WriteBooleanLiteral(false)
                       .WriteEndMethodInvocation(false);

            }
            else
            {
                _writer.WriteLocationTaggedString(chunk.Value)
                       .WriteParameterSeparator()
                       .WriteBooleanLiteral(true)
                       .WriteEndMethodInvocation(false);
            }
        }

        protected override void Visit(CodeAttributeChunk chunk)
        {
            if (_host.DesignTimeMode)
            {
                return; // Don't generate anything!
            }

            if (!String.IsNullOrEmpty(chunk.WriterName))
            {
                _writer.WriteStartMethodInvocation(_host.GeneratedClassContext.WriteAttributeToMethodName)
                       .Write(chunk.WriterName)
                       .WriteParameterSeparator();
            }
            else
            {
                _writer.WriteStartMethodInvocation(_host.GeneratedClassContext.WriteAttributeMethodName);
            }

            _writer.WriteStringLiteral(chunk.Attribute)
                   .WriteParameterSeparator()
                   .WriteLocationTaggedString(chunk.Prefix)
                   .WriteParameterSeparator()
                   .WriteLocationTaggedString(chunk.Suffix);

            Visit((ChunkBlock)chunk);

            _writer.WriteEndMethodInvocation();
        }

        protected override void Visit(SectionChunk chunk)
        {
            _writer.WriteStartMethodInvocation(_host.GeneratedClassContext.DefineSectionMethodName)
                   .WriteStringLiteral(chunk.Name)
                   .WriteParameterSeparator();

            using (_writer.BuildLambda(false))
            {
                Visit((ChunkBlock)chunk);
            }

            _writer.WriteEndMethodInvocation();
        }
    }
}
