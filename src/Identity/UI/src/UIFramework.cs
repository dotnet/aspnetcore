// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Identity.UI;

internal enum UIFramework
{
    // The default framework for a given release must be 0.
    // So this needs to be updated in the future if we include more frameworks.
    Bootstrap5 = 0,
    [Obsolete("Bootstrap 4 support is obsolete. Bootstrap 4 reached end of life on January 1, 2024. Use Bootstrap 5 instead.")]
    Bootstrap4 = 1
}
