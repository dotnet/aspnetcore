// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Razor.Chunks;

namespace Microsoft.AspNet.Razor.CodeGenerators.Visitors
{
    public class CSharpTypeMemberVisitor : CodeVisitor<CSharpCodeWriter>
    {
        private CSharpCodeVisitor _csharpCodeVisitor;

        public CSharpTypeMemberVisitor(CSharpCodeVisitor csharpCodeVisitor,
                                       CSharpCodeWriter writer,
                                       CodeGeneratorContext context)
            : base(writer, context)
        {
            if (csharpCodeVisitor == null)
            {
                throw new ArgumentNullException(nameof(csharpCodeVisitor));
            }

            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

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
