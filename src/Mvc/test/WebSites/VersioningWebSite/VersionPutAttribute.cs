// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Routing;

namespace VersioningWebSite
{
    public class VersionPutAttribute : VersionRouteAttribute, IActionHttpMethodProvider
    {
        public VersionPutAttribute(string template)
            : base(template)
        {
        }

        public VersionPutAttribute(string template, string versionRange)
            : base(template, versionRange)
        {
        }

        private readonly IEnumerable<string> _httpMethods = new[] { "PUT" };

        public IEnumerable<string> HttpMethods
        {
            get
            {
                return _httpMethods;
            }
        }
    }
}