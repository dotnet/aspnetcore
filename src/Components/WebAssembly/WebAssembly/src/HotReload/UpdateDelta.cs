// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.HotReload;

internal readonly struct UpdateDelta(Guid moduleId, byte[] metadataDelta, byte[] ilDelta, byte[] pdbDelta, int[] updatedTypes)
{
    public Guid ModuleId { get; } = moduleId;
    public byte[] MetadataDelta { get; } = metadataDelta;
    public byte[] ILDelta { get; } = ilDelta;
    public byte[] PdbDelta { get; } = pdbDelta;
    public int[] UpdatedTypes { get; } = updatedTypes;
}
