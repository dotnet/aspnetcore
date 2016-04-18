// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.CodeGenerators;
using Microsoft.AspNetCore.Razor.Compilation.TagHelpers;

namespace Microsoft.AspNetCore.Mvc.Razor
{
    /// <inheritdoc />
    public class MvcTagHelperAttributeValueCodeRenderer : TagHelperAttributeValueCodeRenderer
    {
        private const string ModelLambdaVariableName = "__model";

        private readonly GeneratedTagHelperAttributeContext _context;

        /// <summary>
        /// Instantiates a new instance of <see cref="MvcTagHelperAttributeValueCodeRenderer"/>.
        /// </summary>
        /// <param name="context">Contains code generation information for rendering attribute values.</param>
        public MvcTagHelperAttributeValueCodeRenderer(GeneratedTagHelperAttributeContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            _context = context;
        }

        /// <inheritdoc />
        /// <remarks>If the attribute being rendered is of the type
        /// <see cref="GeneratedTagHelperAttributeContext.ModelExpressionTypeName"/>, then a model expression will be
        /// created by calling into <see cref="GeneratedTagHelperAttributeContext.CreateModelExpressionMethodName"/>.
        /// </remarks>
        public override void RenderAttributeValue(
            TagHelperAttributeDescriptor attributeDescriptor,
            CSharpCodeWriter writer,
            CodeGeneratorContext codeGeneratorContext,
            Action<CSharpCodeWriter> renderAttributeValue,
            bool complexValue)
        {
            if (attributeDescriptor == null)
            {
                throw new ArgumentNullException(nameof(attributeDescriptor));
            }

            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            if (codeGeneratorContext == null)
            {
                throw new ArgumentNullException(nameof(codeGeneratorContext));
            }

            if (renderAttributeValue == null)
            {
                throw new ArgumentNullException(nameof(renderAttributeValue));
            }

            if (attributeDescriptor.TypeName.Equals(_context.ModelExpressionTypeName, StringComparison.Ordinal))
            {
                writer
                    .WriteStartInstanceMethodInvocation(_context.ModelExpressionProviderPropertyName, _context.CreateModelExpressionMethodName)
                    .Write(_context.ViewDataPropertyName)
                    .WriteParameterSeparator()
                    .Write(ModelLambdaVariableName)
                    .Write(" => ");
                if (!complexValue)
                {
                    writer
                        .Write(ModelLambdaVariableName)
                        .Write(".");

                }

                renderAttributeValue(writer);

                writer.WriteEndMethodInvocation(endLine: false);
            }
            else
            {
                base.RenderAttributeValue(
                    attributeDescriptor,
                    writer,
                    codeGeneratorContext,
                    renderAttributeValue,
                    complexValue);
            }
        }
    }
}