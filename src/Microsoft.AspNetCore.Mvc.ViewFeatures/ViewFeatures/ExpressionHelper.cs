// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Microsoft.AspNet.Mvc.ViewFeatures
{
    public static class ExpressionHelper
    {
        public static string GetExpressionText(string expression)
        {
            // If it's exactly "model", then give them an empty string, to replicate the lambda behavior.
            return string.Equals(expression, "model", StringComparison.OrdinalIgnoreCase) ? string.Empty : expression;
        }

        public static string GetExpressionText(LambdaExpression expression)
        {
            if (expression == null)
            {
                throw new ArgumentNullException(nameof(expression));
            }

            // Split apart the expression string for property/field accessors to create its name
            var nameParts = new Stack<string>();
            var part = expression.Body;

            while (part != null)
            {
                if (part.NodeType == ExpressionType.Call)
                {
                    var methodExpression = (MethodCallExpression)part;
                    if (!IsSingleArgumentIndexer(methodExpression))
                    {
                        // Unsupported.
                        break;
                    }

                    nameParts.Push(
                        GetIndexerInvocation(
                            methodExpression.Arguments.Single(),
                            expression.Parameters.ToArray()));

                    part = methodExpression.Object;
                }
                else if (part.NodeType == ExpressionType.ArrayIndex)
                {
                    var binaryExpression = (BinaryExpression)part;

                    nameParts.Push(
                        GetIndexerInvocation(
                            binaryExpression.Right,
                            expression.Parameters.ToArray()));

                    part = binaryExpression.Left;
                }
                else if (part.NodeType == ExpressionType.MemberAccess)
                {
                    var memberExpressionPart = (MemberExpression)part;
                    var name = memberExpressionPart.Member.Name;

                    // If identifier contains "__", it is "reserved for use by the implementation" and likely compiler-
                    // or Razor-generated e.g. the name of a field in a delegate's generated class.
                    if (name.Contains("__"))
                    {
                        // Exit loop. Should have the entire name because previous MemberAccess has same name as the
                        // leftmost expression node (a variable).
                        break;
                    }

                    nameParts.Push("." + name);
                    part = memberExpressionPart.Expression;
                }
                else if (part.NodeType == ExpressionType.Parameter)
                {
                    // When the expression is parameter based (m => m.Something...), we'll push an empty
                    // string onto the stack and stop evaluating. The extra empty string makes sure that
                    // we don't accidentally cut off too much of m => m.Model.
                    nameParts.Push(string.Empty);

                    // Exit loop. Have the entire name because the parameter cannot be used as an indexer; always the
                    // leftmost expression node.
                    break;
                }
                else
                {
                    // Unsupported.
                    break;
                }
            }

            // If parts start with "model", then strip that part away.
            if (nameParts.Count > 0 && string.Equals(nameParts.Peek(), ".model", StringComparison.OrdinalIgnoreCase))
            {
                nameParts.Pop();
            }

            if (nameParts.Count > 0)
            {
                return nameParts.Aggregate((left, right) => left + right).TrimStart('.');
            }

            return string.Empty;
        }

        private static string GetIndexerInvocation(
            Expression expression,
            ParameterExpression[] parameters)
        {
            if (expression == null)
            {
                throw new ArgumentNullException(nameof(expression));
            }

            if (parameters == null)
            {
                throw new ArgumentNullException(nameof(parameters));
            }

            var converted = Expression.Convert(expression, typeof(object));
            var fakeParameter = Expression.Parameter(typeof(object), null);
            var lambda = Expression.Lambda<Func<object, object>>(converted, fakeParameter);
            Func<object, object> func;

            try
            {
                func = CachedExpressionCompiler.Process(lambda);
            }
            catch (InvalidOperationException ex)
            {
                throw new InvalidOperationException(
                    Resources.FormatExpressionHelper_InvalidIndexerExpression(expression, parameters[0].Name),
                    ex);
            }

            return "[" + Convert.ToString(func(null), CultureInfo.InvariantCulture) + "]";
        }

        public static bool IsSingleArgumentIndexer(Expression expression)
        {
            var methodExpression = expression as MethodCallExpression;
            if (methodExpression == null || methodExpression.Arguments.Count != 1)
            {
                return false;
            }

            // Check whether GetDefaultMembers() (if present in CoreCLR) would return a member of this type. Compiler
            // names the indexer property, if any, in a generated [DefaultMember] attribute for the containing type.
            var declaringType = methodExpression.Method.DeclaringType;
            var defaultMember = declaringType.GetTypeInfo().GetCustomAttribute<DefaultMemberAttribute>(inherit: true);
            if (defaultMember == null)
            {
                return false;
            }

            // Find default property (the indexer) and confirm its getter is the method in this expression.
            return declaringType.GetRuntimeProperties().Any(
                property => (string.Equals(defaultMember.MemberName, property.Name, StringComparison.Ordinal) &&
                    property.GetMethod == methodExpression.Method));
        }
    }
}
