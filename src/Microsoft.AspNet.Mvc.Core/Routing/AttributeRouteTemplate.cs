// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
            if (left == null && right == null)
            {
                return null;
            }
            else if (left == null)
            {
                return right.Trim('/');
            }
            else if (right == null)
            {
                return left.Trim('/');
            }

            // Neither is null
            var trimmedLeft = left.Trim('/');
            var trimmedRight = right.Trim('/');

            if (trimmedLeft == string.Empty)
            {
                return trimmedRight;
            }
            else if (trimmedRight == string.Empty)
            {
                return trimmedLeft;
            }

            // Both templates contain some text.
            return trimmedLeft + '/' + trimmedRight;
        }
    }
}