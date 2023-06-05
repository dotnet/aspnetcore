// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components;

internal readonly struct CascadingParameterInfo(ICascadingParameterAttribute attribute, string propertyName, Type propertyType)
{
    public ICascadingParameterAttribute Attribute { get; } = attribute;
    public string PropertyName { get; } = propertyName;
    public Type PropertyType { get; } = propertyType;
}
