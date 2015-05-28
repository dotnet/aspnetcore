// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Razor.Chunks;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Razor.CodeGenerators.Visitors
{
    public class CSharpTypeMemberVisitor : CodeVisitor<CSharpCodeWriter>
    {
        private CSharpCodeVisitor _csharpCodeVisitor;

        public CSharpTypeMemberVisitor([NotNull] CSharpCodeVisitor csharpCodeVisitor,
                                       [NotNull] CSharpCodeWriter writer,
                                       [NotNull] CodeGeneratorContext context)
            : base(writer, context)
        {
            _csharpCodeVisitor = csharpCodeVisitor;
        }

        protected override void Visit(TypeMemberChunk chunk)
        {
            if (!string.IsNullOrEmpty(chunk.Code))
            {
                _csharpCodeVisitor.CreateCodeMapping(string.Empty, chunk.Code, chunk);
            }
        }
    }
}
