using System;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;

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

        public bool Accept(RequestContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            var routeValues = context.RouteValues;

            if (routeValues == null)
            {
                throw new ArgumentException("Need route values", "context");
            }

            switch (KeyHandling)
            {
                case RouteKeyHandling.AcceptAlways:
                    return true;
                case RouteKeyHandling.DenyKey:
                    return !routeValues.ContainsKey(RouteKey);
                case RouteKeyHandling.CatchAll:
                    return routeValues.ContainsKey(RouteKey);
            }

            Debug.Assert(KeyHandling == RouteKeyHandling.RequireKey, "Unexpected routeValue");

            object value;
            if (routeValues.TryGetValue(RouteKey, out value))
            {
                return Comparer.Equals(value, RouteValue);
            }
            else
            {
                return false;
            }
        }   
    }
}
