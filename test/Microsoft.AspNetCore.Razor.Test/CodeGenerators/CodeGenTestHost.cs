// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.CodeGenerators;

namespace Microsoft.AspNetCore.Razor.Test.CodeGenerators
{
    public class CodeGenTestHost : RazorEngineHost
    {
        public CodeGenTestHost(RazorCodeLanguage language)
                : base(language)
        {
        }

        public override CodeGenerator DecorateCodeGenerator(CodeGenerator incomingBuilder, CodeGeneratorContext context)
        {
            if (incomingBuilder is CodeGenTestCodeGenerator)
            {
                return incomingBuilder;
            }
            else
            {
                return new CodeGenTestCodeGenerator(context);
            }
        }
    }
}