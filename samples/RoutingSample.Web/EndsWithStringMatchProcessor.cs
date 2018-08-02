// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Matching;
using Microsoft.Extensions.Logging;

namespace RoutingSample.Web
{
    internal class EndsWithStringMatchProcessor : MatchProcessor
    {
        private readonly ILogger<EndsWithStringMatchProcessor> _logger;

        public EndsWithStringMatchProcessor(ILogger<EndsWithStringMatchProcessor> logger)
        {
            _logger = logger;
        }

        public string ParameterName { get; private set; }

        public string ConstraintArgument { get; private set; }

        public override void Initialize(string parameterName, string constraintArgument)
        {
            ParameterName = parameterName;
            ConstraintArgument = constraintArgument;
        }

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

            var valueString = Convert.ToString(value, CultureInfo.InvariantCulture);
            var endsWith = valueString.EndsWith(ConstraintArgument, StringComparison.OrdinalIgnoreCase);

            if (!endsWith)
            {
                _logger.LogDebug(
                    $"Parameter '{ParameterName}' with value '{valueString}' does not end with '{ConstraintArgument}'.");
            }

            return endsWith;
        }
    }
}
