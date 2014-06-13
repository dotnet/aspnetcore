// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Routing.Constraints;

namespace Microsoft.AspNet.Routing
{
    public class RouteConstraintBuilder
    {
        public static IDictionary<string, IRouteConstraint>
            BuildConstraints(IDictionary<string, object> inputConstraints)
        {
            return BuildConstraintsCore(inputConstraints, routeTemplate: null);
        }

        public static IDictionary<string, IRouteConstraint>
            BuildConstraints(IDictionary<string, object> inputConstraints, [NotNull] string routeTemplate)
        {
            return BuildConstraintsCore(inputConstraints, routeTemplate);
        }

        private static IDictionary<string, IRouteConstraint>
            BuildConstraintsCore(IDictionary<string, object> inputConstraints, string routeTemplate)
        {
            if (inputConstraints == null || inputConstraints.Count == 0)
            {
                return null;
            }

            var constraints = new Dictionary<string, IRouteConstraint>(inputConstraints.Count,
                                                                       StringComparer.OrdinalIgnoreCase);

            foreach (var kvp in inputConstraints)
            {
                var constraint = kvp.Value as IRouteConstraint;

                if (constraint == null)
                {
                    var regexPattern = kvp.Value as string;

                    if (regexPattern == null)
                    {
                        if (routeTemplate != null)
                        {
                            throw new InvalidOperationException(
                                Resources.FormatTemplateRoute_ValidationMustBeStringOrCustomConstraint(
                                    kvp.Key, routeTemplate, typeof(IRouteConstraint)));
                        }
                        else
                        {
                            throw new InvalidOperationException(
                                Resources.FormatGeneralConstraints_ValidationMustBeStringOrCustomConstraint(
                                    kvp.Key, typeof(IRouteConstraint)));
                        }
                    }

                    var constraintsRegEx = "^(" + regexPattern + ")$";

                    constraint = new RegexConstraint(constraintsRegEx);
                }

                constraints.Add(kvp.Key, constraint);
            }

            return constraints;
        }
    }
}
