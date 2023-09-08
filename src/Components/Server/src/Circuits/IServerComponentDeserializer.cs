// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components.Server.Circuits;

namespace Microsoft.AspNetCore.Components.Server;

internal interface IServerComponentDeserializer
{
    bool TryDeserializeComponentDescriptorCollection(
        string serializedComponentRecords,
        out List<ComponentDescriptor> descriptors);

    bool TryDeserializeSingleComponentDescriptor(ComponentMarker record, [NotNullWhen(true)] out ComponentDescriptor? result);

    bool TryDeserializeCircuitComponentValidation(string payload, [NotNullWhen(true)] out CircuitRootComponentValidation? result);
}
