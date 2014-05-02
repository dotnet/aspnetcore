// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace Microsoft.AspNet.Mvc.Rendering.Expressions
{
    public static class CachedExpressionCompiler
    {
        // This is the entry point to the cached expression compilation system. The system
        // will try to turn the expression into an actual delegate as quickly as possible,
        // relying on cache lookups and other techniques to save time if appropriate.
        // If the provided expression is particularly obscure and the system doesn't know
        // how to handle it, we'll just compile the expression as normal.
        public static Func<TModel, TValue> Process<TModel, TValue>(
            [NotNull] Expression<Func<TModel, TValue>> lambdaExpression)
        {
            return Compiler<TModel, TValue>.Compile(lambdaExpression);
        }

        private static class Compiler<TIn, TOut>
        {
            private static Func<TIn, TOut> _identityFunc;

            private static readonly ConcurrentDictionary<MemberInfo, Func<TIn, TOut>> _simpleMemberAccessDict =
                new ConcurrentDictionary<MemberInfo, Func<TIn, TOut>>();

            private static readonly ConcurrentDictionary<MemberInfo, Func<object, TOut>> _constMemberAccessDict =
                new ConcurrentDictionary<MemberInfo, Func<object, TOut>>();

            public static Func<TIn, TOut> Compile([NotNull] Expression<Func<TIn, TOut>> expr)
            {
                return CompileFromIdentityFunc(expr)
                       ?? CompileFromConstLookup(expr)
                       ?? CompileFromMemberAccess(expr)
                       ?? CompileSlow(expr);
            }

            private static Func<TIn, TOut> CompileFromConstLookup([NotNull] Expression<Func<TIn, TOut>> expr)
            {
                var constExpr = expr.Body as ConstantExpression;
                if (constExpr != null)
                {
                    // model => {const}

                    var constantValue = (TOut)constExpr.Value;
                    return _ => constantValue;
                }

                return null;
            }

            private static Func<TIn, TOut> CompileFromIdentityFunc([NotNull] Expression<Func<TIn, TOut>> expr)
            {
                if (expr.Body == expr.Parameters[0])
                {
                    // model => model

                    // Don't need to lock, as all identity funcs are identical.
                    if (_identityFunc == null)
                    {
                        _identityFunc = expr.Compile();
                    }

                    return _identityFunc;
                }

                return null;
            }

            private static Func<TIn, TOut> CompileFromMemberAccess([NotNull] Expression<Func<TIn, TOut>> expr)
            {
                // Performance tests show that on the x64 platform, special-casing static member and
                // captured local variable accesses is faster than letting the fingerprinting system
                // handle them. On the x86 platform, the fingerprinting system is faster, but only
                // by around one microsecond, so it's not worth it to complicate the logic here with
                // an architecture check.

                var memberExpr = expr.Body as MemberExpression;
                if (memberExpr != null)
                {
                    if (memberExpr.Expression == expr.Parameters[0] || memberExpr.Expression == null)
                    {
                        // model => model.Member or model => StaticMember
                        return _simpleMemberAccessDict.GetOrAdd(memberExpr.Member, _ => expr.Compile());
                    }

                    var constExpr = memberExpr.Expression as ConstantExpression;
                    if (constExpr != null)
                    {
                        // model => {const}.Member (captured local variable)
                        var del = _constMemberAccessDict.GetOrAdd(memberExpr.Member, _ =>
                        {
                            // rewrite as capturedLocal => ((TDeclaringType)capturedLocal).Member
                            var constParamExpr = Expression.Parameter(typeof(object), "capturedLocal");
                            var constCastExpr = Expression.Convert(constParamExpr, memberExpr.Member.DeclaringType);
                            var newMemberAccessExpr = memberExpr.Update(constCastExpr);
                            var newLambdaExpr =
                                Expression.Lambda<Func<object, TOut>>(newMemberAccessExpr, constParamExpr);
                            return newLambdaExpr.Compile();
                        });

                        var capturedLocal = constExpr.Value;
                        return _ => del(capturedLocal);
                    }
                }

                return null;
            }

            private static Func<TIn, TOut> CompileSlow([NotNull] Expression<Func<TIn, TOut>> expr)
            {
                // fallback compilation system - just compile the expression directly
                return expr.Compile();
            }
        }
    }
}
