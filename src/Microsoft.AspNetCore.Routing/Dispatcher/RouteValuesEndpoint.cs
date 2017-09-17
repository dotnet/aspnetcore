// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Dispatcher;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing.Dispatcher
{
    public class RouteValuesEndpoint : Endpoint
    {
        public RouteValuesEndpoint(RouteValueDictionary requiredValues, RequestDelegate requestDelegate)
            : this(requiredValues, requestDelegate, Array.Empty<object>(), null)
        {
        }

        public RouteValuesEndpoint(RouteValueDictionary requiredValues, RequestDelegate requestDelegate, IEnumerable<object> metadata)
            : this(requiredValues, requestDelegate, metadata, null)
        {
        }

        public RouteValuesEndpoint(
            RouteValueDictionary requiredValues,
            RequestDelegate requestDelegate,
            IEnumerable<object> metadata,
            string displayName)
        {
            if (requiredValues == null)
            {
                throw new ArgumentNullException(nameof(requiredValues));
            }

            if (requestDelegate == null)
            {
                throw new ArgumentNullException(nameof(requestDelegate));
            }

            if (metadata == null)
            {
                throw new ArgumentNullException(nameof(metadata));
            }

            RequiredValues = requiredValues;
            HandlerFactory = (next) => requestDelegate;
            Metadata = metadata.ToArray();
            DisplayName = displayName;
        }

        public override string DisplayName { get; }

        public override IReadOnlyList<object> Metadata { get; }

        public Func<RequestDelegate, RequestDelegate> HandlerFactory { get; set; }

        public RouteValueDictionary RequiredValues { get; set; }
    }
}
