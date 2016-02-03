// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Antiforgery.Internal
{
    public class DefaultAntiforgeryContextAccessor : IAntiforgeryContextAccessor
    {
        public AntiforgeryContext Value { get; set; }
    }
}
