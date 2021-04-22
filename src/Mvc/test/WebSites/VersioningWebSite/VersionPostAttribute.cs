// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Routing;

namespace VersioningWebSite
{
    public class VersionPostAttribute : VersionRouteAttribute, IActionHttpMethodProvider
    {
        public VersionPostAttribute(string template)
            : base(template)
        {
        }

        public VersionPostAttribute(string template, string versionRange)
            : base(template, versionRange)
        {
        }

        private readonly IEnumerable<string> _httpMethods = new[] { "POST" };

        public IEnumerable<string> HttpMethods
        {
            get
            {
                return _httpMethods;
            }
        }
    }
}