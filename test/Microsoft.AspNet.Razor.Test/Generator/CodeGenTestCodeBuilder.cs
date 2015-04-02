// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Razor.Generator;
using Microsoft.AspNet.Razor.Generator.Compiler.CSharp;

namespace Microsoft.AspNet.Razor.Test.Generator
{
    public class CodeGenTestCodeBuilder : CSharpCodeBuilder
    {
        public CodeGenTestCodeBuilder(CodeBuilderContext context)
            : base(context)
        {
        }

        internal override CSharpCodeWriter CreateCodeWriter()
        {
            return new TestCodeWriter();
        }

        private class TestCodeWriter : CSharpCodeWriter
        {
            public TestCodeWriter()
            {
                // We normalize newlines so no matter what platform we're on they're consistent 
                // (for code generation tests).
                NewLine = "\r\n";
            }
        }
    }
}