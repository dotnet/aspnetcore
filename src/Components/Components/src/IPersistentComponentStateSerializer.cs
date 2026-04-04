// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;

namespace Microsoft.AspNetCore.Components;

internal interface IPersistentComponentStateSerializer
{
    void Persist(Type type, object value, IBufferWriter<byte> writer);
    object Restore(Type type, ReadOnlySequence<byte> data);
}