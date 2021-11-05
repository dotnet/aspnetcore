// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Razor.Language;

internal class DefaultBoundAttributeParameterDescriptorBuilder : BoundAttributeParameterDescriptorBuilder
{
    private readonly DefaultBoundAttributeDescriptorBuilder _parent;
    private readonly string _kind;
    private readonly Dictionary<string, string> _metadata;

    private RazorDiagnosticCollection _diagnostics;

    public DefaultBoundAttributeParameterDescriptorBuilder(DefaultBoundAttributeDescriptorBuilder parent, string kind)
    {
        _parent = parent;
        _kind = kind;

        _metadata = new Dictionary<string, string>();
    }

    public override string Name { get; set; }

    public override string TypeName { get; set; }

    public override bool IsEnum { get; set; }

    public override string Documentation { get; set; }

    public override string DisplayName { get; set; }

    public override IDictionary<string, string> Metadata => _metadata;

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

    internal bool CaseSensitive => _parent.CaseSensitive;

    public BoundAttributeParameterDescriptor Build()
    {
        var diagnostics = Validate();
        if (_diagnostics != null)
        {
            diagnostics ??= new();
            diagnostics.UnionWith(_diagnostics);
        }
        var descriptor = new DefaultBoundAttributeParameterDescriptor(
            _kind,
            Name,
            TypeName,
            IsEnum,
            Documentation,
            GetDisplayName(),
            CaseSensitive,
            new Dictionary<string, string>(Metadata),
            diagnostics?.ToArray() ?? Array.Empty<RazorDiagnostic>());

        return descriptor;
    }

    private string GetDisplayName()
    {
        if (DisplayName != null)
        {
            return DisplayName;
        }

        return $":{Name}";
    }

    private HashSet<RazorDiagnostic> Validate()
    {
        HashSet<RazorDiagnostic> diagnostics = null;
        if (string.IsNullOrWhiteSpace(Name))
        {

            var diagnostic = RazorDiagnosticFactory.CreateTagHelper_InvalidBoundAttributeParameterNullOrWhitespace(_parent.Name);
            diagnostics ??= new();
            diagnostics.Add(diagnostic);
        }
        else
        {
            foreach (var character in Name)
            {
                if (char.IsWhiteSpace(character) || HtmlConventions.IsInvalidNonWhitespaceHtmlCharacters(character))
                {
                    var diagnostic = RazorDiagnosticFactory.CreateTagHelper_InvalidBoundAttributeParameterName(
                        _parent.Name,
                        Name,
                        character);

                    diagnostics ??= new();
                    diagnostics.Add(diagnostic);
                }
            }
        }

        return diagnostics;
    }
}
