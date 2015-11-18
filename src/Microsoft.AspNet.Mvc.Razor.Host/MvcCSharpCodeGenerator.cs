// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
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

        protected override CSharpCodeWritingScope BuildClassDeclaration(CSharpCodeWriter writer)
        {
            if (Context.Host.DesignTimeMode &&
                string.Equals(
                    Path.GetFileName(Context.SourceFile),
                    ViewHierarchyUtility.ViewImportsFileName,
                    StringComparison.OrdinalIgnoreCase))
            {
                // Write a using TModel = System.Object; token during design time to make intellisense work
                writer.WriteLine($"using {ChunkHelper.TModelToken} = {typeof(object).FullName};");
            }

            return base.BuildClassDeclaration(writer);
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

        protected override CSharpDesignTimeCodeVisitor CreateCSharpDesignTimeCodeVisitor(
            CSharpCodeVisitor csharpCodeVisitor,
            CSharpCodeWriter writer,
            CodeGeneratorContext context)
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

            return new MvcCSharpDesignTimeCodeVisitor(csharpCodeVisitor, writer, context);
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
            injectVisitor.Accept(Context.ChunkTreeBuilder.Root.Children);

            writer.WriteLine();
            writer.WriteLineHiddenDirective();
        }
    }
}