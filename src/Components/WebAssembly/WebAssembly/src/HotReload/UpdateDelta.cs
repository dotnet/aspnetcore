// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
