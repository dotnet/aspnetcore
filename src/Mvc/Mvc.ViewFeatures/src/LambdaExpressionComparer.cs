// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures
{
    // This comparer is tightly coupled with the logic of ExpressionHelper.GetExpressionText.
    // It is not designed to accurately compare any two arbitrary LambdaExpressions.
    internal class LambdaExpressionComparer : IEqualityComparer<LambdaExpression>
    {
        public static readonly LambdaExpressionComparer Instance = new LambdaExpressionComparer();

        public bool Equals(LambdaExpression lambdaExpression1, LambdaExpression lambdaExpression2)
        {
            if (ReferenceEquals(lambdaExpression1, lambdaExpression2))
            {
                return true;
            }
            // We will cache only pure member access expressions. Hence we compare two expressions
            // to be equal only if they are identical member access expressions.
            var expression1 = lambdaExpression1.Body;
            var expression2 = lambdaExpression2.Body;

            while (true)
            {
                if (expression1 == null && expression2 == null)
                {
                    return true;
                }

                if (expression1 == null || expression2 == null)
                {
                    return false;
                }

                if (expression1.NodeType != expression2.NodeType)
                {
                    return false;
                }

                switch (expression1.NodeType)
                {
                    case ExpressionType.MemberAccess:
                        var memberExpression1 = (MemberExpression)expression1;
                        var memberName1 = memberExpression1.Member.Name;
                        expression1 = memberExpression1.Expression;

                        var memberExpression2 = (MemberExpression)expression2;
                        var memberName2 = memberExpression2.Member.Name;
                        expression2 = memberExpression2.Expression;

                        // If identifier contains "__", it is "reserved for use by the implementation" and likely
                        // compiler- or Razor-generated e.g. the name of a field in a delegate's generated class.
                        if (memberName1.Contains("__") && memberName2.Contains("__"))
                        {
                            return true;
                        }

                        if (!string.Equals(memberName1, memberName2, StringComparison.Ordinal))
                        {
                            return false;
                        }
                        break;

                    case ExpressionType.ArrayIndex:
                        // Shouldn't be cached. Just in case, ensure indexers are all different.
                        return false;

                    case ExpressionType.Call:
                        // Shouldn't be cached. Just in case, ensure indexers and other calls are all different.
                        return false;

                    default:
                        // Everything else terminates name generation. Haven't found a difference so far...
                        return true;
                }
            }
        }

        public int GetHashCode(LambdaExpression lambdaExpression)
        {
            var expression = lambdaExpression.Body;
            var hashCodeCombiner = HashCodeCombiner.Start();

            while (true)
            {
                if (expression != null && expression.NodeType == ExpressionType.MemberAccess)
                {
                    var memberExpression = (MemberExpression)expression;
                    var memberName = memberExpression.Member.Name;

                    if (memberName.Contains("__"))
                    {
                        break;
                    }

                    hashCodeCombiner.Add(memberName, StringComparer.Ordinal);
                    expression = memberExpression.Expression;
                }
                else
                {
                    break;
                }
            }

            return hashCodeCombiner.CombinedHash;
        }
    }
}
