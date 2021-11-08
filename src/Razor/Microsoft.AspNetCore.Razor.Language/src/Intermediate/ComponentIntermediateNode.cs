// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate;

public sealed class ComponentIntermediateNode : IntermediateNode
{
    public IEnumerable<ComponentAttributeIntermediateNode> Attributes => Children.OfType<ComponentAttributeIntermediateNode>();

    public IEnumerable<ReferenceCaptureIntermediateNode> Captures => Children.OfType<ReferenceCaptureIntermediateNode>();

    public IEnumerable<SetKeyIntermediateNode> SetKeys => Children.OfType<SetKeyIntermediateNode>();

    public IEnumerable<SplatIntermediateNode> Splats => Children.OfType<SplatIntermediateNode>();

    public IEnumerable<ComponentChildContentIntermediateNode> ChildContents => Children.OfType<ComponentChildContentIntermediateNode>();

    public override IntermediateNodeCollection Children { get; } = new IntermediateNodeCollection();

    public TagHelperDescriptor Component { get; set; }

    /// <summary>
    /// Gets the child content parameter name (null if unset) that was applied at the component level.
    /// </summary>
    public string ChildContentParameterName { get; set; }

    public IEnumerable<ComponentTypeArgumentIntermediateNode> TypeArguments => Children.OfType<ComponentTypeArgumentIntermediateNode>();

    public string TagName { get; set; }

    // An optional type inference node. This will be populated (and point to a different part of the tree)
    // if this component call site requires type inference.
    public ComponentTypeInferenceMethodIntermediateNode TypeInferenceNode { get; set; }

    /// <summary>
    /// Gets a dictionary (or null) that advertises any type arguments that are available
    /// for use by descendants within the same tree.
    /// </summary>
    public Dictionary<string, CascadingGenericTypeParameter> ProvidesCascadingGenericTypes { get; set; }

    public string TypeName { get; set; }

    public override void Accept(IntermediateNodeVisitor visitor)
    {
        if (visitor == null)
        {
            throw new ArgumentNullException(nameof(visitor));
        }

        visitor.VisitComponent(this);
    }

    public override void FormatNode(IntermediateNodeFormatter formatter)
    {
        formatter.WriteContent(TagName);

        formatter.WriteProperty(nameof(Component), Component?.DisplayName);
        formatter.WriteProperty(nameof(TagName), TagName);
    }
}
