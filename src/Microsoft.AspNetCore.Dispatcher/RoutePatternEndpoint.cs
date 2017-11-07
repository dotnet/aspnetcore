// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Dispatcher
{
    public class RoutePatternEndpoint : Endpoint, IRoutePatternEndpoint
    {
        public RoutePatternEndpoint(string pattern, RequestDelegate requestDelegate, params object[] metadata)
            : this(pattern, (object)null, (string)null, requestDelegate, null, metadata)
        {
        }

        public RoutePatternEndpoint(string pattern, Func<RequestDelegate, RequestDelegate> delegateFactory, params object[] metadata)
            : this(pattern, (object)null, (string)null, delegateFactory, null, metadata)
        {
        }

        public RoutePatternEndpoint(string pattern, object values, RequestDelegate requestDelegate, params object[] metadata)
            : this(pattern, values, null, requestDelegate, null, metadata)
        {
        }

        public RoutePatternEndpoint(string pattern, object values, Func<RequestDelegate, RequestDelegate> delegateFactory, params object[] metadata)
            : this(pattern, values, null, delegateFactory, null, metadata)
        {
        }

        public RoutePatternEndpoint(string pattern, object values, RequestDelegate requestDelegate, string displayName, params object[] metadata)
            : this(pattern, values, null, requestDelegate, displayName, metadata)
        {
        }

        public RoutePatternEndpoint(string pattern, object values, Func<RequestDelegate, RequestDelegate> delegateFactory, string displayName, params object[] metadata)
            : this(pattern, values, null, delegateFactory, displayName, metadata)
        {
        }

        public RoutePatternEndpoint(string pattern, object values, string httpMethod, RequestDelegate requestDelegate, params object[] metadata)
            : this(pattern, values, httpMethod, requestDelegate, null, metadata)
        {
        }

        public RoutePatternEndpoint(string pattern, object values, string httpMethod, Func<RequestDelegate, RequestDelegate> delegateFactory, params object[] metadata)
            : this(pattern, values, httpMethod, delegateFactory, null, metadata)
        {
        }

        public RoutePatternEndpoint(
            string pattern,
            object values,
            string httpMethod,
            RequestDelegate requestDelegate,
            string displayName,
            params object[] metadata)
        {
            if (pattern == null)
            {
                throw new ArgumentNullException(nameof(pattern));
            }

            if (requestDelegate == null)
            {
                throw new ArgumentNullException(nameof(requestDelegate));
            }

            if (metadata == null)
            {
                throw new ArgumentNullException(nameof(metadata));
            }

            Pattern = pattern;
            Values = new DispatcherValueCollection(values);
            HttpMethod = httpMethod;
            HandlerFactory = (next) => requestDelegate;
            DisplayName = displayName;
            Metadata = new MetadataCollection(metadata);
        }

        public RoutePatternEndpoint(
            string pattern,
            object values,
            string httpMethod,
            Func<RequestDelegate, RequestDelegate> delegateFactory,
            string displayName,
            params object[] metadata)
        {
            if (pattern == null)
            {
                throw new ArgumentNullException(nameof(pattern));
            }

            if (delegateFactory == null)
            {
                throw new ArgumentNullException(nameof(delegateFactory));
            }

            if (metadata == null)
            {
                throw new ArgumentNullException(nameof(metadata));
            }

            Pattern = pattern;
            Values = new DispatcherValueCollection(values);
            HttpMethod = httpMethod;
            HandlerFactory = delegateFactory;
            DisplayName = displayName;
            Metadata = new MetadataCollection(metadata);
        }

        public override string DisplayName { get; }

        public string HttpMethod { get; }

        public override MetadataCollection Metadata { get; }

        public Func<RequestDelegate, RequestDelegate> HandlerFactory { get; }

        public string Pattern { get; }

        public DispatcherValueCollection Values { get; }
    }
}
