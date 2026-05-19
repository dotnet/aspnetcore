// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.ObjectModel;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Mvc.Cors;

internal sealed class CorsHttpMethodActionConstraint : HttpMethodActionConstraint
{
    private readonly string OriginHeader = "Origin";
    private readonly string AccessControlRequestMethod = "Access-Control-Request-Method";

    public CorsHttpMethodActionConstraint(HttpMethodActionConstraint constraint)
        : base(constraint.HttpMethods)
    {
    }

    public override bool Accept(ActionConstraintContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

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
