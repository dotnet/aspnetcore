// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.Components.Server;

internal interface IServerComponentDeserializer
{
    bool TryDeserializeComponentDescriptorCollection(
        string serializedComponentRecords,
        out List<ComponentDescriptor> descriptors);
    bool TryDeserializeRootComponentOperations(string serializedComponentOperations, [NotNullWhen(true)] out RootComponentOperationBatch? operationBatch);
}
