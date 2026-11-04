// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Identity.UI;

internal enum UIFramework
{
    // The default framework for a given release must be 0.
    // So this needs to be updated in the future if we include more frameworks.
    Bootstrap5 = 0,
    Bootstrap4 = 1
}
