// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Evolution.Intermediate;

namespace Microsoft.AspNetCore.Razor.Evolution.CodeGeneration
{
    internal class DefaultDocumentWriter : DocumentWriter
    {
        private readonly CSharpRenderingContext _context;
        private readonly RuntimeTarget _target;

        private readonly PageStructureCSharpRenderer _renderer;

        public DefaultDocumentWriter(RuntimeTarget target, CSharpRenderingContext context, PageStructureCSharpRenderer renderer)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (renderer == null)
            {
                throw new ArgumentNullException(nameof(renderer));
            }

            _target = target;
            _context = context;
            _renderer = renderer;
        }

        public override void WriteDocument(DocumentIRNode node)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            var visitor = new Visitor(_target, _context, _renderer);
            _context.RenderChildren = visitor.RenderChildren;

            visitor.VisitDocument(node);
            _context.RenderChildren = null;
        }

        private class Visitor : RazorIRNodeVisitor
        {
            private readonly CSharpRenderingContext _context;
            private readonly RuntimeTarget _target;

            private readonly PageStructureCSharpRenderer _renderer;

            public Visitor(RuntimeTarget target, CSharpRenderingContext context, PageStructureCSharpRenderer renderer)
            {
                _target = target;
                _context = context;
                _renderer = renderer;
            }

            public void RenderChildren(RazorIRNode node)
            {
                for (var i = 0; i < node.Children.Count; i++)
                {
                    var child = node.Children[i];
                    Visit(child);
                }
            }

            public override void VisitDocument(DocumentIRNode node)
            {
                RenderChildren(node);
            }

            public override void VisitDefault(RazorIRNode node)
            {
                // This is a temporary bridge to the renderer, which allows us to move functionality piecemeal
                // into this class. 
                _renderer.Visit(node);
            }
        }
    }
}
