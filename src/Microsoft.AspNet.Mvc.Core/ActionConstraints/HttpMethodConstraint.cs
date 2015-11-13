// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNet.Mvc.ActionConstraints
{
    public class HttpMethodConstraint : IActionConstraint
    {
        public static readonly int HttpMethodConstraintOrder = 100;

        private readonly IReadOnlyList<string> _httpMethods;

        private readonly string OriginHeader = "Origin";
        private readonly string AccessControlRequestMethod = "Access-Control-Request-Method";
        private readonly string PreflightHttpMethod = "OPTIONS";

        // Empty collection means any method will be accepted.
        public HttpMethodConstraint(IEnumerable<string> httpMethods)
        {
            if (httpMethods == null)
            {
                throw new ArgumentNullException(nameof(httpMethods));
            }

            var methods = new List<string>();

            foreach (var method in httpMethods)
            {
                if (string.IsNullOrEmpty(method))
                {
                    throw new ArgumentException("httpMethod cannot be null or empty");
                }

                methods.Add(method);
            }

            _httpMethods = new ReadOnlyCollection<string>(methods);
        }

        public IEnumerable<string> HttpMethods
        {
            get
            {
                return _httpMethods;
            }
        }

        public int Order
        {
            get { return HttpMethodConstraintOrder; }
        }

        public bool Accept(ActionConstraintContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (_httpMethods.Count == 0)
            {
                return true;
            }

            var request = context.RouteContext.HttpContext.Request;
            var method = request.Method;
            if (request.Headers.ContainsKey(OriginHeader))
            {
                // Update the http method if it is preflight request.
                var accessControlRequestMethod = request.Headers[AccessControlRequestMethod];
                if (string.Equals(
                        request.Method,
                        PreflightHttpMethod,
                        StringComparison.OrdinalIgnoreCase) &&
                    !StringValues.IsNullOrEmpty(accessControlRequestMethod))
                {
                    method = accessControlRequestMethod;
                }
            }

            for (var i = 0; i < _httpMethods.Count; i++)
            {
                var supportedMethod = _httpMethods[i];
                if (string.Equals(supportedMethod, method, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
