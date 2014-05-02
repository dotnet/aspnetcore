// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

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
                var routeConstraints = actionDescriptor.
                                       ControllerDescriptor.
                                       ControllerTypeInfo.
                                       GetCustomAttributes<RouteConstraintAttribute>().
                                       ToArray();

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