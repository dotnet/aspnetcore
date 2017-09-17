// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Dispatcher
{
    public class SimpleEndpoint : Endpoint
    {
        public SimpleEndpoint(RequestDelegate requestDelegate)
            : this(requestDelegate, Array.Empty<object>(), null)
        {
        }

        public SimpleEndpoint(Func<RequestDelegate, RequestDelegate> delegateFactory)
            : this(delegateFactory, Array.Empty<object>(), null)
        {
        }

        public SimpleEndpoint(RequestDelegate requestDelegate, IEnumerable<object> metadata)
            : this(requestDelegate, metadata, null)
        {
        }

        public SimpleEndpoint(Func<RequestDelegate, RequestDelegate> delegateFactory, IEnumerable<object> metadata)
            : this(delegateFactory, metadata, null)
        {
        }

        public SimpleEndpoint(RequestDelegate requestDelegate, IEnumerable<object> metadata, string displayName)
        {
            if (metadata == null)
            {
                throw new ArgumentNullException(nameof(metadata));
            }

            if (requestDelegate == null)
            {
                throw new ArgumentNullException(nameof(requestDelegate));
            }

            DisplayName = displayName;
            Metadata = metadata.ToArray();
            DelegateFactory = (next) => requestDelegate;
        }

        public SimpleEndpoint(Func<RequestDelegate, RequestDelegate> delegateFactory, IEnumerable<object> metadata, string displayName)
        {
            if (metadata == null)
            {
                throw new ArgumentNullException(nameof(metadata));
            }

            if (delegateFactory == null)
            {
                throw new ArgumentNullException(nameof(delegateFactory));
            }

            DisplayName = displayName;
            Metadata = metadata.ToArray();
            DelegateFactory = delegateFactory;
        }

        public override string DisplayName { get; }

        public override IReadOnlyList<object> Metadata { get; }

        public Func<RequestDelegate, RequestDelegate> DelegateFactory { get; }
    }
}
