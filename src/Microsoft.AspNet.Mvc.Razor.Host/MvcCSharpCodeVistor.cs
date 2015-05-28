// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Razor.CodeGenerators;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.Razor
{
    public abstract class MvcCSharpCodeVisitor : MvcCSharpChunkVisitor
    {
        public MvcCSharpCodeVisitor(
            [NotNull] CSharpCodeWriter writer,
            [NotNull] CodeGeneratorContext context)
            : base(writer, context)
        {
        }

        protected override void Visit(InjectChunk chunk)
        {
        }
        protected override void Visit(ModelChunk chunk)
        {
        }
    }
}