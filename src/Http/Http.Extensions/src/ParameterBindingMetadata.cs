// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;

namespace Microsoft.AspNetCore.Http.Metadata;

internal sealed class ParameterBindingMetadata(
    string name,
    ParameterInfo parameterInfo,
    bool hasTryParse = false,
    bool hasBindAsync = false,
    bool isOptional = false) : IParameterBindingMetadata
{
    public string Name => name;

    public bool HasTryParse => hasTryParse;

    public bool HasBindAsync => hasBindAsync;

    public ParameterInfo ParameterInfo => parameterInfo;

    public bool IsOptional => isOptional;
}
