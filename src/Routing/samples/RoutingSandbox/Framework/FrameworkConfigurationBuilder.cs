// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Patterns;

namespace RoutingSandbox.Framework
{
    public class FrameworkConfigurationBuilder
    {
        private readonly FrameworkEndpointDataSource _dataSource;

        internal FrameworkConfigurationBuilder(FrameworkEndpointDataSource dataSource)
        {
            _dataSource = dataSource;
        }

        public void AddPattern(string pattern)
        {
            AddPattern(RoutePatternFactory.Parse(pattern));
        }

        public void AddPattern(RoutePattern pattern)
        {    
            _dataSource.Patterns.Add(pattern);
        }

        public void AddHubMethod(string hub, string method, RequestDelegate requestDelegate)
        {
            _dataSource.HubMethods.Add(new HubMethod
            {
                Hub = hub,
                Method = method,
                RequestDelegate = requestDelegate
            });
        }
    }
}
