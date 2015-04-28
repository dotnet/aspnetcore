// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Razor.Generator.Compiler.CSharp
{
    public class CSharpTypeMemberVisitor : CodeVisitor<CSharpCodeWriter>
    {
        private CSharpCodeVisitor _csharpCodeVisitor;

        public CSharpTypeMemberVisitor([NotNull] CSharpCodeVisitor csharpCodeVisitor,
                                       [NotNull] CSharpCodeWriter writer,
                                       [NotNull] CodeBuilderContext context)
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
