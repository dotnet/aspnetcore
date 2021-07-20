// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.HotReload
{
    internal sealed class UpdateDelta
    {
        public Guid ModuleId { get; set; }

        public byte[] MetadataDelta { get; set; } = default!;

        public byte[] ILDelta { get; set; } = default!;
    }
}
