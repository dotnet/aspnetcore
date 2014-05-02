// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using Microsoft.AspNet.Mvc.Core;

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

        public bool Accept([NotNull] RequestContext context)
        {
            var routeValues = context.RouteValues;
            if (routeValues == null)
            {
                throw new ArgumentException(Resources.FormatPropertyOfTypeCannotBeNull(
                        "RouteValues", 
                        typeof(RequestContext)), 
                    "context");
            }

            return Accept(routeValues);
        }   
    }
}
