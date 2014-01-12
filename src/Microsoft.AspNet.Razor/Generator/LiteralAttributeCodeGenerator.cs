// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Globalization;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.Text;
using Microsoft.Internal.Web.Utils;
using System;

namespace Microsoft.AspNet.Razor.Generator
{
    public class LiteralAttributeCodeGenerator : SpanCodeGenerator
    {
        public LiteralAttributeCodeGenerator(LocationTagged<string> prefix, LocationTagged<SpanCodeGenerator> valueGenerator)
        {
            Prefix = prefix;
            ValueGenerator = valueGenerator;
        }

        public LiteralAttributeCodeGenerator(LocationTagged<string> prefix, LocationTagged<string> value)
        {
            Prefix = prefix;
            Value = value;
        }

        public LocationTagged<string> Prefix { get; private set; }
        public LocationTagged<string> Value { get; private set; }
        public LocationTagged<SpanCodeGenerator> ValueGenerator { get; private set; }

        public override void GenerateCode(Span target, CodeGeneratorContext context)
        {
            if (context.Host.DesignTimeMode)
            {
                return;
            }
            ExpressionRenderingMode oldMode = context.ExpressionRenderingMode;
            context.BufferStatementFragment(context.BuildCodeString(cw =>
            {
                cw.WriteParameterSeparator();
                cw.WriteStartMethodInvoke("Tuple.Create");
                cw.WriteLocationTaggedString(Prefix);
                cw.WriteParameterSeparator();
                if (ValueGenerator != null)
                {
                    cw.WriteStartMethodInvoke("Tuple.Create", "System.Object", "System.Int32");
                    context.ExpressionRenderingMode = ExpressionRenderingMode.InjectCode;
                }
                else
                {
                    cw.WriteLocationTaggedString(Value);
                    cw.WriteParameterSeparator();
                    // literal: true - This attribute value is a literal value
                    cw.WriteBooleanLiteral(true);
                    cw.WriteEndMethodInvoke();

                    // In VB, we need a line continuation
                    cw.WriteLineContinuation();
                }
            }));
            if (ValueGenerator != null)
            {
                ValueGenerator.Value.GenerateCode(target, context);
                context.FlushBufferedStatement();
                context.ExpressionRenderingMode = oldMode;
                context.AddStatement(context.BuildCodeString(cw =>
                {
                    cw.WriteParameterSeparator();
                    cw.WriteSnippet(ValueGenerator.Location.AbsoluteIndex.ToString(CultureInfo.CurrentCulture));
                    cw.WriteEndMethodInvoke();
                    cw.WriteParameterSeparator();
                    // literal: false - This attribute value is not a literal value, it is dynamically generated
                    cw.WriteBooleanLiteral(false);
                    cw.WriteEndMethodInvoke();

                    // In VB, we need a line continuation
                    cw.WriteLineContinuation();
                }));
            }
            else
            {
                context.FlushBufferedStatement();
            }
        }

        public override string ToString()
        {
            if (ValueGenerator == null)
            {
                return String.Format(CultureInfo.CurrentCulture, "LitAttr:{0:F},{1:F}", Prefix, Value);
            }
            else
            {
                return String.Format(CultureInfo.CurrentCulture, "LitAttr:{0:F},<Sub:{1:F}>", Prefix, ValueGenerator);
            }
        }

        public override bool Equals(object obj)
        {
            LiteralAttributeCodeGenerator other = obj as LiteralAttributeCodeGenerator;
            return other != null &&
                   Equals(other.Prefix, Prefix) &&
                   Equals(other.Value, Value) &&
                   Equals(other.ValueGenerator, ValueGenerator);
        }

        public override int GetHashCode()
        {
            return HashCodeCombiner.Start()
                .Add(Prefix)
                .Add(Value)
                .Add(ValueGenerator)
                .CombinedHash;
        }
    }
}
