// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components;

#nullable enable // This is shared-source with Mvc.ViewFeatures which doesn't enable nullability by default

// The DTO that we data-protect and include into any
// generated component marker and that allows the client
// to bootstrap a blazor server-side application.
internal struct ServerComponent
{
    public ServerComponent(
        int sequence,
        ComponentMarkerKey? key,
        string assemblyName,
        string typeName,
        IList<ComponentParameter> parametersDefinitions,
        IList<object?> parameterValues,
        Guid invocationId) =>
        (Sequence, Key, AssemblyName, TypeName, ParameterDefinitions, ParameterValues, InvocationId) =
        (sequence, key, assemblyName, typeName, parametersDefinitions, parameterValues, invocationId);

    // The order in which this component was rendered
    public int Sequence { get; set; }

    // A key that identifies the component instance.
    public ComponentMarkerKey? Key { get; set; }

    // The assembly name for the rendered component.
    public string AssemblyName { get; set; }

    // The type name of the component.
    public string TypeName { get; set; }

    // The definition for the parameters for the component.
    public IList<ComponentParameter> ParameterDefinitions { get; set; }

    // The values for the parameters for the component.
    public IList<object?> ParameterValues { get; set; }

    // An id that uniquely identifies all components generated as part of a single HTTP response.
    public Guid InvocationId { get; set; }
}
