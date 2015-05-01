// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using LoggingWebSite;
using Xunit.Sdk;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public static class LoggingAssert
    {
        /// <summary>
        /// Compares two trees and verifies if the scope nodes are equal
        /// </summary>
        /// <param name="expected"></param>
        /// <param name="actual"></param>
        /// <returns></returns>
        public static bool ScopesEqual(ScopeNodeDto expected, ScopeNodeDto actual)
        {
            // To enable diagnosis, here a flat-list(pe-order traversal based) of 
            // these trees is provided.
            if (!AreScopesEqual(expected, actual))
            {
                var expectedScopes = new List<string>();
                var actualScopes = new List<string>();

                TraverseScopeTree(expected, expectedScopes);
                TraverseScopeTree(actual, actualScopes);

                throw new EqualException(expected: string.Join(", ", expectedScopes),
                                        actual: string.Join(", ", actualScopes));
            }

            return true;
        }

        /// <summary>
        /// Compares two trees and verifies if the scope nodes are equal
        /// </summary>
        /// <param name="root1"></param>
        /// <param name="root2"></param>
        /// <returns></returns>
        private static bool AreScopesEqual(ScopeNodeDto root1, ScopeNodeDto root2)
        {
            if (root1 == null && root2 == null)
            {
                return true;
            }

            if (root1 == null || root2 == null)
            {
                return false;
            }

            if (!string.Equals(root1.State?.ToString(), root2.State?.ToString(), StringComparison.OrdinalIgnoreCase)
                || root1.Children.Count != root2.Children.Count)
            {
                return false;
            }

            bool isChildScopeEqual = true;
            for (int i = 0; i < root1.Children.Count; i++)
            {
                isChildScopeEqual = AreScopesEqual(root1.Children[i], root2.Children[i]);

                if (!isChildScopeEqual)
                {
                    break;
                }
            }

            return isChildScopeEqual;
        }

        /// <summary>
        /// Traverses the scope node sub-tree and collects the list scopes
        /// </summary>
        /// <param name="root"></param>
        /// <param name="scopes"></param>
        private static void TraverseScopeTree(ScopeNodeDto root, List<string> scopes)
        {
            if (root == null)
            {
                return;
            }

            scopes.Add(root.State?.ToString());

            foreach (var childScope in root.Children)
            {
                TraverseScopeTree(childScope, scopes);
            }
        }
    }
}