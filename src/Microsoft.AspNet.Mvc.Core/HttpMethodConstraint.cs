// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.AspNet.Cors.Core;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc
{
    public class HttpMethodConstraint : IActionConstraint
    {
        public static readonly int HttpMethodConstraintOrder = 100;

        private readonly IReadOnlyList<string> _methods;

        // Empty collection means any method will be accepted.
        public HttpMethodConstraint([NotNull] IEnumerable<string> httpMethods)
        {
            var methods = new List<string>();

            foreach (var method in httpMethods)
            {
                if (string.IsNullOrEmpty(method))
                {
                    throw new ArgumentException("httpMethod cannot be null or empty");
                }

                methods.Add(method);
            }

            _methods = new ReadOnlyCollection<string>(methods);
        }

        public IEnumerable<string> HttpMethods
        {
            get
            {
                return _methods;
            }
        }

        public int Order
        {
            get { return HttpMethodConstraintOrder; }
        }

        public bool Accept([NotNull] ActionConstraintContext context)
        {
            if (_methods.Count == 0)
            {
                return true;
            }

            var request = context.RouteContext.HttpContext.Request;
            var method = request.Method;
            if (request.Headers.ContainsKey(CorsConstants.Origin))
            {
                // Update the http method if it is preflight request.
                var accessControlRequestMethod = request.Headers.Get(CorsConstants.AccessControlRequestMethod);
                if (string.Equals(
                        request.Method,
                        CorsConstants.PreflightHttpMethod,
                        StringComparison.Ordinal) &&
                    accessControlRequestMethod != null)
                {
                    method = accessControlRequestMethod;
                }
            }

            return (HttpMethods.Any(m => m.Equals(method, StringComparison.Ordinal)));
        }
    }
}
