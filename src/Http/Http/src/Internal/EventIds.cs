// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Http
{
    internal static class EventIds
    {
        public static readonly EventId SameSiteNotSecure = new EventId(1, "SameSiteNotSecure");
    }
}
