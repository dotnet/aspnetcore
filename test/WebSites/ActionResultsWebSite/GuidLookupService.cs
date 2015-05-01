// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace ActionResultsWebSite
{
    public class GuidLookupService
    {
        public Dictionary<string, bool> IsDisposed { get; } = new Dictionary<string, bool>();
    }
}