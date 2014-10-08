// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Razor.Generator;
using Microsoft.AspNet.Razor.Generator.Compiler.CSharp;
using Microsoft.AspNet.Razor.TagHelpers;

namespace Microsoft.AspNet.Mvc.Razor
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
        public MvcTagHelperAttributeValueCodeRenderer([NotNull] GeneratedTagHelperAttributeContext context)
        {
            _context = context;
        }

        /// <inheritdoc />
        /// <remarks>If the attribute being rendered is of the type 
        /// <see cref="GeneratedTagHelperAttributeContext.ModelExpressionTypeName"/> then a model expression will be
        /// created by calling into <see cref="GeneratedTagHelperAttributeContext.CreateModelExpressionMethodName"/>.
        /// </remarks>
        public override void RenderAttributeValue([NotNull] TagHelperAttributeDescriptor attributeDescriptor,
                                                  [NotNull] CSharpCodeWriter writer,
                                                  [NotNull] CodeBuilderContext codeBuilderContext,
                                                  [NotNull] Action<CSharpCodeWriter> renderAttributeValue)
        {
            var propertyType = attributeDescriptor.PropertyInfo.PropertyType;

            if (propertyType.FullName.Equals(_context.ModelExpressionTypeName, StringComparison.Ordinal))
            {
                writer.WriteStartMethodInvocation(_context.CreateModelExpressionMethodName)
                      .Write(ModelLambdaVariableName)
                      .Write(" => ")
                      .Write(ModelLambdaVariableName)
                      .Write(".");

                renderAttributeValue(writer);

                writer.WriteEndMethodInvocation(endLine: false);
            }
            else
            {
                base.RenderAttributeValue(attributeDescriptor, writer, codeBuilderContext, renderAttributeValue);
            }
        }
    }
}