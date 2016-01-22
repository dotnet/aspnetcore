// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace InlineConstraintSample.Web.Constraints
{
    public class IsbnDigitScheme10Constraint : IRouteConstraint
    {
        private readonly bool _allowDashes;

        public IsbnDigitScheme10Constraint(bool allowDashes)
        {
            _allowDashes = allowDashes;
        }

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

            var inputString = value as string;
            string isbnNumber;

            if (inputString == null
                || !TryGetIsbn10(inputString, _allowDashes, out isbnNumber))
            {
                return false;
            }

            var sum = 0;
            Func<char, int> convertToInt = (char n) => n - '0';

            for (int i = 0; i < isbnNumber.Length - 1; ++i)
            {
                sum += convertToInt(isbnNumber[i]) * (i + 1);
            }

            var checkSum = sum % 11;
            var lastDigit = isbnNumber.Last();

            if (checkSum == 10)
            {
                return char.ToUpperInvariant(lastDigit) == 'X';
            }
            else
            {
                return checkSum == convertToInt(lastDigit);
            }
        }

        private static bool TryGetIsbn10(string value, bool allowDashes, out string isbnNumber)
        {
            if (!allowDashes)
            {
                if (CheckIsbn10Characters(value))
                {
                    isbnNumber = value;
                    return true;
                }
                else
                {
                    isbnNumber = null;
                    return false;
                }
            }

            var isbnParts = value.Split(
                new char[] { '-' },
                StringSplitOptions.RemoveEmptyEntries);

            if (isbnParts.Length == 4)
            {
                value = value.Replace("-", string.Empty);
                if (CheckIsbn10Characters(value))
                {
                    isbnNumber = value;
                    return true;
                }
            }

            isbnNumber = null;
            return false;
        }

        private static bool CheckIsbn10Characters(string value)
        {
            if (value.Length != 10)
            {
                return false;
            }

            var digits = value.Substring(0, 9);
            var checksum = value.Last();

            return digits.All(n => '0' <= n && n <= '9')
                && ('0' <= checksum && checksum <= '9'
                    || 'X' == char.ToUpperInvariant(checksum));
        }
    }
}