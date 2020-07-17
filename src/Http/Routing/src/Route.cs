// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Routing
{
    public class Route : RouteBase
    {
        private readonly IRouter _target;

        public Route(
            IRouter target,
            string routeTemplate,
            IInlineConstraintResolver inlineConstraintResolver)
            : this(
                target,
                routeTemplate,
                defaults: null,
                constraints: null,
                dataTokens: null,
                inlineConstraintResolver: inlineConstraintResolver)
        {
        }

        public Route(
            IRouter target,
            string routeTemplate,
            RouteValueDictionary? defaults,
            IDictionary<string, object>? constraints,
            RouteValueDictionary? dataTokens,
            IInlineConstraintResolver inlineConstraintResolver)
            : this(target, null, routeTemplate, defaults, constraints, dataTokens, inlineConstraintResolver)
        {
        }

        public Route(
            IRouter target,
            string? routeName,
            string? routeTemplate,
            RouteValueDictionary? defaults,
            IDictionary<string, object>? constraints,
            RouteValueDictionary? dataTokens,
            IInlineConstraintResolver inlineConstraintResolver)
            : base(
                  routeTemplate, 
                  routeName, 
                  inlineConstraintResolver, 
                  defaults, 
                  constraints, 
                  dataTokens)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            _target = target;
        }

        public string RouteTemplate => ParsedTemplate.TemplateText;

        protected override Task OnRouteMatched(RouteContext context)
        {
            context.RouteData.Routers.Add(_target);
            return _target.RouteAsync(context);
        }

        protected override VirtualPathData? OnVirtualPathGenerated(VirtualPathContext context)
        {
            return _target.GetVirtualPath(context);
        }
    }
}
