// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Dispatcher
{
    public class TemplateEndpoint : Endpoint, ITemplateEndpoint
    {
        public TemplateEndpoint(string template, RequestDelegate requestDelegate, params object[] metadata)
            : this(template, (object)null, (string)null, requestDelegate, null, metadata)
        {
        }

        public TemplateEndpoint(string template, Func<RequestDelegate, RequestDelegate> delegateFactory, params object[] metadata)
            : this(template, (object)null, (string)null, delegateFactory, null, metadata)
        {
        }

        public TemplateEndpoint(string template, object values, RequestDelegate requestDelegate, params object[] metadata)
            : this(template, values, null, requestDelegate, null, metadata)
        {
        }

        public TemplateEndpoint(string template, object values, Func<RequestDelegate, RequestDelegate> delegateFactory, params object[] metadata)
            : this(template, values, null, delegateFactory, null, metadata)
        {
        }

        public TemplateEndpoint(string template, object values, RequestDelegate requestDelegate, string displayName, params object[] metadata)
    : this(template, values, null, requestDelegate, displayName, metadata)
        {
        }

        public TemplateEndpoint(string template, object values, Func<RequestDelegate, RequestDelegate> delegateFactory, string displayName, params object[] metadata)
            : this(template, values, null, delegateFactory, displayName, metadata)
        {
        }

        public TemplateEndpoint(string template, object values, string httpMethod, RequestDelegate requestDelegate, params object[] metadata)
            : this(template, values, httpMethod, requestDelegate, null, metadata)
        {
        }

        public TemplateEndpoint(string template, object values, string httpMethod, Func<RequestDelegate, RequestDelegate> delegateFactory, params object[] metadata)
            : this(template, values, httpMethod, delegateFactory, null, metadata)
        {
        }

        public TemplateEndpoint(
            string template,
            object values,
            string httpMethod,
            RequestDelegate requestDelegate,
            string displayName,
            params object[] metadata)
        {
            if (template == null)
            {
                throw new ArgumentNullException(nameof(template));
            }

            if (requestDelegate == null)
            {
                throw new ArgumentNullException(nameof(requestDelegate));
            }

            if (metadata == null)
            {
                throw new ArgumentNullException(nameof(metadata));
            }

            Template = template;
            Values = new DispatcherValueCollection(values);
            HttpMethod = httpMethod;
            HandlerFactory = (next) => requestDelegate;
            DisplayName = displayName;
            Metadata = new MetadataCollection(metadata);
        }

        public TemplateEndpoint(
            string template,
            object values,
            string httpMethod,
            Func<RequestDelegate, RequestDelegate> delegateFactory,
            string displayName,
            params object[] metadata)
        {
            if (template == null)
            {
                throw new ArgumentNullException(nameof(template));
            }

            if (delegateFactory == null)
            {
                throw new ArgumentNullException(nameof(delegateFactory));
            }

            if (metadata == null)
            {
                throw new ArgumentNullException(nameof(metadata));
            }

            Template = template;
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

        public string Template { get; }

        public DispatcherValueCollection Values { get; }
    }
}
