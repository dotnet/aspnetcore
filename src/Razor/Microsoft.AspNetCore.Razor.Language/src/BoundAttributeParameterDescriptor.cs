// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Razor.Language;

public abstract class BoundAttributeParameterDescriptor : IEquatable<BoundAttributeParameterDescriptor>
{
    protected BoundAttributeParameterDescriptor(string kind)
    {
        Kind = kind;
    }

    public string Kind { get; }

    public bool IsEnum { get; protected set; }

    public bool IsStringProperty { get; protected set; }

    public bool IsBooleanProperty { get; protected set; }

    public string Name { get; protected set; }

    public string TypeName { get; protected set; }

    public string Documentation { get; protected set; }

    public string DisplayName { get; protected set; }

    public bool CaseSensitive { get; protected set; }

    public IReadOnlyList<RazorDiagnostic> Diagnostics { get; protected set; }

    public IReadOnlyDictionary<string, string> Metadata { get; protected set; }

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

    public bool Equals(BoundAttributeParameterDescriptor other)
    {
        return BoundAttributeParameterDescriptorComparer.Default.Equals(this, other);
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as BoundAttributeParameterDescriptor);
    }

    public override int GetHashCode()
    {
        return BoundAttributeParameterDescriptorComparer.Default.GetHashCode(this);
    }
}
