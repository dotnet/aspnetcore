// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.Extensions
{
    /// <summary>
    /// An <see cref="ExtensionIntermediateNode"/> that generates code for <c>RazorCompiledItemMetadataAttribute</c>.
    /// </summary>
    public class RazorCompiledItemMetadataAttributeIntermediateNode : ExtensionIntermediateNode
    {
        public override IntermediateNodeCollection Children => IntermediateNodeCollection.ReadOnly;

        /// <summary>
        /// Gets or sets the attribute key.
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// Gets or sets the attribute value.
        /// </summary>
        public string Value { get; set; }

        public override void Accept(IntermediateNodeVisitor visitor)
        {
            if (visitor == null)
            {
                throw new ArgumentNullException(nameof(visitor));
            }

            AcceptExtensionNode(this, visitor);
        }

        public override void WriteNode(CodeTarget target, CodeRenderingContext context)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var extension = target.GetExtension<IMetadataAttributeTargetExtension>();
            if (extension == null)
            {
                ReportMissingCodeTargetExtension<IMetadataAttributeTargetExtension>(context);
                return;
            }

            extension.WriteRazorCompiledItemMetadataAttribute(context, this);
        }

        public override void FormatNode(IntermediateNodeFormatter formatter)
        {
            formatter.WriteProperty(nameof(Key), Key);
            formatter.WriteProperty(nameof(Value), Value);
        }
    }
}
