// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.CodeGeneration
{
    public abstract class CodeRenderingContext
    {
        internal static readonly object NewLineString = "NewLineString";
        internal static readonly object SuppressUniqueIds = "SuppressUniqueIds";

        public abstract CodeWriter CodeWriter { get; }

        public abstract RazorDiagnosticCollection Diagnostics { get; }

        public abstract string DocumentKind { get; }

        public abstract ItemCollection Items { get; }
        
        public abstract IEnumerable<IntermediateNode> Ancestors { get; }

        public abstract IntermediateNodeWriter NodeWriter { get; }

        public abstract RazorCodeGenerationOptions Options { get; }

        public abstract IntermediateNode Parent { get; }

        public abstract RazorSourceDocument SourceDocument { get; }

        public abstract Scope CreateScope();

        public abstract Scope CreateScope(IntermediateNodeWriter writer);

        public abstract void EndScope();

        public abstract void RenderNode(IntermediateNode node);

        public abstract void RenderChildren(IntermediateNode node);

        public abstract void AddLineMappingFor(IntermediateNode node);

        public struct Scope : IDisposable
        {
            private readonly CodeRenderingContext _context;

            public Scope(CodeRenderingContext context)
            {
                if (context == null)
                {
                    throw new ArgumentNullException(nameof(context));
                }

                _context = context;
            }

            public void Dispose()
            {
                _context.EndScope();
            }
        }
    }
}
