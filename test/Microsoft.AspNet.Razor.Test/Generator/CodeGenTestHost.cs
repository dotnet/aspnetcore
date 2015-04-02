// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Razor.Generator;
using Microsoft.AspNet.Razor.Generator.Compiler;

namespace Microsoft.AspNet.Razor.Test.Generator
{
    public class CodeGenTestHost : RazorEngineHost
    {
        public CodeGenTestHost(RazorCodeLanguage language)
                : base(language)
        {
        }

        public override CodeBuilder DecorateCodeBuilder(CodeBuilder incomingBuilder, CodeBuilderContext context)
        {
            if (incomingBuilder is CodeGenTestCodeBuilder)
            {
                return incomingBuilder;
            }
            else
            {
                return new CodeGenTestCodeBuilder(context);
            }
        }
    }
}