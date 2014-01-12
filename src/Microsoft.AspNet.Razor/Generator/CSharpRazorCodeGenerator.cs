// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.CodeDom;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNet.Razor.Generator
{
    public class CSharpRazorCodeGenerator : RazorCodeGenerator
    {
        private const string HiddenLinePragma = "#line hidden";

        public CSharpRazorCodeGenerator(string className, string rootNamespaceName, string sourceFileName, RazorEngineHost host)
            : base(className, rootNamespaceName, sourceFileName, host)
        {
        }

        internal override Func<CodeWriter> CodeWriterFactory
        {
            get { return () => new CSharpCodeWriter(); }
        }

        [SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "System.CodeDom.CodeSnippetTypeMember.#ctor(System.String)", Justification = "Value is never to be localized")]
        protected override void Initialize(CodeGeneratorContext context)
        {
            base.Initialize(context);

            context.GeneratedClass.Members.Insert(0, new CodeSnippetTypeMember(HiddenLinePragma));
        }
    }
}
