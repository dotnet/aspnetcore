// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Server;

internal sealed class ComponentDescriptor
{
    public Type ComponentType { get; set; }

    public ParameterView Parameters { get; set; }

    public int Sequence { get; set; }

    public int? UniqueId { get; set; }

    public void Deconstruct(out Type componentType, out ParameterView parameters, out int sequence, out int? uniqueId) =>
        (componentType, sequence, parameters, uniqueId) = (ComponentType, Sequence, Parameters, UniqueId);
}
