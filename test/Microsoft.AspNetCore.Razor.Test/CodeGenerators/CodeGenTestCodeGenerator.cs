// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.CodeGenerators;

namespace Microsoft.AspNetCore.Razor.Test.CodeGenerators
{
    public class CodeGenTestCodeGenerator : CSharpCodeGenerator
    {
        public CodeGenTestCodeGenerator(CodeGeneratorContext context)
            : base(context)
        {
        }

        protected override CSharpCodeWriter CreateCodeWriter()
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