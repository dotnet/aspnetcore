// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace InlineConstraintSample.Web.Constraints
{
    public class IsbnDigitScheme13Constraint : IRouteConstraint
    {
        private static readonly int[] _isbn13Weights = new int[] { 1, 3 };

        public bool Match(
            HttpContext httpContext,
            IRouter route,
            string routeKey,
            RouteValueDictionary values,
            RouteDirection routeDirection)
        {
            object value;

            if (!values.TryGetValue(routeKey, out value))
            {
                return false;
            }

            var isbnNumber = value as string;

            if (isbnNumber == null
                || isbnNumber.Length != 13
                || isbnNumber.Any(n => n < '0' || n > '9'))
            {
                return false;
            }

            var sum = 0;
            Func<char, int> convertToInt = (char n) => n - '0';

            for (int i = 0; i < isbnNumber.Length - 1; ++i)
            {
                sum +=
                    convertToInt(isbnNumber[i]) * _isbn13Weights[i % 2];
            }

            var checkSum = 10 - sum % 10;

            if (checkSum == convertToInt(isbnNumber.Last()))
            {
                return true;
            }

            return false;
        }
    }
}