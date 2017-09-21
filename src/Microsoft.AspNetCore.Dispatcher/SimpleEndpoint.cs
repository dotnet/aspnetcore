// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Dispatcher
{
    public class SimpleEndpoint : Endpoint, IDispatcherValueSelectableEndpoint
    {
        public SimpleEndpoint(RequestDelegate requestDelegate)
            : this(requestDelegate, Array.Empty<object>(), null, null)
        {
        }

        public SimpleEndpoint(Func<RequestDelegate, RequestDelegate> delegateFactory)
            : this(delegateFactory, Array.Empty<object>(), null, null)
        {
        }

        public SimpleEndpoint(RequestDelegate requestDelegate, IEnumerable<object> metadata)
            : this(requestDelegate, metadata, null, null)
        {
        }

        public SimpleEndpoint(Func<RequestDelegate, RequestDelegate> delegateFactory, IEnumerable<object> metadata)
            : this(delegateFactory, metadata, null, null)
        {
        }

        public SimpleEndpoint(Func<RequestDelegate, RequestDelegate> delegateFactory, IEnumerable<object> metadata, object values)
            : this(delegateFactory, metadata, null, null)
        {
        }

        public SimpleEndpoint(RequestDelegate requestDelegate, IEnumerable<object> metadata, object values, string displayName)
        {
            if (metadata == null)
            {
                throw new ArgumentNullException(nameof(metadata));
            }

            if (requestDelegate == null)
            {
                throw new ArgumentNullException(nameof(requestDelegate));
            }

            HandlerFactory = (next) => requestDelegate;
            Metadata = metadata.ToArray();
            Values = new DispatcherValueCollection(values);
            DisplayName = displayName;
        }

        public SimpleEndpoint(Func<RequestDelegate, RequestDelegate> delegateFactory, IEnumerable<object> metadata, object values, string displayName)
        {
            if (metadata == null)
            {
                throw new ArgumentNullException(nameof(metadata));
            }

            if (delegateFactory == null)
            {
                throw new ArgumentNullException(nameof(delegateFactory));
            }

            HandlerFactory = delegateFactory;
            Metadata = metadata.ToArray();
            Values = new DispatcherValueCollection(values);
            DisplayName = displayName;
        }

        public override string DisplayName { get; }

        public override IReadOnlyList<object> Metadata { get; }

        public Func<RequestDelegate, RequestDelegate> HandlerFactory { get; }

        public DispatcherValueCollection Values { get; }
    }
}
