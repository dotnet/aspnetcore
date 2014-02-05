// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Linq;
using Microsoft.AspNet.Razor.Generator.Compiler;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.Text;
using Microsoft.Internal.Web.Utils;

namespace Microsoft.AspNet.Razor.Generator
{
    public class DynamicAttributeBlockCodeGenerator : BlockCodeGenerator
    {
        private const string ValueWriterName = "__razor_attribute_value_writer";
        private string _oldTargetWriter;
        private bool _isExpression;
        private ExpressionRenderingMode _oldRenderingMode;

        public DynamicAttributeBlockCodeGenerator(LocationTagged<string> prefix, int offset, int line, int col)
            : this(prefix, new SourceLocation(offset, line, col))
        {
        }

        public DynamicAttributeBlockCodeGenerator(LocationTagged<string> prefix, SourceLocation valueStart)
        {
            Prefix = prefix;
            ValueStart = valueStart;
        }

        public LocationTagged<string> Prefix { get; private set; }
        public SourceLocation ValueStart { get; private set; }

        public void GenerateStartBlockCode(SyntaxTreeNode target, CodeTreeBuilder codeTreeBuilder, CodeGeneratorContext context)
        {
            DynamicCodeAttributeChunk chunk = codeTreeBuilder.StartChunkBlock<DynamicCodeAttributeChunk>(target);
            chunk.Start = ValueStart;
            chunk.Prefix = Prefix;
        }

        public override void GenerateStartBlockCode(Block target, CodeGeneratorContext context)
        {
#if NET45
            // This code will not be needed once we transition to the CodeTree

            if (context.Host.DesignTimeMode)
            {
                return; // Don't generate anything!
            }

            // What kind of block is nested within
            string generatedCode;
#endif
            Block child = target.Children.Where(n => n.IsBlock).Cast<Block>().FirstOrDefault();

            if (child != null && child.Type == BlockType.Expression)
            {
                _isExpression = true;
#if NET45
                // No CodeDOM + This code will not be needed once we transition to the CodeTree

                generatedCode = context.BuildCodeString(cw =>
                {
                    cw.WriteParameterSeparator();
                    cw.WriteStartMethodInvoke("Tuple.Create");
                    cw.WriteLocationTaggedString(Prefix);
                    cw.WriteParameterSeparator();
                    cw.WriteStartMethodInvoke("Tuple.Create", "System.Object", "System.Int32");
                });
#endif
                _oldRenderingMode = context.ExpressionRenderingMode;
                context.ExpressionRenderingMode = ExpressionRenderingMode.InjectCode;
            }
#if NET45
            // No CodeDOM + This code will not be needed once we transition to the CodeTree

            else
            {
                generatedCode = context.BuildCodeString(cw =>
                {
                    cw.WriteParameterSeparator();
                    cw.WriteStartMethodInvoke("Tuple.Create");
                    cw.WriteLocationTaggedString(Prefix);
                    cw.WriteParameterSeparator();
                    cw.WriteStartMethodInvoke("Tuple.Create", "System.Object", "System.Int32");
                    cw.WriteStartConstructor(context.Host.GeneratedClassContext.TemplateTypeName);
                    cw.WriteStartLambdaDelegate(ValueWriterName);
                });
            }

            context.MarkEndOfGeneratedCode();
            context.BufferStatementFragment(generatedCode);
#endif
            _oldTargetWriter = context.TargetWriterName;
            context.TargetWriterName = ValueWriterName;

            // TODO: Make this generate the primary generator
            GenerateStartBlockCode(target, context.CodeTreeBuilder, context);
        }

        public void GenerateEndBlockCode(SyntaxTreeNode target, CodeTreeBuilder codeTreeBuilder, CodeGeneratorContext context)
        {
            codeTreeBuilder.EndChunkBlock();
        }

        public override void GenerateEndBlockCode(Block target, CodeGeneratorContext context)
        {
            if (context.Host.DesignTimeMode)
            {
                return; // Don't generate anything!
            }

            string generatedCode;
            if (_isExpression)
            {
#if NET45
                // No CodeDOM + This code will not be needed once we transition to the CodeTree

                generatedCode = context.BuildCodeString(cw =>
                {
                    cw.WriteParameterSeparator();
                    cw.WriteSnippet(ValueStart.AbsoluteIndex.ToString(CultureInfo.CurrentCulture));
                    cw.WriteEndMethodInvoke();
                    cw.WriteParameterSeparator();
                    // literal: false - This attribute value is not a literal value, it is dynamically generated
                    cw.WriteBooleanLiteral(false);
                    cw.WriteEndMethodInvoke();
                    cw.WriteLineContinuation();
                });
#endif
                context.ExpressionRenderingMode = _oldRenderingMode;
            }
#if NET45
            // No CodeDOM + This code will not be needed once we transition to the CodeTree

            else
            {
                generatedCode = context.BuildCodeString(cw =>
                {
                    cw.WriteEndLambdaDelegate();
                    cw.WriteEndConstructor();
                    cw.WriteParameterSeparator();
                    cw.WriteSnippet(ValueStart.AbsoluteIndex.ToString(CultureInfo.CurrentCulture));
                    cw.WriteEndMethodInvoke();
                    cw.WriteParameterSeparator();
                    // literal: false - This attribute value is not a literal value, it is dynamically generated
                    cw.WriteBooleanLiteral(false);
                    cw.WriteEndMethodInvoke();
                    cw.WriteLineContinuation();
                });
            }

            context.AddStatement(generatedCode);
#endif
            context.TargetWriterName = _oldTargetWriter;

            // TODO: Make this generate the primary generator
            GenerateEndBlockCode(target, context.CodeTreeBuilder, context);
        }

        public override string ToString()
        {
            return String.Format(CultureInfo.CurrentCulture, "DynAttr:{0:F}", Prefix);
        }

        public override bool Equals(object obj)
        {
            DynamicAttributeBlockCodeGenerator other = obj as DynamicAttributeBlockCodeGenerator;
            return other != null &&
                   Equals(other.Prefix, Prefix);
        }

        public override int GetHashCode()
        {
            return HashCodeCombiner.Start()
                .Add(Prefix)
                .CombinedHash;
        }
    }
}
