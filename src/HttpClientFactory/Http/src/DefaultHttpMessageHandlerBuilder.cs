// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net.Http;

namespace Microsoft.Extensions.Http
{
    internal class DefaultHttpMessageHandlerBuilder : HttpMessageHandlerBuilder
    {
        public DefaultHttpMessageHandlerBuilder(IServiceProvider services)
        {
            Services = services;
        }

        private string _name;

        public override string Name
        {
            get => _name;
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                _name = value;
            }
        }

        public override HttpMessageHandler PrimaryHandler { get; set; } = new HttpClientHandler();

        public override IList<DelegatingHandler> AdditionalHandlers { get; } = new List<DelegatingHandler>();

        public override IServiceProvider Services { get; }

        public override HttpMessageHandler Build()
        {
            if (PrimaryHandler == null)
            {
                var message = Resources.FormatHttpMessageHandlerBuilder_PrimaryHandlerIsNull(nameof(PrimaryHandler));
                throw new InvalidOperationException(message);
            }
            
            return CreateHandlerPipeline(PrimaryHandler, AdditionalHandlers);
        }
    }
}
