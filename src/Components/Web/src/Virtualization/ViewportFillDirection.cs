// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Web.Virtualization;

/// <remarks>
/// The numeric values must stay in sync with the <c>ViewportFillDirection</c> constant in
/// <c>Virtualize.ts</c>.
/// </remarks>
internal enum ViewportFillDirection
{
    Covered = 0,
    Before = 1,
    After = 2,
}
