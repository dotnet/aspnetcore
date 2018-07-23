// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using Microsoft.AspNetCore.Routing.Matchers;
using Microsoft.Extensions.Logging;

namespace RoutingSample.Web
{
    internal class EndsWithStringMatchProcessor : MatchProcessorBase
    {
        private readonly ILogger<EndsWithStringMatchProcessor> _logger;

        public EndsWithStringMatchProcessor(ILogger<EndsWithStringMatchProcessor> logger)
        {
            _logger = logger;
        }

        public override bool Process(object value)
        {
            if (value == null)
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
