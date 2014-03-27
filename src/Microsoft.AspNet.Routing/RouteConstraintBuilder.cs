using System;
using System.Collections.Generic;

namespace Microsoft.AspNet.Routing
{
    public class RouteConstraintBuilder
    {
        public static IDictionary<string, IRouteConstraint> 
            BuildConstraints(IDictionary<string, object> inputConstraints)
        {
            if (inputConstraints == null || inputConstraints.Count == 0)
            {
                return null;
            }

            var constraints = new Dictionary<string, IRouteConstraint>(inputConstraints.Count, StringComparer.OrdinalIgnoreCase);

            foreach (var kvp in inputConstraints)
            {
                var constraint = kvp.Value as IRouteConstraint;

                if (constraint == null)
                {
                    var regexPattern = kvp.Value as string;

                    if (regexPattern == null)
                    {
                        throw new InvalidOperationException("Constraint can be a valid regex string or an IRouteConstraint");
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
