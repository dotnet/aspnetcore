// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.Razor.Language.Legacy;

[Flags]
internal enum BalancingModes
{
    None = 0,
    BacktrackOnFailure = 1,
    NoErrorOnFailure = 2,
    AllowCommentsAndTemplates = 4,
    AllowEmbeddedTransitions = 8,
    StopAtEndOfLine = 16,
}
