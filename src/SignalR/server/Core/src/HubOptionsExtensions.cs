// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.SignalR.Internal;

namespace Microsoft.AspNetCore.SignalR
{
    public static class HubOptionsExtensions
    {
        public static void AddFilter(this HubOptions options, IHubFilter hubFilter)
        {
            if (options.HubFilters == null)
            {
                options.HubFilters = new List<IHubFilter>();
            }

            options.HubFilters.Add(hubFilter);
        }

        public static void AddFilter<T>(this HubOptions options) where T : IHubFilter
        {
            if (options.HubFilters == null)
            {
                options.HubFilters = new List<IHubFilter>();
            }

            options.HubFilters.Add(new HubFilterFactory(typeof(T)));
        }
    }
}
