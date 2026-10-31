// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;

namespace Microsoft.AspNetCore.Components.Endpoints.FormMapping.Metadata;
internal class FormDataParameterMetadata(ParameterInfo parameter, FormDataTypeMetadata parameterTypeInfo) : IFormDataValue
{
    public ParameterInfo Parameter { get; } = parameter;
    public string Name { get; set; } = parameter.Name!;
    public Type Type { get; set; } = parameter.ParameterType;

    public bool Required => true;

    internal FormDataTypeMetadata ParameterMetadata { get; } = parameterTypeInfo;
}

internal interface IFormDataValue
{
    public string Name { get; }

    public Type Type { get; }

    public bool Required { get; }
}
