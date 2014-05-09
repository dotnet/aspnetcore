// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Razor.Generator.Compiler
{
    public abstract class CodeBuilder
    {
        private readonly CodeGeneratorContext _context;

        public CodeBuilder(CodeGeneratorContext context)
        {
            _context = context;
        }

        protected CodeGeneratorContext Context
        {
            get { return _context; }
        }

        public abstract CodeBuilderResult Build();
    }
}
