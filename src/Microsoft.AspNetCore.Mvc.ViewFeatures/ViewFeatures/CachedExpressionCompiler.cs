// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures
{
    internal static class CachedExpressionCompiler
    {
        // This is the entry point to the cached expression compilation system. The system
        // will try to turn the expression into an actual delegate as quickly as possible,
        // relying on cache lookups and other techniques to save time if appropriate.
        // If the provided expression is particularly obscure and the system doesn't know
        // how to handle it, we'll just compile the expression as normal.
        public static Func<TModel, TResult> Process<TModel, TResult>(
            Expression<Func<TModel, TResult>> expression)
        {
            if (expression == null)
            {
                throw new ArgumentNullException(nameof(expression));
            }

            return Compiler<TModel, TResult>.Compile(expression);
        }

        private static class Compiler<TModel, TResult>
        {
            private static Func<TModel, TResult> _identityFunc;

            private static readonly ConcurrentDictionary<MemberInfo, Func<TModel, TResult>> _simpleMemberAccessCache =
                new ConcurrentDictionary<MemberInfo, Func<TModel, TResult>>();

            private static readonly ConcurrentDictionary<MemberInfo, Func<object, TResult>> _constMemberAccessCache =
                new ConcurrentDictionary<MemberInfo, Func<object, TResult>>();

            public static Func<TModel, TResult> Compile(Expression<Func<TModel, TResult>> expression)
            {
                Debug.Assert(expression != null);

                switch (expression.Body)
                {
                    // model => model
                    case var body when body == expression.Parameters[0]:
                        return CompileFromIdentityFunc(expression);

                    // model => (object){const}
                    case ConstantExpression constantExpression:
                        return CompileFromConstLookup(constantExpression);

                    // model => CapturedConstant
                    case MemberExpression memberExpression when memberExpression.Expression is ConstantExpression constantExpression:
                        return CompileCapturedConstant(memberExpression, constantExpression);

                    // model => StaticMember
                    case MemberExpression memberExpression when memberExpression.Expression == null:
                        return CompileFromStaticMemberAccess(expression, memberExpression);

                    // model => model.Member
                    case MemberExpression memberExpression when memberExpression.Expression == expression.Parameters[0]:
                        return CompileFromMemberAccess(expression, memberExpression);

                    default:
                        return CompileSlow(expression);
                }
            }

            private static Func<TModel, TResult> CompileFromConstLookup(
                ConstantExpression constantExpression)
            {
                // model => {const}
                var constantValue = (TResult)constantExpression.Value;
                return _ => constantValue;
            }

            private static Func<TModel, TResult> CompileFromIdentityFunc(
                Expression<Func<TModel, TResult>> expression)
            {
                // model => model

                // Don't need to lock, as all identity funcs are identical.
                if (_identityFunc == null)
                {
                    _identityFunc = expression.Compile();
                }

                return _identityFunc;
            }

            private static Func<TModel, TResult> CompileFromMemberAccess(
                Expression<Func<TModel, TResult>> expression,
                MemberExpression memberExpression)
            {
                // model => model.Member
                if (_simpleMemberAccessCache.TryGetValue(memberExpression.Member, out var result))
                {
                    return result;
                }

                result = expression.Compile();
                result = _simpleMemberAccessCache.GetOrAdd(memberExpression.Member, result);
                return result;
            }

            private static Func<TModel, TResult> CompileFromStaticMemberAccess(
                Expression<Func<TModel, TResult>> expression,
                MemberExpression memberExpression)
            {
                // model => model.StaticMember
                if (_simpleMemberAccessCache.TryGetValue(memberExpression.Member, out var result))
                {
                    return result;
                }

                result = expression.Compile();
                result = _simpleMemberAccessCache.GetOrAdd(memberExpression.Member, result);
                return result;
            }

            private static Func<TModel, TResult> CompileCapturedConstant(MemberExpression memberExpression, ConstantExpression constantExpression)
            {
                // model => {const}.Member (captured local variable)
                if (!_constMemberAccessCache.TryGetValue(memberExpression.Member, out var result))
                {
                    // rewrite as capturedLocal => ((TDeclaringType)capturedLocal).Member
                    var parameterExpression = Expression.Parameter(typeof(object), "capturedLocal");
                    var castExpression =
                        Expression.Convert(parameterExpression, memberExpression.Member.DeclaringType);
                    var replacementMemberExpression = memberExpression.Update(castExpression);
                    var replacementExpression = Expression.Lambda<Func<object, TResult>>(
                        replacementMemberExpression,
                        parameterExpression);

                    result = replacementExpression.Compile();
                    result = _constMemberAccessCache.GetOrAdd(memberExpression.Member, result);
                }

                var capturedLocal = constantExpression.Value;
                return _ => result(capturedLocal);
            }

            private static Func<TModel, TResult> CompileSlow(Expression<Func<TModel, TResult>> expression)
            {
                // fallback compilation system - just compile the expression directly
                return expression.Compile();
            }
        }
    }
}
