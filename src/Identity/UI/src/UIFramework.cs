// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Identity.UI
{
    internal enum UIFramework
    {
        // The default framework for a given release must be 0.
        // So this needs to be updated in the future if we include more frameworks.
        Bootstrap4 = 0,
        Bootstrap3 = 1,
    }
}
