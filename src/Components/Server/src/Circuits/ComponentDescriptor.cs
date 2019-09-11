// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Components.Server
{
    internal class ComponentDescriptor
    {
        public Type ComponentType { get; set; }

        public int Sequence { get; set; }

        public void Deconstruct(out Type componentType, out int sequence)
        {
            componentType = ComponentType;
            sequence = Sequence;
        }
    }
}
