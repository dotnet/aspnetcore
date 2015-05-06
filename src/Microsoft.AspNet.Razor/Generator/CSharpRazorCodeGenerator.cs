// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Razor.Generator
{
    public class CSharpRazorCodeGenerator : RazorCodeGenerator
    {
        public CSharpRazorCodeGenerator(
            string className,
            [NotNull] string rootNamespaceName,
            string sourceFileName,
            [NotNull] RazorEngineHost host)
            : base(className, rootNamespaceName, sourceFileName, host)
        {
        }

        protected override void Initialize(CodeGeneratorContext context)
        {
            base.Initialize(context);
        }
    }
}
