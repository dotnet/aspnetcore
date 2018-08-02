// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing.Matching
{
    public abstract class MatchProcessorBase : MatchProcessor
    {
        public string ParameterName { get; private set; }

        public string ConstraintArgument { get; private set; }

        public override void Initialize(string parameterName, string constraintArgument)
        {
            ParameterName = parameterName;
            ConstraintArgument = constraintArgument;
        }

        public abstract bool Process(object value);

        public override bool ProcessInbound(HttpContext httpContext, RouteValueDictionary values)
        {
            return Process(values);
        }

        public override bool ProcessOutbound(HttpContext httpContext, RouteValueDictionary values)
        {
            return Process(values);
        }

        private bool Process(RouteValueDictionary values)
        {
            if (!values.TryGetValue(ParameterName, out var value) || value == null)
            {
                return false;
            }

            return Process(value);
        }
    }
}
