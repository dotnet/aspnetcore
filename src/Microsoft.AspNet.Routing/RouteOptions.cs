// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Routing.Constraints;

namespace Microsoft.AspNet.Routing
{
    public class RouteOptions
    {
        private IDictionary<string, Type> _constraintTypeMap = GetDefaultConstraintMap();

        public IDictionary<string, Type> ConstraintMap
        {
            get
            {
                return _constraintTypeMap;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value",
                                                    Resources.FormatPropertyOfTypeCannotBeNull(
                                                                "ConstraintMap", typeof(RouteOptions)));
                }

                _constraintTypeMap = value;
            }
        }

        private static IDictionary<string, Type> GetDefaultConstraintMap()
        {
            return new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
            {
                // Type-specific constraints
                { "int", typeof(IntRouteConstraint) },
            };
        }
    }
}
