// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing.Matchers
{
    internal class OptionalMatchProcessor : MatchProcessor
    {
        private readonly MatchProcessor _innerMatchProcessor;

        public OptionalMatchProcessor(MatchProcessor innerMatchProcessor)
        {
            _innerMatchProcessor = innerMatchProcessor;
        }

        public string ParameterName { get; private set; }

        public override void Initialize(string parameterName, string constraintArgument)
        {
            ParameterName = parameterName;
            _innerMatchProcessor.Initialize(parameterName, constraintArgument);
        }

        public override bool ProcessInbound(HttpContext httpContext, RouteValueDictionary values)
        {
            return Process(httpContext, values);
        }

        public override bool ProcessOutbound(HttpContext httpContext, RouteValueDictionary values)
        {
            return Process(httpContext, values);
        }

        private bool Process(HttpContext httpContext, RouteValueDictionary values)
        {
            if (values.TryGetValue(ParameterName, out var value))
            {
                return _innerMatchProcessor.ProcessInbound(httpContext, values);
            }
            return true;
        }
    }
}
