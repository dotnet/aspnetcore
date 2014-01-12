// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
namespace Microsoft.AspNet.Razor.Generator
{
    internal class VBCodeWriter : BaseCodeWriter
    {
        public override bool SupportsMidStatementLinePragmas
        {
            get { return false; }
        }

        protected internal override void WriteStartGenerics()
        {
            InnerWriter.Write("(Of ");
        }

        protected internal override void WriteEndGenerics()
        {
            InnerWriter.Write(")");
        }

        public override void WriteLineContinuation()
        {
            InnerWriter.Write(" _");
        }

        public override int WriteVariableDeclaration(string type, string name, string value)
        {
            InnerWriter.Write("Dim ");
            InnerWriter.Write(name);
            InnerWriter.Write(" As ");
            int typePos = InnerWriter.GetStringBuilder().Length;
            InnerWriter.Write(type);
            if (!String.IsNullOrEmpty(value))
            {
                InnerWriter.Write(" = ");
                InnerWriter.Write(value);
            }
            else
            {
                InnerWriter.Write(" = Nothing");
            }
            return typePos;
        }

        public override void WriteStringLiteral(string literal)
        {
            bool inQuotes = true;
            InnerWriter.Write("\"");
            for (int i = 0; i < literal.Length; i++)
            {
                switch (literal[i])
                {
                    case '\t':
                    case '\n':
                    case '\r':
                    case '\0':
                    case '\u2028':
                    case '\u2029':
                        // Exit quotes
                        EnsureOutOfQuotes(ref inQuotes);

                        // Write concat character
                        InnerWriter.Write("&");

                        // Write character literal
                        WriteCharLiteral(literal[i]);
                        break;
                    case '"':
                    case '“':
                    case '”':
                    case (char)0xff02:
                        EnsureInQuotes(ref inQuotes);
                        InnerWriter.Write(literal[i]);
                        InnerWriter.Write(literal[i]);
                        break;
                    default:
                        EnsureInQuotes(ref inQuotes);
                        InnerWriter.Write(literal[i]);
                        break;
                }
                if (i > 0 && (i % 80) == 0)
                {
                    if ((Char.IsHighSurrogate(literal[i]) && (i < (literal.Length - 1))) && Char.IsLowSurrogate(literal[i + 1]))
                    {
                        InnerWriter.Write(literal[++i]);
                    }
                    if (inQuotes)
                    {
                        InnerWriter.Write("\"");
                    }
                    inQuotes = true;
                    InnerWriter.Write("& _ ");
                    InnerWriter.Write(Environment.NewLine);
                    InnerWriter.Write('"');
                }
            }
            EnsureOutOfQuotes(ref inQuotes);
        }

        protected internal override void EmitStartLambdaExpression(string[] parameterNames)
        {
            InnerWriter.Write("Function (");
            WriteCommaSeparatedList(parameterNames, InnerWriter.Write);
            InnerWriter.Write(") ");
        }

        protected internal override void EmitStartConstructor(string typeName)
        {
            InnerWriter.Write("New ");
            InnerWriter.Write(typeName);
            InnerWriter.Write("(");
        }

        protected internal override void EmitStartLambdaDelegate(string[] parameterNames)
        {
            InnerWriter.Write("Sub (");
            WriteCommaSeparatedList(parameterNames, InnerWriter.Write);
            InnerWriter.WriteLine(")");
        }

        protected internal override void EmitEndLambdaDelegate()
        {
            InnerWriter.Write("End Sub");
        }

        private void WriteCharLiteral(char literal)
        {
            InnerWriter.Write("Global.Microsoft.VisualBasic.ChrW(");
            InnerWriter.Write((int)literal);
            InnerWriter.Write(")");
        }

        private void EnsureInQuotes(ref bool inQuotes)
        {
            if (!inQuotes)
            {
                InnerWriter.Write("&\"");
                inQuotes = true;
            }
        }

        private void EnsureOutOfQuotes(ref bool inQuotes)
        {
            if (inQuotes)
            {
                InnerWriter.Write("\"");
                inQuotes = false;
            }
        }

        public override void WriteReturn()
        {
            InnerWriter.Write("Return ");
        }

        public override void WriteLinePragma(int? lineNumber, string fileName)
        {
            InnerWriter.WriteLine();
            if (lineNumber != null)
            {
                InnerWriter.Write("#ExternalSource(\"");
                InnerWriter.Write(fileName);
                InnerWriter.Write("\", ");
                InnerWriter.Write(lineNumber);
                InnerWriter.WriteLine(")");
            }
            else
            {
                InnerWriter.WriteLine("#End ExternalSource");
            }
        }

        public override void WriteHelperHeaderPrefix(string templateTypeName, bool isStatic)
        {
            InnerWriter.Write("Public ");
            if (isStatic)
            {
                InnerWriter.Write("Shared ");
            }
            InnerWriter.Write("Function ");
        }

        public override void WriteHelperHeaderSuffix(string templateTypeName)
        {
            InnerWriter.Write(" As ");
            InnerWriter.WriteLine(templateTypeName);
        }

        public override void WriteHelperTrailer()
        {
            InnerWriter.WriteLine("End Function");
        }

        public override void WriteEndStatement()
        {
            InnerWriter.WriteLine();
        }
    }
}
