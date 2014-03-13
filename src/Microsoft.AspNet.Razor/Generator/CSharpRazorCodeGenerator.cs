// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNet.Razor.Generator
{
    public class CSharpRazorCodeGenerator : RazorCodeGenerator
    {
        public CSharpRazorCodeGenerator(string className, string rootNamespaceName, string sourceFileName, RazorEngineHost host)
            : base(className, rootNamespaceName, sourceFileName, host)
        {
        }

        protected override void Initialize(CodeGeneratorContext context)
        {
            base.Initialize(context);
        }
    }
}
