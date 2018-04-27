// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using System;

namespace Microsoft.AspNetCore.Blazor.Razor
{
    internal class RefExtensionNode : ExtensionIntermediateNode
    {
        public override IntermediateNodeCollection Children => IntermediateNodeCollection.ReadOnly;

        public IntermediateToken IdentifierToken { get; }

        public bool IsComponentCapture { get; }

        public string ComponentCaptureTypeName { get; }

        public RefExtensionNode(IntermediateToken identifierToken)
        {
            IdentifierToken = identifierToken ?? throw new ArgumentNullException(nameof(identifierToken));
            Source = IdentifierToken.Source;
        }

        public RefExtensionNode(IntermediateToken identifierToken, string componentCaptureTypeName)
            : this(identifierToken)
        {
            if (string.IsNullOrEmpty(componentCaptureTypeName))
            {
                throw new ArgumentException("Cannot be null or empty", nameof(componentCaptureTypeName));
            }

            IsComponentCapture = true;
            ComponentCaptureTypeName = componentCaptureTypeName;
        }

        public override void Accept(IntermediateNodeVisitor visitor)
        {
            if (visitor == null)
            {
                throw new ArgumentNullException(nameof(visitor));
            }

            AcceptExtensionNode<RefExtensionNode>(this, visitor);
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

            var writer = (BlazorNodeWriter)context.NodeWriter;
            writer.WriteReferenceCapture(context, this);
        }
    }
}
