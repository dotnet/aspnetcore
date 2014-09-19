// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNet.Mvc;

namespace VersioningWebSite
{
    public class VersionGetAttribute : VersionRoute, IActionHttpMethodProvider
    {
        public VersionGetAttribute(string template)
            : base(template)
        {
        }

        public VersionGetAttribute(string template, string versionRange)
            : base(template, versionRange)
        {
        }

        private readonly IEnumerable<string> _httpMethods = new[] { "GET" };

        public IEnumerable<string> HttpMethods
        {
            get
            {
                return _httpMethods;
            }
        }
    }
}