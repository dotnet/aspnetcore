// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.CodeGeneration
{
    public static class TestCodeRenderingContext
    {
        public static CodeRenderingContext CreateDesignTime(
            string newLineString = null,
            string suppressUniqueIds = "test",
            RazorSourceDocument source = null,
            IntermediateNodeWriter nodeWriter = null)
        {
            var codeWriter = new CodeWriter();
            var documentNode = new DocumentIntermediateNode();
            var options = RazorCodeGenerationOptions.CreateDesignTimeDefault();

            if (source == null)
            {
                source = TestRazorSourceDocument.Create(); 
            }

            var codeDocument = RazorCodeDocument.Create(source);
            if (newLineString != null)
            {
                codeDocument.Items[CodeRenderingContext.NewLineString] = newLineString;
            }

            if (suppressUniqueIds != null)
            {
                codeDocument.Items[CodeRenderingContext.SuppressUniqueIds] = suppressUniqueIds;
            }

            if (nodeWriter == null)
            {
                nodeWriter = new DesignTimeNodeWriter();
            }

            var context = new DefaultCodeRenderingContext(codeWriter, nodeWriter, codeDocument, documentNode, options);
            context.Visitor = new RenderChildrenVisitor(context);

            return context;
        }

        public static CodeRenderingContext CreateRuntime(
            string newLineString = null,
            string suppressUniqueIds = "test",
            RazorSourceDocument source = null,
            IntermediateNodeWriter nodeWriter = null)
        {
            var codeWriter = new CodeWriter();
            var documentNode = new DocumentIntermediateNode();
            var options = RazorCodeGenerationOptions.CreateDefault();

            if (source == null)
            {
                source = TestRazorSourceDocument.Create();
            }

            var codeDocument = RazorCodeDocument.Create(source);
            if (newLineString != null)
            {
                codeDocument.Items[CodeRenderingContext.NewLineString] = newLineString;
            }

            if (suppressUniqueIds != null)
            {
                codeDocument.Items[CodeRenderingContext.SuppressUniqueIds] = suppressUniqueIds;
            }

            if (nodeWriter == null)
            {
                nodeWriter = new RuntimeNodeWriter();
            }

            var context = new DefaultCodeRenderingContext(codeWriter, nodeWriter, codeDocument, documentNode, options);
            context.Visitor = new RenderChildrenVisitor(context);

            return context;
        }

        private class RenderChildrenVisitor : IntermediateNodeVisitor
        {
            private readonly CodeRenderingContext _context;
            public RenderChildrenVisitor(CodeRenderingContext context)
            {
                _context = context;
            }

            public override void VisitDefault(IntermediateNode node)
            {
                _context.CodeWriter.WriteLine("Render Children");
            }
        }

    }
}
