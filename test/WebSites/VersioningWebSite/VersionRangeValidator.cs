// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc;

namespace VersioningWebSite
{
    public class VersionRangeValidator : IActionConstraint
    {
        private readonly int? _minVersion;
        private readonly int? _maxVersion;

        public int Order { get; set; }

        public VersionRangeValidator(int? minVersion, int? maxVersion)
        {
            _minVersion = minVersion;
            _maxVersion = maxVersion;
        }

        public static string GetVersion(HttpRequest request)
        {
            return request.Query["version"];
        }

        public bool Accept(ActionConstraintContext context)
        {
            int version;
            if (int.TryParse(GetVersion(context.RouteContext.HttpContext.Request), out version))
            {
                return (_minVersion == null || _minVersion <= version) &&
                    (_maxVersion == null || _maxVersion >= version);
            }
            else
            {
                return false;
            }
        }
    }
}