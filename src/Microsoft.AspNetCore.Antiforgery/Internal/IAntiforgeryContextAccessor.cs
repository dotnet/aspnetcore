// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Antiforgery.Internal
{
    public interface IAntiforgeryContextAccessor
    {
        AntiforgeryContext Value { get; set; }
    }
}
