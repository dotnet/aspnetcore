// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Razor.CodeGenerators;
using Microsoft.AspNet.Razor.CodeGenerators.Visitors;

namespace Microsoft.AspNet.Mvc.Razor
{
    public class MvcCSharpCodeGenerator : CSharpCodeGenerator
    {
        private readonly GeneratedTagHelperAttributeContext _tagHelperAttributeContext;
        private readonly string _defaultModel;
        private readonly string _injectAttribute;

        public MvcCSharpCodeGenerator(
            CodeGeneratorContext context,
            string defaultModel,
            string injectAttribute,
            GeneratedTagHelperAttributeContext tagHelperAttributeContext)
            : base(context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (defaultModel == null)
            {
                throw new ArgumentNullException(nameof(defaultModel));
            }

            if (injectAttribute == null)
            {
                throw new ArgumentNullException(nameof(injectAttribute));
            }

            if (tagHelperAttributeContext == null)
            {
                throw new ArgumentNullException(nameof(tagHelperAttributeContext));
            }

            _tagHelperAttributeContext = tagHelperAttributeContext;
            _defaultModel = defaultModel;
            _injectAttribute = injectAttribute;
        }

        protected override CSharpCodeVisitor CreateCSharpCodeVisitor(
            CSharpCodeWriter writer,
            CodeGeneratorContext context)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var csharpCodeVisitor = base.CreateCSharpCodeVisitor(writer, context);

            csharpCodeVisitor.TagHelperRenderer.AttributeValueCodeRenderer =
                new MvcTagHelperAttributeValueCodeRenderer(_tagHelperAttributeContext);

            return csharpCodeVisitor;
        }

        protected override void BuildConstructor(CSharpCodeWriter writer)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            base.BuildConstructor(writer);

            writer.WriteLineHiddenDirective();

            var injectVisitor = new InjectChunkVisitor(writer, Context, _injectAttribute);
            injectVisitor.Accept(Context.ChunkTreeBuilder.ChunkTree.Chunks);

            writer.WriteLine();
            writer.WriteLineHiddenDirective();
        }
    }
}