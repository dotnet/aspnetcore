// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Testing
{
    [Flags]
    public enum OperatingSystems
    {
        Linux = 1,
        MacOSX = 2,
        Windows = 4,
    }
}