// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using Microsoft.AspNet.Mvc.Core;
using Microsoft.AspNet.Routing;

namespace Microsoft.AspNet.Mvc
{
    public class RouteDataActionConstraint : IActionConstraint
    {
        private IEqualityComparer _comparer;

        private RouteDataActionConstraint(string routeKey)
        {
            if (routeKey == null)
            {
                throw new ArgumentNullException("routeKey");
            }

            RouteKey = routeKey;
            Comparer = StringComparer.OrdinalIgnoreCase; // Is this the right comparer for route values?
        }

        public RouteDataActionConstraint(string routeKey, string routeValue)
            : this(routeKey)
        {
            if (string.IsNullOrEmpty(routeValue))
            {
                throw new ArgumentNullException("routeValue");
            }

            RouteValue = routeValue;
            KeyHandling = RouteKeyHandling.RequireKey;
        }

        public RouteDataActionConstraint(string routeKey, RouteKeyHandling keyHandling)
            : this(routeKey)
        {
            switch (keyHandling)
            {
                case RouteKeyHandling.AcceptAlways:
                case RouteKeyHandling.CatchAll:
                case RouteKeyHandling.DenyKey:
                case RouteKeyHandling.RequireKey:
                    KeyHandling = keyHandling;
                    break;
                default:
#if NET45
                    throw new InvalidEnumArgumentException("keyHandling", (int)keyHandling, typeof (RouteKeyHandling));
#else
                    throw new ArgumentOutOfRangeException("keyHandling");
#endif
            }
        }

        public string RouteKey { get; private set; }
        public string RouteValue { get; private set; }
        public RouteKeyHandling KeyHandling { get; private set; }

        public IEqualityComparer Comparer
        {
            get { return _comparer; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                _comparer = value;
            }
        }

        public bool Accept([NotNull] IDictionary<string, object> routeValues)
        {
            object value;
            switch (KeyHandling)
            {
                case RouteKeyHandling.AcceptAlways:
                    return true;

                case RouteKeyHandling.CatchAll:
                    return routeValues.ContainsKey(RouteKey);

                case RouteKeyHandling.DenyKey:
                    // Routing considers a null or empty string to also be the lack of a value
                    if (!routeValues.TryGetValue(RouteKey, out value) || value == null)
                    {
                        return true;
                    }

                    var stringValue = value as string;
                    if (stringValue != null && stringValue.Length == 0)
                    {
                        return true;
                    }

                    return false;

                case RouteKeyHandling.RequireKey:
                    if (routeValues.TryGetValue(RouteKey, out value))
                    {
                        return Comparer.Equals(value, RouteValue);
                    }
                    else
                    {
                        return false;
                    }

                default:
                    Debug.Fail("Unexpected routeValue");
                    return false;
            }
        }

        public bool Accept([NotNull] RouteContext context)
        {
            var routeValues = context.RouteData.Values;
            if (routeValues == null)
            {
                throw new ArgumentException(Resources.FormatPropertyOfTypeCannotBeNull(
                        "Values",
                        typeof(RouteData)),
                    "context");
            }

            return Accept(routeValues);
        }
    }
}
