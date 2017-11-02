// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Microsoft.AspNetCore.Dispatcher
{
    public class RegexDispatcherValueConstraint : IDispatcherValueConstraint
    {
        private static readonly TimeSpan RegexMatchTimeout = TimeSpan.FromSeconds(10);

        public RegexDispatcherValueConstraint(Regex regex)
        {
            if (regex == null)
            {
                throw new ArgumentNullException(nameof(regex));
            }

            Constraint = regex;
        }

        public RegexDispatcherValueConstraint(string regexPattern)
        {
            if (regexPattern == null)
            {
                throw new ArgumentNullException(nameof(regexPattern));
            }

            Constraint = new Regex(
                regexPattern,
                RegexOptions.CultureInvariant | RegexOptions.IgnoreCase,
                RegexMatchTimeout);
        }

        public Regex Constraint { get; private set; }

        /// <inheritdoc />
        public bool Match(DispatcherValueConstraintContext constraintContext)
        {
            if (constraintContext == null)
            {
                throw new ArgumentNullException(nameof(constraintContext));
            }

            if (constraintContext.Values.TryGetValue(constraintContext.Key, out var routeValue)
                && routeValue != null)
            {
                var parameterValueString = Convert.ToString(routeValue, CultureInfo.InvariantCulture);

                return Constraint.IsMatch(parameterValueString);
            }

            return false;
        }
    }
}
