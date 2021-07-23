// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Diagnostics;

namespace Ignitor
{
    [DebuggerDisplay("{SerializedValue}")]
    public abstract class Node
    {
        public virtual ContainerNode? Parent { get; set; }

        public string SerializedValue => NodeSerializer.Serialize(this);
    }
}

#nullable restore
