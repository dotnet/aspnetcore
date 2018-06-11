// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Microsoft.AspNetCore.Routing.EndpointConstraints
{
    public class HttpMethodEndpointConstraint : IEndpointConstraint
    {
        public static readonly int HttpMethodConstraintOrder = 100;

        private readonly IReadOnlyList<string> _httpMethods;

        // Empty collection means any method will be accepted.
        public HttpMethodEndpointConstraint(IEnumerable<string> httpMethods)
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

        public IEnumerable<string> HttpMethods => _httpMethods;

        public int Order => HttpMethodConstraintOrder;

        public virtual bool Accept(EndpointConstraintContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (_httpMethods.Count == 0)
            {
                return true;
            }

            var request = context.HttpContext.Request;
            var method = request.Method;

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