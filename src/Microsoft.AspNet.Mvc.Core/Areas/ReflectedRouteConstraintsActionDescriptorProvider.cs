// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Microsoft.AspNet.Mvc
{
    public class ReflectedRouteConstraintsActionDescriptorProvider : IActionDescriptorProvider
    {
        public int Order
        {
            get { return ReflectedActionDescriptorProvider.DefaultOrder + 100; }
        }

        public void Invoke([NotNull]ActionDescriptorProviderContext context, Action callNext)
        {
            var removalConstraints = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Iterate all the Reflected Action Descriptor providers and add area or other route constraints
            foreach (var actionDescriptor in context.Results.OfType<ReflectedActionDescriptor>())
            {
                var routeConstraints = actionDescriptor
                    .ControllerDescriptor
                    .ControllerTypeInfo
                    .GetCustomAttributes<RouteConstraintAttribute>()
                    .ToArray();

                foreach (var routeConstraint in routeConstraints)
                {
                    if (routeConstraint.BlockNonAttributedActions)
                    {
                        removalConstraints.Add(routeConstraint.RouteKey);
                    }

                    // Skip duplicates
                    if (!HasConstraint(actionDescriptor, routeConstraint.RouteKey))
                    {
                        actionDescriptor.RouteConstraints.Add(new RouteDataActionConstraint(
                            routeConstraint.RouteKey, routeConstraint.RouteValue));
                    }
                }
            }

            foreach (var actionDescriptor in context.Results.OfType<ReflectedActionDescriptor>())
            {
                foreach (var key in removalConstraints)
                {
                    if (!HasConstraint(actionDescriptor, key))
                    {
                        actionDescriptor.RouteConstraints.Add(new RouteDataActionConstraint(key, RouteKeyHandling.DenyKey));
                    }
                }
            }

            callNext();
        }

        private bool HasConstraint(ActionDescriptor actionDescript, string routeKey)
        {
            return actionDescript.RouteConstraints.Any(rc => string.Equals(rc.RouteKey, routeKey, StringComparison.OrdinalIgnoreCase));
        }
    }
}