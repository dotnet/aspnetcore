// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Razor.Language;

internal class DefaultRequiredAttributeDescriptorBuilder : RequiredAttributeDescriptorBuilder
{
    private readonly DefaultTagMatchingRuleDescriptorBuilder _parent;
    private RazorDiagnosticCollection _diagnostics;
    private readonly Dictionary<string, string> _metadata = new Dictionary<string, string>();

    public DefaultRequiredAttributeDescriptorBuilder(DefaultTagMatchingRuleDescriptorBuilder parent)
    {
        _parent = parent;
    }

    public override string Name { get; set; }

    public override RequiredAttributeDescriptor.NameComparisonMode NameComparisonMode { get; set; }

    public override string Value { get; set; }

    public override RequiredAttributeDescriptor.ValueComparisonMode ValueComparisonMode { get; set; }

    public override RazorDiagnosticCollection Diagnostics
    {
        get
        {
            if (_diagnostics == null)
            {
                _diagnostics = new RazorDiagnosticCollection();
            }

            return _diagnostics;
        }
    }

    public override IDictionary<string, string> Metadata => _metadata;

    internal bool CaseSensitive => _parent.CaseSensitive;

    public RequiredAttributeDescriptor Build()
    {
        var diagnostics = Validate();
        if (_diagnostics != null)
        {
            diagnostics ??= new();
            diagnostics.UnionWith(_diagnostics);
        }

        var displayName = GetDisplayName();
        var rule = new DefaultRequiredAttributeDescriptor(
            Name,
            NameComparisonMode,
            CaseSensitive,
            Value,
            ValueComparisonMode,
            displayName,
            diagnostics?.ToArray() ?? Array.Empty<RazorDiagnostic>(),
            new Dictionary<string, string>(Metadata));

        return rule;
    }

    private string GetDisplayName()
    {
        return NameComparisonMode == RequiredAttributeDescriptor.NameComparisonMode.PrefixMatch ? string.Concat(Name, "...") : Name;
    }

    private HashSet<RazorDiagnostic> Validate()
    {
        HashSet<RazorDiagnostic> diagnostics = null;

        if (string.IsNullOrWhiteSpace(Name))
        {
            var diagnostic = RazorDiagnosticFactory.CreateTagHelper_InvalidTargetedAttributeNameNullOrWhitespace();

            diagnostics ??= new();
            diagnostics.Add(diagnostic);
        }
        else
        {
            var name = new StringSegment(Name);
            var isDirectiveAttribute = this.IsDirectiveAttribute();
            if (isDirectiveAttribute && name.StartsWith("@", StringComparison.Ordinal))
            {
                name = name.Subsegment(1);
            }
            else if (isDirectiveAttribute)
            {
                var diagnostic = RazorDiagnosticFactory.CreateTagHelper_InvalidRequiredDirectiveAttributeName(GetDisplayName(), Name);

                diagnostics ??= new();
                diagnostics.Add(diagnostic);
            }

            for (var i = 0; i < name.Length; i++)
            {
                var character = name[i];
                if (char.IsWhiteSpace(character) || HtmlConventions.IsInvalidNonWhitespaceHtmlCharacters(character))
                {
                    var diagnostic = RazorDiagnosticFactory.CreateTagHelper_InvalidTargetedAttributeName(Name, character);

                    diagnostics ??= new();
                    diagnostics.Add(diagnostic);
                }
            }
        }

        return diagnostics;
    }
}
