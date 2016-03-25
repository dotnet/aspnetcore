// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures.Internal
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
            return GetExpressionText(expression, expressionTextCache: null);
        }

        public static string GetExpressionText(LambdaExpression expression, ExpressionTextCache expressionTextCache)
        {
            if (expression == null)
            {
                throw new ArgumentNullException(nameof(expression));
            }

            string expressionText;
            if (expressionTextCache != null &&
                expressionTextCache.Entries.TryGetValue(expression, out expressionText))
            {
                return expressionText;
            }

            var containsIndexers = false;
            var part = expression.Body;

            // Builder to concatenate the names for property/field accessors within an expression to create a string.
            var builder = new StringBuilder();

            while (part != null)
            {
                if (part.NodeType == ExpressionType.Call)
                {
                    containsIndexers = true;
                    var methodExpression = (MethodCallExpression)part;
                    if (!IsSingleArgumentIndexer(methodExpression))
                    {
                        // Unsupported.
                        break;
                    }

                    InsertIndexerInvocationText(
                        builder,
                        methodExpression.Arguments.Single(),
                        expression);

                    part = methodExpression.Object;
                }
                else if (part.NodeType == ExpressionType.ArrayIndex)
                {
                    containsIndexers = true;
                    var binaryExpression = (BinaryExpression)part;

                    InsertIndexerInvocationText(
                        builder,
                        binaryExpression.Right,
                        expression);

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

                    builder.Insert(0, name);
                    builder.Insert(0, '.');
                    part = memberExpressionPart.Expression;
                }
                else
                {
                    break;
                }
            }

            // If parts start with "model", then strip that part away.
            if (part == null || part.NodeType != ExpressionType.Parameter)
            {
                var text = builder.ToString();
                if (text.StartsWith(".model", StringComparison.OrdinalIgnoreCase))
                {
                    // 6 is the length of the string ".model".
                    builder.Remove(0, 6);
                }
            }

            if (builder.Length > 0)
            {
                // Trim the leading "." if present.
                builder.Replace(".", string.Empty, 0, 1);
            }

            expressionText = builder.ToString();

            if (expressionTextCache != null && !containsIndexers)
            {
                expressionTextCache.Entries.TryAdd(expression, expressionText);
            }

            return expressionText;
        }

        private static void InsertIndexerInvocationText(
            StringBuilder builder,
            Expression indexExpression,
            LambdaExpression parentExpression)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (indexExpression == null)
            {
                throw new ArgumentNullException(nameof(indexExpression));
            }

            if (parentExpression == null)
            {
                throw new ArgumentNullException(nameof(parentExpression));
            }

            if (parentExpression.Parameters == null)
            {
                throw new ArgumentException(Resources.FormatPropertyOfTypeCannotBeNull(
                    nameof(parentExpression.Parameters),
                    nameof(parentExpression)));
            }

            var converted = Expression.Convert(indexExpression, typeof(object));
            var fakeParameter = Expression.Parameter(typeof(object), null);
            var lambda = Expression.Lambda<Func<object, object>>(converted, fakeParameter);
            Func<object, object> func;

            try
            {
                func = CachedExpressionCompiler.Process(lambda);
            }
            catch (InvalidOperationException ex)
            {
                var parameters = parentExpression.Parameters.ToArray();
                throw new InvalidOperationException(
                    Resources.FormatExpressionHelper_InvalidIndexerExpression(indexExpression, parameters[0].Name),
                    ex);
            }

            builder.Insert(0, ']');
            builder.Insert(0, Convert.ToString(func(null), CultureInfo.InvariantCulture));
            builder.Insert(0, '[');
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
            var runtimeProperties = declaringType.GetRuntimeProperties();
            foreach (var property in runtimeProperties)
            {
                if ((string.Equals(defaultMember.MemberName, property.Name, StringComparison.Ordinal) &&
                    property.GetMethod == methodExpression.Method))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
