// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;

namespace Microsoft.AspNetCore.Components.Endpoints.FormMapping.Metadata;

internal class FormDataTypeMetadata(Type type)
{
    public FormDataTypeKind Kind { get; set; }

    public Type Type { get; set; } = type;

    public FormDataTypeMetadata? ElementType { get; set; }

    public FormDataTypeMetadata? KeyType { get; set; }

    public FormDataTypeMetadata? ValueType { get; set; }

    public ConstructorInfo? Constructor { get; set; }

    public IList<FormDataParameterMetadata> ConstructorParameters { get; set; } = new List<FormDataParameterMetadata>();

    public IList<FormDataPropertyMetadata> Properties { get; set; } = new List<FormDataPropertyMetadata>();

    public bool IsRecursive { get; internal set; }
}
