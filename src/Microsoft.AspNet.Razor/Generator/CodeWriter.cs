// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.CodeDom;
using System.Globalization;
using System.IO;

namespace Microsoft.AspNet.Razor.Generator
{
    // Utility class which helps write code snippets
    internal abstract class CodeWriter : IDisposable
    {
        private StringWriter _writer;

        protected CodeWriter()
        {
        }

        private enum WriterMode
        {
            Constructor,
            MethodCall,
            LambdaDelegate,
            LambdaExpression
        }

        public string Content
        {
            get { return InnerWriter.ToString(); }
        }

        public StringWriter InnerWriter
        {
            get
            {
                if (_writer == null)
                {
                    _writer = new StringWriter(CultureInfo.InvariantCulture);
                }
                return _writer;
            }
        }

        public virtual bool SupportsMidStatementLinePragmas
        {
            get { return true; }
        }

        public abstract void WriteParameterSeparator();
        public abstract void WriteReturn();
        public abstract void WriteLinePragma(int? lineNumber, string fileName);
        public abstract void WriteHelperHeaderPrefix(string templateTypeName, bool isStatic);
        public abstract void WriteSnippet(string snippet);
        public abstract void WriteStringLiteral(string literal);
        public abstract int WriteVariableDeclaration(string type, string name, string value);
#if NET45
        // No CodeDOM in CoreCLR

        public virtual void WriteLinePragma()
        {
            WriteLinePragma(null);
        }

        public virtual void WriteLinePragma(CodeLinePragma pragma)
        {
            if (pragma == null)
            {
                WriteLinePragma(null, null);
            }
            else
            {
                WriteLinePragma(pragma.LineNumber, pragma.FileName);
            }
        }

        public CodeSnippetStatement ToStatement()
        {
            return new CodeSnippetStatement(Content);
        }

        public CodeSnippetTypeMember ToTypeMember()
        {
            return new CodeSnippetTypeMember(Content);
        }
#endif

        public virtual void WriteHiddenLinePragma()
        {
        }

        public virtual void WriteDisableUnusedFieldWarningPragma()
        {
        }

        public virtual void WriteRestoreUnusedFieldWarningPragma()
        {
        }

        public virtual void WriteIdentifier(string identifier)
        {
            InnerWriter.Write(identifier);
        }

        public virtual void WriteHelperHeaderSuffix(string templateTypeName)
        {
        }

        public virtual void WriteHelperTrailer()
        {
        }

        public void WriteStartMethodInvoke(string methodName)
        {
            EmitStartMethodInvoke(methodName);
        }

        public void WriteStartMethodInvoke(string methodName, params string[] genericArguments)
        {
            EmitStartMethodInvoke(methodName, genericArguments);
        }

        public void WriteEndMethodInvoke()
        {
            EmitEndMethodInvoke();
        }

        public virtual void WriteEndStatement()
        {
        }

        public virtual void WriteStartAssignment(string variableName)
        {
            InnerWriter.Write(variableName);
            InnerWriter.Write(" = ");
        }

        public void WriteStartLambdaExpression(params string[] parameterNames)
        {
            EmitStartLambdaExpression(parameterNames);
        }

        public void WriteStartConstructor(string typeName)
        {
            EmitStartConstructor(typeName);
        }

        public void WriteStartLambdaDelegate(params string[] parameterNames)
        {
            EmitStartLambdaDelegate(parameterNames);
        }

        public void WriteEndLambdaExpression()
        {
            EmitEndLambdaExpression();
        }

        public void WriteEndConstructor()
        {
            EmitEndConstructor();
        }

        public void WriteEndLambdaDelegate()
        {
            EmitEndLambdaDelegate();
        }

        public virtual void WriteLineContinuation()
        {
        }

        public virtual void WriteBooleanLiteral(bool value)
        {
#if NET45
            // ToString does not take a parameter in CoreCLR
            // #if'd the entire section because once we transition over to the CodeTree we will not need all this code.

            WriteSnippet(value.ToString(CultureInfo.InvariantCulture));
#endif
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Clear()
        {
            if (InnerWriter != null)
            {
                InnerWriter.GetStringBuilder().Clear();
            }
        }

        protected internal abstract void EmitStartLambdaDelegate(string[] parameterNames);
        protected internal abstract void EmitStartLambdaExpression(string[] parameterNames);
        protected internal abstract void EmitStartConstructor(string typeName);
        protected internal abstract void EmitStartMethodInvoke(string methodName);

        protected internal virtual void EmitStartMethodInvoke(string methodName, params string[] genericArguments)
        {
            EmitStartMethodInvoke(methodName);
        }

        protected internal abstract void EmitEndLambdaDelegate();
        protected internal abstract void EmitEndLambdaExpression();
        protected internal abstract void EmitEndConstructor();
        protected internal abstract void EmitEndMethodInvoke();

        protected virtual void Dispose(bool disposing)
        {
            if (disposing && _writer != null)
            {
                _writer.Dispose();
            }
        }
    }
}
