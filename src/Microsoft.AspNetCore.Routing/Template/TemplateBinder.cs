// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Dispatcher;
using Microsoft.Extensions.ObjectPool;

namespace Microsoft.AspNetCore.Routing.Template
{
    public class TemplateBinder
    {
        private readonly RoutePatternBinder _binder;

        public TemplateBinder(
            UrlEncoder urlEncoder,
            ObjectPool<UriBuildingContext> pool,
            RouteTemplate template,
            RouteValueDictionary defaults)
        {
            if (urlEncoder == null)
            {
                throw new ArgumentNullException(nameof(urlEncoder));
            }

            if (pool == null)
            {
                throw new ArgumentNullException(nameof(pool));
            }

            if (template == null)
            {
                throw new ArgumentNullException(nameof(template));
            }

            _binder = new RoutePatternBinder(urlEncoder, pool, template.ToRoutePattern(), defaults);
        }

        // Step 1: Get the list of values we're going to try to use to match and generate this URI
        public TemplateValuesResult GetValues(RouteValueDictionary ambientValues, RouteValueDictionary values)
        {
            (var acceptedValues, var combinedValues) = _binder.GetValues(ambientValues, values);
            if (acceptedValues == null || combinedValues == null)
            {
                return null;
            }

            return new TemplateValuesResult()
            {
                AcceptedValues = acceptedValues.AsRouteValueDictionary(),
                CombinedValues = combinedValues.AsRouteValueDictionary(),
            };
        }

        // Step 2: If the route is a match generate the appropriate URI
        public string BindValues(RouteValueDictionary acceptedValues)
        {
            return _binder.BindValues(acceptedValues);
        }

        /// <summary>
        /// Compares two objects for equality as parts of a case-insensitive path.
        /// </summary>
        /// <param name="a">An object to compare.</param>
        /// <param name="b">An object to compare.</param>
        /// <returns>True if the object are equal, otherwise false.</returns>
        public static bool RoutePartsEqual(object a, object b)
        {
            var sa = a as string;
            var sb = b as string;

            if (sa != null && sb != null)
            {
                // For strings do a case-insensitive comparison
                return string.Equals(sa, sb, StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                if (a != null && b != null)
                {
                    // Explicitly call .Equals() in case it is overridden in the type
                    return a.Equals(b);
                }
                else
                {
                    // At least one of them is null. Return true if they both are
                    return a == b;
                }
            }
        }
    }
}
