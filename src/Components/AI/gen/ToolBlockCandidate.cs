// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Components.AI.SourceGenerators;

internal sealed class ToolBlockCandidate
{
    public string Namespace { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public string ToolName { get; set; } = string.Empty;
    public List<ToolParameterInfo> Parameters { get; set; } = new();
    public List<ToolResultPropertyInfo> ResultProperties { get; set; } = new();
}

internal sealed class ToolResultPropertyInfo
{
    public string PropertyName { get; set; } = string.Empty;
    public string ResultKey { get; set; } = string.Empty;
    public string TypeName { get; set; } = string.Empty;
    public bool IsNullable { get; set; }
    public ParameterTypeKind TypeKind { get; set; }
}

internal sealed class ToolParameterInfo
{
    public string PropertyName { get; set; } = string.Empty;
    public string ArgumentKey { get; set; } = string.Empty;
    public string TypeName { get; set; } = string.Empty;
    public bool IsNullable { get; set; }
    public ParameterTypeKind TypeKind { get; set; }
}

internal enum ParameterTypeKind
{
    String,
    Int32,
    Int64,
    Double,
    Single,
    Decimal,
    Boolean,
    Complex
}
