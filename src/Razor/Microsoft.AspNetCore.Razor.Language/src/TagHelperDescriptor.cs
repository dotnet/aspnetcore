// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Razor.Language;

public abstract class TagHelperDescriptor : IEquatable<TagHelperDescriptor>
{
    private IEnumerable<RazorDiagnostic> _allDiagnostics;
    private BoundAttributeDescriptor[] _editorRequiredAttributes;

    protected TagHelperDescriptor(string kind)
    {
        Kind = kind;
    }

    public string Kind { get; }

    public string Name { get; protected set; }

    public IReadOnlyList<TagMatchingRuleDescriptor> TagMatchingRules { get; protected set; }

    public string AssemblyName { get; protected set; }

    public IReadOnlyList<BoundAttributeDescriptor> BoundAttributes { get; protected set; }

    public IReadOnlyList<AllowedChildTagDescriptor> AllowedChildTags { get; protected set; }

    public string Documentation { get; protected set; }

    public string DisplayName { get; protected set; }

    public string TagOutputHint { get; protected set; }

    public bool CaseSensitive { get; protected set; }

    public IReadOnlyList<RazorDiagnostic> Diagnostics { get; protected set; }

    public IReadOnlyDictionary<string, string> Metadata { get; protected set; }

    // Hoisted / cached metadata
    private int? _hashCode;
    internal bool? IsComponentFullyQualifiedNameMatchCache { get; set; }
    internal bool? IsChildContentTagHelperCache { get; set; }
    internal ParsedTypeInformation? ParsedTypeInfo { get; set; }
    internal BoundAttributeDescriptor[] EditorRequiredAttributes
    {
        get
        {
            _editorRequiredAttributes ??= GetEditorRequiredAttributes(BoundAttributes);
            return _editorRequiredAttributes;
        }
    }

    public bool HasErrors
    {
        get
        {
            var allDiagnostics = GetAllDiagnostics();
            var errors = allDiagnostics.Any(diagnostic => diagnostic.Severity == RazorDiagnosticSeverity.Error);

            return errors;
        }
    }

    public virtual IEnumerable<RazorDiagnostic> GetAllDiagnostics()
    {
        if (_allDiagnostics == null)
        {
            var allowedChildTagDiagnostics = AllowedChildTags.SelectMany(childTag => childTag.Diagnostics);
            var attributeDiagnostics = BoundAttributes.SelectMany(attribute => attribute.Diagnostics);
            var ruleDiagnostics = TagMatchingRules.SelectMany(rule => rule.GetAllDiagnostics());
            var combinedDiagnostics = allowedChildTagDiagnostics
                .Concat(attributeDiagnostics)
                .Concat(ruleDiagnostics)
                .Concat(Diagnostics);
            _allDiagnostics = combinedDiagnostics.ToArray();
        }

        return _allDiagnostics;
    }

    public override string ToString()
    {
        return DisplayName ?? base.ToString();
    }

    public bool Equals(TagHelperDescriptor other)
    {
        return TagHelperDescriptorComparer.Default.Equals(this, other);
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as TagHelperDescriptor);
    }

    public override int GetHashCode()
    {
        // TagHelperDescriptors are immutable instances and it should be safe to cache it's hashes once.
        _hashCode ??= TagHelperDescriptorComparer.Default.GetHashCode(this);
        return _hashCode.Value;
    }

    private static BoundAttributeDescriptor[] GetEditorRequiredAttributes(IReadOnlyList<BoundAttributeDescriptor> boundAttributeDescriptors)
    {
        List<BoundAttributeDescriptor> editorRequiredAttributes = null;
        var count = boundAttributeDescriptors.Count;
        for (var i = 0; i < count; i++)
        {
            var attribute = boundAttributeDescriptors[i];
            if (attribute.IsEditorRequired)
            {
                editorRequiredAttributes ??= new();
                editorRequiredAttributes.Add(attribute);
            }
        }

        return editorRequiredAttributes?.ToArray() ?? Array.Empty<BoundAttributeDescriptor>();
    }

    internal readonly struct ParsedTypeInformation
    {
        public ParsedTypeInformation(bool success, StringSegment @namespace, StringSegment typeName)
        {
            Success = success;
            Namespace = @namespace;
            TypeName = typeName;
        }

        public bool Success { get; }
        public StringSegment Namespace { get; }
        public StringSegment TypeName { get; }
    }
}
