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
                { "bool", typeof(BoolRouteConstraint) },
                { "datetime", typeof(DateTimeRouteConstraint) },
                { "decimal", typeof(DecimalRouteConstraint) },
                { "double", typeof(DoubleRouteConstraint) },
                { "float", typeof(FloatRouteConstraint) },
                { "guid", typeof(GuidRouteConstraint) },
                { "long", typeof(LongRouteConstraint) },

                // Length constraints
                { "minlength", typeof(MinLengthRouteConstraint) },
                { "maxlength", typeof(MaxLengthRouteConstraint) },
                { "length", typeof(LengthRouteConstraint) },

                // Min/Max value constraints
                { "min", typeof(MinRouteConstraint) },
                { "max", typeof(MaxRouteConstraint) },
                { "range", typeof(RangeRouteConstraint) },

                // Regex-based constraints
                { "alpha", typeof(AlphaRouteConstraint) },
                { "regex", typeof(RegexInlineRouteConstraint) },

                {"required", typeof(RequiredRouteConstraint) },
            };
        }

        /// <summary>
        /// Gets or sets the value that enables best-effort link generation.
        ///
        /// If enabled, link generation will use allow link generation to succeed when the set of values provided
        /// cannot be validated.
        /// </summary>
        public bool UseBestEffortLinkGeneration { get; set; }
    }
}
