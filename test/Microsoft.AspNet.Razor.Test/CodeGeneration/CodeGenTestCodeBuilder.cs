// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Razor.Chunks.Generators;
using Microsoft.AspNet.Razor.CodeGeneration;

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
                // We normalize newlines so no matter what platform we're on they're consistent (for code generation
                // tests).
                NewLine = "\r\n";
            }
        }
    }
}