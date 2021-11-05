// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate;

/// <summary>
/// Represents a type-inference thunk that is used by the generated component code.
/// </summary>
public sealed class ComponentTypeInferenceMethodIntermediateNode : IntermediateNode
{
    public override IntermediateNodeCollection Children => IntermediateNodeCollection.ReadOnly;

    /// <summary>
    /// Gets the component usage linked to this type inference method.
    /// </summary>
    public ComponentIntermediateNode Component { get; set; }

    /// <summary>
    /// Gets the full type name of the generated class containing this method.
    /// </summary>
    public string FullTypeName { get; internal set; }

    /// <summary>
    /// Gets the name of the generated method.
    /// </summary>
    public string MethodName { get; set; }

    /// <summary>
    /// Gets a list (or null) describing additional arguments for type inference.
    /// These are populated from ancestor components that choose to cascade their type parameters.
    /// </summary>
    public List<CascadingGenericTypeParameter> ReceivesCascadingGenericTypes { get; set; }

    /// <summary>
    /// Gets a list describing the type and property constraints that are
    /// set for generic types on the methods.
    /// <example>
    /// ["where T: Foo, new()", "where U: Image", "where Z: notnull"]
    /// </example>
    /// </summary>
    public IEnumerable<string> GenericTypeConstraints { get; set; }

    public override void Accept(IntermediateNodeVisitor visitor)
    {
        if (visitor == null)
        {
            throw new ArgumentNullException(nameof(visitor));
        }

        visitor.VisitComponentTypeInferenceMethod(this);
    }

    public override void FormatNode(IntermediateNodeFormatter formatter)
    {
        if (formatter == null)
        {
            throw new ArgumentNullException(nameof(formatter));
        }

        formatter.WriteContent(Component?.TagName);

        formatter.WriteProperty(nameof(Component), Component?.Component?.DisplayName);
        formatter.WriteProperty(nameof(FullTypeName), FullTypeName);
        formatter.WriteProperty(nameof(MethodName), MethodName);
    }
}
