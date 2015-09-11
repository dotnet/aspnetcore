// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Razor.TagHelpers;

namespace Microsoft.AspNet.Razor.CodeGenerators
{
    /// <summary>
    /// Renders code for tag helper property initialization.
    /// </summary>
    public class TagHelperAttributeValueCodeRenderer
    {
        /// <summary>
        /// Called during Razor's code generation process to generate code that instantiates the value of the tag
        /// helper's property. Last value written should not be or end with a semicolon.
        /// </summary>
        /// <param name="attributeDescriptor">
        /// The <see cref="TagHelperAttributeDescriptor"/> to generate code for.
        /// </param>
        /// <param name="writer">The <see cref="CSharpCodeWriter"/> that's used to write code.</param>
        /// <param name="context">A <see cref="Chunks.Generators.ChunkGeneratorContext"/> instance that contains
        /// information about the current code generation process.</param>
        /// <param name="renderAttributeValue">
        /// <see cref="Action"/> that renders the raw value of the HTML attribute.
        /// </param>
        /// <param name="complexValue">
        /// Indicates whether or not the source attribute value contains more than simple text. <c>false</c> for plain
        /// C# expressions e.g. <c>"PropertyName"</c>. <c>true</c> if the attribute value contain at least one in-line
        /// Razor construct e.g. <c>"@(@readonly)"</c>.
        /// </param>
        public virtual void RenderAttributeValue(
            TagHelperAttributeDescriptor attributeDescriptor,
            CSharpCodeWriter writer,
            CodeGeneratorContext context,
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

            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (renderAttributeValue == null)
            {
                throw new ArgumentNullException(nameof(renderAttributeValue));
            }

            renderAttributeValue(writer);
        }
    }
}