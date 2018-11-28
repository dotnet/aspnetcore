// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Components.Razor
{
    internal class ComponentExtensionNode : ExtensionIntermediateNode
    {
        public IEnumerable<ComponentAttributeExtensionNode> Attributes => Children.OfType<ComponentAttributeExtensionNode>();

        public IEnumerable<RefExtensionNode> Captures => Children.OfType<RefExtensionNode>();

        public IEnumerable<ComponentChildContentIntermediateNode> ChildContents => Children.OfType<ComponentChildContentIntermediateNode>();

        public override IntermediateNodeCollection Children { get; } = new IntermediateNodeCollection();

        public TagHelperDescriptor Component { get; set; }

        /// <summary>
        /// Gets the child content parameter name (null if unset) that was applied at the component level.
        /// </summary>
        public string ChildContentParameterName { get; set; }

        public IEnumerable<ComponentTypeArgumentExtensionNode> TypeArguments => Children.OfType<ComponentTypeArgumentExtensionNode>();

        public string TagName { get; set; }
        
        // An optional type inference node. This will be populated (and point to a different part of the tree)
        // if this component call site requires type inference.
        public ComponentTypeInferenceMethodIntermediateNode TypeInferenceNode { get; set; }
        
        public string TypeName { get; set; }

        public override void Accept(IntermediateNodeVisitor visitor)
        {
            if (visitor == null)
            {
                throw new ArgumentNullException(nameof(visitor));
            }

            AcceptExtensionNode<ComponentExtensionNode>(this, visitor);
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
            writer.WriteComponent(context, this);
        }

        private string DebuggerDisplay
        {
            get
            {
                var builder = new StringBuilder();
                builder.Append("Component: ");
                builder.Append("<");
                builder.Append(TagName);

                foreach (var attribute in Attributes)
                {
                    builder.Append(" ");
                    builder.Append(attribute.AttributeName);
                    builder.Append("=\"...\"");
                }

                foreach (var capture in Captures)
                {
                    builder.Append(" ");
                    builder.Append("ref");
                    builder.Append("=\"...\"");
                }

                foreach (var typeArgument in TypeArguments)
                {
                    builder.Append(" ");
                    builder.Append(typeArgument.TypeParameterName);
                    builder.Append("=\"...\"");
                }

                builder.Append(">");
                builder.Append(ChildContents.Any() ? "..." : string.Empty);
                builder.Append("</");
                builder.Append(TagName);
                builder.Append(">");

                return builder.ToString();
            }
        }
    }
}
