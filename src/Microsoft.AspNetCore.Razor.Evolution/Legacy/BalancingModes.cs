// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Razor.Evolution.Legacy
{
    [Flags]
    internal enum BalancingModes
    {
        None = 0,
        BacktrackOnFailure = 1,
        NoErrorOnFailure = 2,
        AllowCommentsAndTemplates = 4,
        AllowEmbeddedTransitions = 8
    }
}
