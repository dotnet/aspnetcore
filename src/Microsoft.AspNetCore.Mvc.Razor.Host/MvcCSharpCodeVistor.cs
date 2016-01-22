// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.CodeGenerators;

namespace Microsoft.AspNetCore.Mvc.Razor
{
    public abstract class MvcCSharpCodeVisitor : MvcCSharpChunkVisitor
    {
        public MvcCSharpCodeVisitor(
            CSharpCodeWriter writer,
            CodeGeneratorContext context)
            : base(writer, context)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
        }

        protected override void Visit(InjectChunk chunk)
        {
        }
    }
}