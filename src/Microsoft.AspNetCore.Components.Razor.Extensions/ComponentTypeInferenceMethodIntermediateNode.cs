// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Components.Razor
{
    /// <summary>
    /// Represents a type-inference thunk that is used by the generated component code.
    /// </summary>
    internal class ComponentTypeInferenceMethodIntermediateNode : ExtensionIntermediateNode
    {
        public Dictionary<string, GenericTypeNameRewriter.Binding> Bindings { get; set; }

        public override IntermediateNodeCollection Children => IntermediateNodeCollection.ReadOnly;
        
        /// <summary>
        /// Gets the component usage linked to this type inference method.
        /// </summary>
        public ComponentExtensionNode Component { get; set; }
        
        /// <summary>
        /// Gets the full type name of the generated class containing this method.
        /// </summary>
        public string FullTypeName { get; internal set; }

        /// <summary>
        /// Gets the name of the generated method.
        /// </summary>
        public string MethodName { get; set; }

        public override void Accept(IntermediateNodeVisitor visitor)
        {
            if (visitor == null)
            {
                throw new ArgumentNullException(nameof(visitor));
            }

            AcceptExtensionNode<ComponentTypeInferenceMethodIntermediateNode>(this, visitor);
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
            writer.WriteComponentTypeInferenceMethod(context, this);
        }
    }
}
