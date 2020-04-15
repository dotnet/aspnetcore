// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.AspNetCore.Routing.Constraints;

namespace Microsoft.AspNetCore.Routing
{
    public class RouteOptions
    {
        private IDictionary<string, Type> _constraintTypeMap = GetDefaultConstraintMap();
        private ICollection<EndpointDataSource> _endpointDataSources;

        /// <summary>
        /// Gets a collection of <see cref="EndpointDataSource"/> instances configured with routing.
        /// </summary>
        internal ICollection<EndpointDataSource> EndpointDataSources
        {
            get
            {
                Debug.Assert(_endpointDataSources != null, "Endpoint data sources should have been set in DI.");
                return _endpointDataSources;
            }
            set => _endpointDataSources = value;
        }

        /// <summary>
        /// Gets or sets a value indicating whether all generated paths URLs are lowercase.
        /// Use <see cref="LowercaseQueryStrings" /> to configure the behavior for query strings.
        /// </summary>
        public bool LowercaseUrls { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether a generated query strings are lowercase.
        /// This property will not be used unless <see cref="LowercaseUrls" /> is also <c>true</c>.
        /// </summary>
        public bool LowercaseQueryStrings { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether a trailing slash should be appended to the generated URLs.
        /// </summary>
        public bool AppendTrailingSlash { get; set; }

        /// <summary>
        /// Gets or sets a value that indicates if the check for unhandled security endpoint metadata is suppressed.
        /// <para>
        /// Endpoints can be associated with metadata such as authorization, or CORS, that needs to be
        /// handled by a specific middleware to be actionable. If the middleware is not configured, such
        /// metadata will go unhandled.
        /// </para>
        /// <para>
        /// When <see langword="false"/>, prior to the execution of the endpoint, routing will verify that
        /// all known security-specific metadata has been handled.
        /// Setting this property to <see langword="true"/> suppresses this check.
        /// </para>
        /// </summary>
        /// <value>Defaults to <see langword="false"/>.</value>
        /// <remarks>
        /// This check exists as a safeguard against accidental insecure configuration. You may suppress
        /// this check if it does not match your application's requirements.
        /// </remarks>
        public bool SuppressCheckForUnhandledSecurityMetadata { get; set; }

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
                    throw new ArgumentNullException(nameof(ConstraintMap));
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

                // Files
                { "file", typeof(FileNameRouteConstraint) },
                { "nonfile", typeof(NonFileNameRouteConstraint) },
            };
        }
    }
}
