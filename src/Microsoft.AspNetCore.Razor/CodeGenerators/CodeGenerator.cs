// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Razor.CodeGenerators
{
    public abstract class CodeGenerator
    {
        private readonly CodeGeneratorContext _context;

        public CodeGenerator(CodeGeneratorContext context)
        {
            _context = context;
        }

        protected CodeGeneratorContext Context
        {
            get { return _context; }
        }

        public abstract CodeGeneratorResult Generate();
    }
}
