// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Razor.Language;

internal class DefaultAllowedChildTagDescriptorBuilder : AllowedChildTagDescriptorBuilder
{
    private readonly DefaultTagHelperDescriptorBuilder _parent;
    private RazorDiagnosticCollection _diagnostics;

    public DefaultAllowedChildTagDescriptorBuilder(DefaultTagHelperDescriptorBuilder parent)
    {
        _parent = parent;
    }

    public override string Name { get; set; }

    public override string DisplayName { get; set; }

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

    public AllowedChildTagDescriptor Build()
    {
        var diagnostics = Validate();
        if (_diagnostics != null)
        {
            diagnostics ??= new();
            diagnostics.UnionWith(_diagnostics);
        }

        var displayName = DisplayName ?? Name;
        var descriptor = new DefaultAllowedChildTagDescriptor(
            Name,
            displayName,
            diagnostics?.ToArray() ?? Array.Empty<RazorDiagnostic>());

        return descriptor;
    }

    private HashSet<RazorDiagnostic> Validate()
    {
        HashSet<RazorDiagnostic> diagnostics = null;
        if (string.IsNullOrWhiteSpace(Name))
        {
            var diagnostic = RazorDiagnosticFactory.CreateTagHelper_InvalidRestrictedChildNullOrWhitespace(_parent.GetDisplayName());

            diagnostics ??= new();
            diagnostics.Add(diagnostic);
        }
        else if (Name != TagHelperMatchingConventions.ElementCatchAllName)
        {
            foreach (var character in Name)
            {
                if (char.IsWhiteSpace(character) || HtmlConventions.IsInvalidNonWhitespaceHtmlCharacters(character))
                {
                    var diagnostic = RazorDiagnosticFactory.CreateTagHelper_InvalidRestrictedChild(_parent.GetDisplayName(), Name, character);
                    diagnostics ??= new();
                    diagnostics.Add(diagnostic);
                }
            }
        }

        return diagnostics;
    }
}
