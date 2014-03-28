// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace System.Web.Mvc.ExpressionUtil
{
    internal static class CachedExpressionCompiler
    {
        // This is the entry point to the cached expression compilation system. The system
        // will try to turn the expression into an actual delegate as quickly as possible,
        // relying on cache lookups and other techniques to save time if appropriate.
        // If the provided expression is particularly obscure and the system doesn't know
        // how to handle it, we'll just compile the expression as normal.
        public static Func<TModel, TValue> Process<TModel, TValue>(Expression<Func<TModel, TValue>> lambdaExpression)
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

            private static readonly ConcurrentDictionary<ExpressionFingerprintChain, Hoisted<TIn, TOut>> _fingerprintedCache =
                new ConcurrentDictionary<ExpressionFingerprintChain, Hoisted<TIn, TOut>>();

            public static Func<TIn, TOut> Compile(Expression<Func<TIn, TOut>> expr)
            {
                return CompileFromIdentityFunc(expr)
                       ?? CompileFromConstLookup(expr)
                       ?? CompileFromMemberAccess(expr)
                       ?? CompileFromFingerprint(expr)
                       ?? CompileSlow(expr);
            }

            private static Func<TIn, TOut> CompileFromConstLookup(Expression<Func<TIn, TOut>> expr)
            {
                ConstantExpression constExpr = expr.Body as ConstantExpression;
                if (constExpr != null)
                {
                    // model => {const}

                    TOut constantValue = (TOut)constExpr.Value;
                    return _ => constantValue;
                }

                return null;
            }

            private static Func<TIn, TOut> CompileFromIdentityFunc(Expression<Func<TIn, TOut>> expr)
            {
                if (expr.Body == expr.Parameters[0])
                {
                    // model => model

                    // don't need to lock, as all identity funcs are identical
                    if (_identityFunc == null)
                    {
                        _identityFunc = expr.Compile();
                    }

                    return _identityFunc;
                }

                return null;
            }

            private static Func<TIn, TOut> CompileFromFingerprint(Expression<Func<TIn, TOut>> expr)
            {
                List<object> capturedConstants;
                ExpressionFingerprintChain fingerprint = FingerprintingExpressionVisitor.GetFingerprintChain(expr, out capturedConstants);

                if (fingerprint != null)
                {
                    var del = _fingerprintedCache.GetOrAdd(fingerprint, _ =>
                    {
                        // Fingerprinting succeeded, but there was a cache miss. Rewrite the expression
                        // and add the rewritten expression to the cache.

                        var hoistedExpr = HoistingExpressionVisitor<TIn, TOut>.Hoist(expr);
                        return hoistedExpr.Compile();
                    });
                    return model => del(model, capturedConstants);
                }

                // couldn't be fingerprinted
                return null;
            }

            private static Func<TIn, TOut> CompileFromMemberAccess(Expression<Func<TIn, TOut>> expr)
            {
                // Performance tests show that on the x64 platform, special-casing static member and
                // captured local variable accesses is faster than letting the fingerprinting system
                // handle them. On the x86 platform, the fingerprinting system is faster, but only
                // by around one microsecond, so it's not worth it to complicate the logic here with
                // an architecture check.

                MemberExpression memberExpr = expr.Body as MemberExpression;
                if (memberExpr != null)
                {
                    if (memberExpr.Expression == expr.Parameters[0] || memberExpr.Expression == null)
                    {
                        // model => model.Member or model => StaticMember
                        return _simpleMemberAccessDict.GetOrAdd(memberExpr.Member, _ => expr.Compile());
                    }

                    ConstantExpression constExpr = memberExpr.Expression as ConstantExpression;
                    if (constExpr != null)
                    {
                        // model => {const}.Member (captured local variable)
                        var del = _constMemberAccessDict.GetOrAdd(memberExpr.Member, _ =>
                        {
                            // rewrite as capturedLocal => ((TDeclaringType)capturedLocal).Member
                            var constParamExpr = Expression.Parameter(typeof(object), "capturedLocal");
                            var constCastExpr = Expression.Convert(constParamExpr, memberExpr.Member.DeclaringType);
                            var newMemberAccessExpr = memberExpr.Update(constCastExpr);
                            var newLambdaExpr = Expression.Lambda<Func<object, TOut>>(newMemberAccessExpr, constParamExpr);
                            return newLambdaExpr.Compile();
                        });

                        object capturedLocal = constExpr.Value;
                        return _ => del(capturedLocal);
                    }
                }

                return null;
            }

            private static Func<TIn, TOut> CompileSlow(Expression<Func<TIn, TOut>> expr)
            {
                // fallback compilation system - just compile the expression directly
                return expr.Compile();
            }
        }
    }
}
