// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using Microsoft.AspNet.Mvc.Razor.Directives;
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

        private string Model { get; set; }

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

        protected override CSharpCodeWritingScope BuildClassDeclaration(CSharpCodeWriter writer)
        {
            // Grab the last model chunk so it gets intellisense.
            var modelChunk = ChunkHelper.GetModelChunk(Context.ChunkTreeBuilder.ChunkTree);

            Model = modelChunk != null ? modelChunk.ModelType : _defaultModel;

            // If there were any model chunks then we need to modify the class declaration signature.
            if (modelChunk != null)
            {
                writer.Write(string.Format(CultureInfo.InvariantCulture, "public class {0} : ", Context.ClassName));

                var modelVisitor = new ModelChunkVisitor(writer, Context);
                // This generates the base class signature
                modelVisitor.Accept(modelChunk);

                writer.WriteLine();

                return new CSharpCodeWritingScope(writer);
            }
            else
            {
                return base.BuildClassDeclaration(writer);
            }
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