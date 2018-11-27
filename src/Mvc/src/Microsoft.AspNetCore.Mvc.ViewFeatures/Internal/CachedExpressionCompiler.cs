// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures.Internal
{
    public static class CachedExpressionCompiler
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
                if (expression == null)
                {
                    throw new ArgumentNullException(nameof(expression));
                }

                return CompileFromIdentityFunc(expression)
                       ?? CompileFromConstLookup(expression)
                       ?? CompileFromMemberAccess(expression)
                       ?? CompileSlow(expression);
            }

            private static Func<TModel, TResult> CompileFromConstLookup(
                Expression<Func<TModel, TResult>> expression)
            {
                if (expression == null)
                {
                    throw new ArgumentNullException(nameof(expression));
                }

                var constantExpression = expression.Body as ConstantExpression;
                if (constantExpression != null)
                {
                    // model => {const}

                    var constantValue = (TResult)constantExpression.Value;
                    return _ => constantValue;
                }

                return null;
            }

            private static Func<TModel, TResult> CompileFromIdentityFunc(
                Expression<Func<TModel, TResult>> expression)
            {
                if (expression == null)
                {
                    throw new ArgumentNullException(nameof(expression));
                }

                if (expression.Body == expression.Parameters[0])
                {
                    // model => model

                    // Don't need to lock, as all identity funcs are identical.
                    if (_identityFunc == null)
                    {
                        _identityFunc = expression.Compile();
                    }

                    return _identityFunc;
                }

                return null;
            }

            private static Func<TModel, TResult> CompileFromMemberAccess(
                Expression<Func<TModel, TResult>> expression)
            {
                if (expression == null)
                {
                    throw new ArgumentNullException(nameof(expression));
                }

                // Performance tests show that on the x64 platform, special-casing static member and
                // captured local variable accesses is faster than letting the fingerprinting system
                // handle them. On the x86 platform, the fingerprinting system is faster, but only
                // by around one microsecond, so it's not worth it to complicate the logic here with
                // an architecture check.

                var memberExpression = expression.Body as MemberExpression;
                if (memberExpression != null)
                {
                    if (memberExpression.Expression == expression.Parameters[0] || memberExpression.Expression == null)
                    {
                        // model => model.Member or model => StaticMember
                        return _simpleMemberAccessCache.GetOrAdd(memberExpression.Member, _ => expression.Compile());
                    }

                    var constantExpression = memberExpression.Expression as ConstantExpression;
                    if (constantExpression != null)
                    {
                        // model => {const}.Member (captured local variable)
                        var compiledExpression = _constMemberAccessCache.GetOrAdd(memberExpression.Member, _ =>
                        {
                            // rewrite as capturedLocal => ((TDeclaringType)capturedLocal).Member
                            var parameterExpression = Expression.Parameter(typeof(object), "capturedLocal");
                            var castExpression =
                                Expression.Convert(parameterExpression, memberExpression.Member.DeclaringType);
                            var replacementMemberExpression = memberExpression.Update(castExpression);
                            var replacementExpression = Expression.Lambda<Func<object, TResult>>(
                                replacementMemberExpression,
                                parameterExpression);

                            return replacementExpression.Compile();
                        });

                        var capturedLocal = constantExpression.Value;
                        return _ => compiledExpression(capturedLocal);
                    }
                }

                return null;
            }

            private static Func<TModel, TResult> CompileSlow(Expression<Func<TModel, TResult>> expression)
            {
                if (expression == null)
                {
                    throw new ArgumentNullException(nameof(expression));
                }

                // fallback compilation system - just compile the expression directly
                return expression.Compile();
            }
        }
    }
}
