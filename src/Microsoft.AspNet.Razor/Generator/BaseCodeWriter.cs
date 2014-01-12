// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
namespace Microsoft.AspNet.Razor.Generator
{
    internal abstract class BaseCodeWriter : CodeWriter
    {
        public override void WriteSnippet(string snippet)
        {
            InnerWriter.Write(snippet);
        }

        protected internal override void EmitStartMethodInvoke(string methodName)
        {
            EmitStartMethodInvoke(methodName, new string[0]);
        }

        protected internal override void EmitStartMethodInvoke(string methodName, params string[] genericArguments)
        {
            InnerWriter.Write(methodName);
            if (genericArguments != null && genericArguments.Length > 0)
            {
                WriteStartGenerics();
                for (int i = 0; i < genericArguments.Length; i++)
                {
                    if (i > 0)
                    {
                        WriteParameterSeparator();
                    }
                    WriteSnippet(genericArguments[i]);
                }
                WriteEndGenerics();
            }

            InnerWriter.Write("(");
        }

        protected internal override void EmitEndMethodInvoke()
        {
            InnerWriter.Write(")");
        }

        protected internal override void EmitEndConstructor()
        {
            InnerWriter.Write(")");
        }

        protected internal override void EmitEndLambdaExpression()
        {
        }

        public override void WriteParameterSeparator()
        {
            InnerWriter.Write(", ");
        }

        protected internal void WriteCommaSeparatedList<T>(T[] items, Action<T> writeItemAction)
        {
            for (int i = 0; i < items.Length; i++)
            {
                if (i > 0)
                {
                    InnerWriter.Write(", ");
                }
                writeItemAction(items[i]);
            }
        }

        protected internal virtual void WriteStartGenerics()
        {
        }

        protected internal virtual void WriteEndGenerics()
        {
        }
    }
}
