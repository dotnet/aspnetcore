// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.


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
