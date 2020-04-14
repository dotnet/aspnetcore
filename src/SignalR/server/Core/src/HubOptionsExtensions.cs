// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.SignalR
{
    public static class HubOptionsExtensions
    {
        public static void AddFilter(this HubOptions options, IHubFilter hubFilter)
        {
            if (options.HubFilters == null)
            {
                options.HubFilters = new List<object>();
            }

            options.HubFilters.Add(hubFilter);
        }

        public static void AddFilter<IHubFilter>(this HubOptions options)
        {
            if (options.HubFilters == null)
            {
                options.HubFilters = new List<object>();
            }

            options.HubFilters.Add(typeof(IHubFilter));
        }
    }
}
