// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.ObjectModel;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Mvc.Cors
{
    internal class CorsHttpMethodActionConstraint : HttpMethodActionConstraint
    {
        private readonly string OriginHeader = "Origin";
        private readonly string AccessControlRequestMethod = "Access-Control-Request-Method";

        public CorsHttpMethodActionConstraint(HttpMethodActionConstraint constraint)
            : base(constraint.HttpMethods)
        {
        }

        public override bool Accept(ActionConstraintContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var methods = (ReadOnlyCollection<string>)HttpMethods;
            if (methods.Count == 0)
            {
                return true;
            }

            var request = context.RouteContext.HttpContext.Request;
            // Perf: Check http method before accessing the Headers collection.
            if (Http.HttpMethods.IsOptions(request.Method) &&
                request.Headers.ContainsKey(OriginHeader) &&
                request.Headers.TryGetValue(AccessControlRequestMethod, out var accessControlRequestMethod) &&
                !StringValues.IsNullOrEmpty(accessControlRequestMethod))
            {
                // Read interface .Count once rather than per iteration
                var methodsCount = methods.Count;
                for (var i = 0; i < methodsCount; i++)
                {
                    var supportedMethod = methods[i];
                    if (string.Equals(supportedMethod, accessControlRequestMethod, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }

                return false;
            }

            return base.Accept(context);
        }
    }
}
