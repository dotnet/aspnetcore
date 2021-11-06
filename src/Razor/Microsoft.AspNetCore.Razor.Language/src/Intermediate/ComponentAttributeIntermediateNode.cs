// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Razor.Language.Components;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate;

public sealed class ComponentAttributeIntermediateNode : IntermediateNode
{
    public ComponentAttributeIntermediateNode()
    {
    }

    public ComponentAttributeIntermediateNode(TagHelperHtmlAttributeIntermediateNode attributeNode)
    {
        if (attributeNode == null)
        {
            throw new ArgumentNullException(nameof(attributeNode));
        }

        AttributeName = attributeNode.AttributeName;
        AttributeStructure = attributeNode.AttributeStructure;
        Source = attributeNode.Source;

        for (var i = 0; i < attributeNode.Children.Count; i++)
        {
            Children.Add(attributeNode.Children[i]);
        }

        for (var i = 0; i < attributeNode.Diagnostics.Count; i++)
        {
            Diagnostics.Add(attributeNode.Diagnostics[i]);
        }
    }

    public ComponentAttributeIntermediateNode(TagHelperPropertyIntermediateNode propertyNode)
    {
        if (propertyNode == null)
        {
            throw new ArgumentNullException(nameof(propertyNode));
        }

        var attributeName = propertyNode.AttributeName;

        AttributeName = attributeName;
        AttributeStructure = propertyNode.AttributeStructure;
        BoundAttribute = propertyNode.BoundAttribute;
        PropertyName = propertyNode.BoundAttribute.GetPropertyName();
        Source = propertyNode.Source;
        TagHelper = propertyNode.TagHelper;
        TypeName = propertyNode.BoundAttribute.IsWeaklyTyped() ? null : propertyNode.BoundAttribute.TypeName;

        for (var i = 0; i < propertyNode.Children.Count; i++)
        {
            Children.Add(propertyNode.Children[i]);
        }

        for (var i = 0; i < propertyNode.Diagnostics.Count; i++)
        {
            Diagnostics.Add(propertyNode.Diagnostics[i]);
        }
    }

    public ComponentAttributeIntermediateNode(TagHelperDirectiveAttributeIntermediateNode directiveAttributeNode)
    {
        if (directiveAttributeNode == null)
        {
            throw new ArgumentNullException(nameof(directiveAttributeNode));
        }

        AttributeName = directiveAttributeNode.AttributeName;
        AttributeStructure = directiveAttributeNode.AttributeStructure;
        BoundAttribute = directiveAttributeNode.BoundAttribute;
        PropertyName = directiveAttributeNode.BoundAttribute.GetPropertyName();
        Source = directiveAttributeNode.Source;
        TagHelper = directiveAttributeNode.TagHelper;
        TypeName = directiveAttributeNode.BoundAttribute.IsWeaklyTyped() ? null : directiveAttributeNode.BoundAttribute.TypeName;

        for (var i = 0; i < directiveAttributeNode.Children.Count; i++)
        {
            Children.Add(directiveAttributeNode.Children[i]);
        }

        for (var i = 0; i < directiveAttributeNode.Diagnostics.Count; i++)
        {
            Diagnostics.Add(directiveAttributeNode.Diagnostics[i]);
        }
    }

    public ComponentAttributeIntermediateNode(TagHelperDirectiveAttributeParameterIntermediateNode directiveAttributeParameterNode)
    {
        if (directiveAttributeParameterNode == null)
        {
            throw new ArgumentNullException(nameof(directiveAttributeParameterNode));
        }

        AttributeName = directiveAttributeParameterNode.AttributeNameWithoutParameter;
        AttributeStructure = directiveAttributeParameterNode.AttributeStructure;
        BoundAttribute = directiveAttributeParameterNode.BoundAttribute;
        PropertyName = directiveAttributeParameterNode.BoundAttributeParameter.GetPropertyName();
        Source = directiveAttributeParameterNode.Source;
        TagHelper = directiveAttributeParameterNode.TagHelper;
        TypeName = directiveAttributeParameterNode.BoundAttributeParameter.TypeName;

        for (var i = 0; i < directiveAttributeParameterNode.Children.Count; i++)
        {
            Children.Add(directiveAttributeParameterNode.Children[i]);
        }

        for (var i = 0; i < directiveAttributeParameterNode.Diagnostics.Count; i++)
        {
            Diagnostics.Add(directiveAttributeParameterNode.Diagnostics[i]);
        }
    }

    public override IntermediateNodeCollection Children { get; } = new IntermediateNodeCollection();

    public string AttributeName { get; set; }

    public AttributeStructure AttributeStructure { get; set; }

    public BoundAttributeDescriptor BoundAttribute { get; set; }

    public string PropertyName { get; set; }

    public TagHelperDescriptor TagHelper { get; set; }

    public string TypeName { get; set; }

    public string GloballyQualifiedTypeName { get; set; }

    public override void Accept(IntermediateNodeVisitor visitor)
    {
        if (visitor == null)
        {
            throw new ArgumentNullException(nameof(visitor));
        }

        visitor.VisitComponentAttribute(this);
    }

    public override void FormatNode(IntermediateNodeFormatter formatter)
    {
        if (formatter == null)
        {
            throw new ArgumentNullException(nameof(formatter));
        }

        formatter.WriteContent(AttributeName);

        formatter.WriteProperty(nameof(AttributeName), AttributeName);
        formatter.WriteProperty(nameof(AttributeStructure), AttributeStructure.ToString());
        formatter.WriteProperty(nameof(BoundAttribute), BoundAttribute?.DisplayName);
        formatter.WriteProperty(nameof(PropertyName), PropertyName);
        formatter.WriteProperty(nameof(TagHelper), TagHelper?.DisplayName);
        formatter.WriteProperty(nameof(TypeName), TypeName);
        formatter.WriteProperty(nameof(GloballyQualifiedTypeName), GloballyQualifiedTypeName);
    }

    public bool TryParseEventCallbackTypeArgument(out string argument)
    {
        if (TryParseEventCallbackTypeArgument(out StringSegment stringSegment))
        {
            argument = stringSegment.Value;
            return true;
        }

        argument = null;
        return false;
    }

    internal bool TryParseEventCallbackTypeArgument(out StringSegment argument)
    {
        // This is ugly and ad-hoc, but for various layering reasons we can't just use Roslyn APIs
        // to parse this. We need to parse this just before we write it out to the code generator,
        // so we can't compute it up front either.

        if (BoundAttribute == null || !BoundAttribute.IsEventCallbackProperty())
        {
            throw new InvalidOperationException("This attribute is not an EventCallback attribute.");
        }

        if (string.Equals(TypeName, ComponentsApi.EventCallback.FullTypeName, StringComparison.Ordinal))
        {
            // Non-Generic
            argument = default;
            return false;
        }

        if (TypeName != null &&
            TypeName.Length > ComponentsApi.EventCallback.FullTypeName.Length + "<>".Length &&
            TypeName.StartsWith(ComponentsApi.EventCallback.FullTypeName, StringComparison.Ordinal) &&
            TypeName[ComponentsApi.EventCallback.FullTypeName.Length] == '<' &&
            TypeName[TypeName.Length - 1] == '>')
        {
            // OK this is promising.
            //
            // Chop off leading `...EventCallback<` and let the length so the ending `>` is cut off as well.
            argument = new StringSegment(TypeName, ComponentsApi.EventCallback.FullTypeName.Length + 1, TypeName.Length - (ComponentsApi.EventCallback.FullTypeName.Length + "<>".Length));
            return true;
        }

        // If we get here this is a failure. This should only happen if someone manages to mangle the name with extensibility.
        // We don't really want to crash though.
        argument = default;
        return false;
    }
}
