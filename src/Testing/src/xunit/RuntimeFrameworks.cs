// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Testing
{
    [Flags]
    public enum RuntimeFrameworks
    {
        None = 0,
        Mono = 1 << 0,
        CLR = 1 << 1,
        CoreCLR = 1 << 2
    }
}