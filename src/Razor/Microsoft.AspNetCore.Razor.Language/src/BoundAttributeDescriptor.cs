// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Razor.Language;

/// <summary>
/// A metadata class describing a tag helper attribute.
/// </summary>
public abstract class BoundAttributeDescriptor : IEquatable<BoundAttributeDescriptor>
{
    protected BoundAttributeDescriptor(string kind)
    {
        Kind = kind;
    }

    public string Kind { get; }

    public bool IsIndexerStringProperty { get; protected set; }

    public bool IsIndexerBooleanProperty { get; protected set; }

    public bool IsEnum { get; protected set; }

    public bool IsStringProperty { get; protected set; }

    public bool IsBooleanProperty { get; protected set; }

    internal bool IsEditorRequired { get; set; }

    public string Name { get; protected set; }

    public string IndexerNamePrefix { get; protected set; }

    public string TypeName { get; protected set; }

    public string IndexerTypeName { get; protected set; }

    public bool HasIndexer { get; protected set; }

    public string Documentation { get; protected set; }

    public string DisplayName { get; protected set; }

    public bool CaseSensitive { get; protected set; }

    public IReadOnlyList<RazorDiagnostic> Diagnostics { get; protected set; }

    public IReadOnlyDictionary<string, string> Metadata { get; protected set; }

    public virtual IReadOnlyList<BoundAttributeParameterDescriptor> BoundAttributeParameters { get; protected set; }

    public bool HasErrors
    {
        get
        {
            var errors = Diagnostics.Any(diagnostic => diagnostic.Severity == RazorDiagnosticSeverity.Error);

            return errors;
        }
    }

    public override string ToString()
    {
        return DisplayName ?? base.ToString();
    }

    public bool Equals(BoundAttributeDescriptor other)
    {
        return BoundAttributeDescriptorComparer.Default.Equals(this, other);
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as BoundAttributeDescriptor);
    }

    public override int GetHashCode()
    {
        return BoundAttributeDescriptorComparer.Default.GetHashCode(this);
    }
}
