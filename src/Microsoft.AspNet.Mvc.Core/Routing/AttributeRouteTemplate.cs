// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.Mvc.Routing
{
    /// <summary>
    /// Functionality supporting route templates for attribute routes.
    /// </summary>
    public static class AttributeRouteTemplate
    {
        /// <summary>
        /// Combines attribute routing templates.
        /// </summary>
        /// <param name="left">The left template.</param>
        /// <param name="right">The right template.</param>
        /// <returns>A combined template.</returns>
        public static string Combine(string left, string right)
        {
            var result = CombineCore(left, right);
            return CleanTemplate(result);
        }

        private static string CombineCore(string left, string right)
        {
            if (left == null && right == null)
            {
                return null;
            }
            else if (left == null)
            {
                return right;
            }
            else if (right == null)
            {
                return left;
            }

            if (right.StartsWith("~/", StringComparison.OrdinalIgnoreCase) ||
                right.StartsWith("/", StringComparison.OrdinalIgnoreCase) ||
                left.Equals("~/", StringComparison.OrdinalIgnoreCase) ||
                left.Equals("/", StringComparison.OrdinalIgnoreCase))
            {
                return right;
            }

            if (left.EndsWith("/", StringComparison.OrdinalIgnoreCase))
            {
                return left + right;
            }

            // Both templates contain some text.
            return left + '/' + right;
        }

        private static string CleanTemplate(string result)
        {
            if (result == null)
            {
                return null;
            }

            // This is an invalid combined template, so we don't want to
            // accidentally clean it and produce a valid template. For that
            // reason we ignore the clean up process for it.
            if (result.Equals("//", StringComparison.OrdinalIgnoreCase))
            {
                return result;
            }

            var startIndex = 0;
            if (result.StartsWith("/", StringComparison.OrdinalIgnoreCase))
            {
                startIndex = 1;
            }
            else if (result.StartsWith("~/", StringComparison.OrdinalIgnoreCase))
            {
                startIndex = 2;
            }

            // We are in the case where the string is "/" or "~/"
            if (startIndex == result.Length)
            {
                return "";
            }

            var subStringLength = result.Length - startIndex;
            if (result.EndsWith("/", StringComparison.OrdinalIgnoreCase))
            {
                subStringLength--;
            }

            return result.Substring(startIndex, subStringLength);
        }
    }
}