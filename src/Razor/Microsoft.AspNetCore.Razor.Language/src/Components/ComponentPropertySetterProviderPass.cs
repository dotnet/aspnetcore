// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.Components;

internal class ComponentPropertySetterProviderPass : ComponentIntermediateNodePassBase, IRazorDocumentClassifierPass
{
    // Run as soon as possible after the component rewrite pass
    public override int Order => 1;

    protected override void ExecuteCore(RazorCodeDocument codeDocument, DocumentIntermediateNode documentNode)
    {
        if (!IsComponentDocument(documentNode))
        {
            return;
        }

        var tagHelperContext = codeDocument.GetTagHelperContext();

        if (tagHelperContext is null)
        {
            return;
        }

        var primaryNamespace = documentNode.FindPrimaryNamespace();
        var primaryClass = documentNode.FindPrimaryClass();
        var primaryClassTypeName = GetTypeName(primaryNamespace, primaryClass);

        var tagHelper = tagHelperContext.TagHelpers
            .Where(th => th.IsComponentTagHelper())
            .FirstOrDefault(th => th.Name == primaryClassTypeName);

        if (tagHelper is null)
        {
            return;
        }

        var parameterDescriptors = tagHelper.BoundAttributes.Where(bad
            => bad.Metadata.ContainsKey(TagHelperMetadata.Common.PropertyName)
            && IsMetadataValueFalsey(bad, ComponentMetadata.Component.TypeParameterKey)
            && IsMetadataValueFalsey(bad, ComponentMetadata.Component.ChildContentParameterNameKey));

        var componentParameterDataNode = new ComponentParameterDataIntermediateNode();

        foreach (var descriptor in parameterDescriptors)
        {
            componentParameterDataNode.AddParameterData(descriptor);
        }

        var nodeParameterDataBuilder = IntermediateNodeBuilder.Create(primaryClass);
        nodeParameterDataBuilder.Add(componentParameterDataNode);
    }

    private static string GetTypeName(
        NamespaceDeclarationIntermediateNode namespaceNode,
        ClassDeclarationIntermediateNode classNode)
    {
        var fullyQualifiedName = namespaceNode.Content + Type.Delimiter + classNode.ClassName;

        if (classNode.TypeParameters.Count == 0)
        {
            return fullyQualifiedName;
        }

        var typeParameters = string.Join(", ", classNode.TypeParameters.Select(tp => tp.ParameterName));

        return $"{fullyQualifiedName}<{typeParameters}>";
    }

    private static bool IsMetadataValueFalsey(BoundAttributeDescriptor descriptor, string key)
        => !descriptor.Metadata.TryGetValue(key, out var value) || value == bool.FalseString;
}
