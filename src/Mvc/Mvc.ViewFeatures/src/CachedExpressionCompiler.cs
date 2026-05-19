// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures;

internal static class CachedExpressionCompiler
{
    private static readonly Expression NullExpression = Expression.Constant(value: null);

    /// <remarks>
    /// This is the entry point to the expression compilation system. The system
    /// a) Will rewrite the expression to avoid null refs when any part of the expression tree is evaluated  to null
    /// b) Attempt to cache the result, or an intermediate part of the result.
    /// If the provided expression is particularly obscure and the system doesn't know how to handle it, it will
    /// return null.
    /// </remarks>
    public static Func<TModel, object> Process<TModel, TResult>(
        Expression<Func<TModel, TResult>> expression)
    {
        ArgumentNullException.ThrowIfNull(expression);

        return Compiler<TModel, TResult>.Compile(expression);
    }

    private static class Compiler<TModel, TResult>
    {
        private static Func<TModel, object> _identityFunc;

        private static readonly ConcurrentDictionary<MemberInfo, Func<TModel, object>> _simpleMemberAccessCache =
            new ConcurrentDictionary<MemberInfo, Func<TModel, object>>();

        private static readonly ConcurrentDictionary<MemberExpressionCacheKey, Func<TModel, object>> _chainedMemberAccessCache =
            new ConcurrentDictionary<MemberExpressionCacheKey, Func<TModel, object>>(MemberExpressionCacheKeyComparer.Instance);

        private static readonly ConcurrentDictionary<MemberInfo, Func<object, TResult>> _constMemberAccessCache =
            new ConcurrentDictionary<MemberInfo, Func<object, TResult>>();

        public static Func<TModel, object> Compile(Expression<Func<TModel, TResult>> expression)
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

                // model => ModelType.StaticMember
                case MemberExpression memberExpression when memberExpression.Expression == null:
                    return CompileFromStaticMemberAccess(expression, memberExpression);

                // model => model.Member
                case MemberExpression memberExpression when memberExpression.Expression == expression.Parameters[0]:
                    return CompileFromSimpleMemberAccess(expression, memberExpression);

                // model => model.Member1.Member2
                case MemberExpression memberExpression when IsChainedPropertyAccessor(memberExpression):
                    return CompileForChainedMemberAccess(expression, memberExpression);

                default:
                    return null;
            }

            bool IsChainedPropertyAccessor(MemberExpression memberExpression)
            {
                while (memberExpression.Expression != null)
                {
                    if (memberExpression.Expression is MemberExpression leftExpression)
                    {
                        memberExpression = leftExpression;
                        continue;
                    }
                    else if (memberExpression.Expression == expression.Parameters[0])
                    {
                        return true;
                    }

                    break;
                }

                return false;
            }
        }

        private static Func<TModel, object> CompileFromConstLookup(
            ConstantExpression constantExpression)
        {
            // model => {const}
            var constantValue = constantExpression.Value;
            return _ => constantValue;
        }

        private static Func<TModel, object> CompileFromIdentityFunc(
            Expression<Func<TModel, TResult>> expression)
        {
            // model => model
            // Don't need to lock, as all identity funcs are identical.
            if (_identityFunc == null)
            {
                var identityFuncCore = expression.Compile();
                _identityFunc = model => identityFuncCore(model);
            }

            return _identityFunc;
        }

        private static Func<TModel, object> CompileFromStaticMemberAccess(
            Expression<Func<TModel, TResult>> expression,
            MemberExpression memberExpression)
        {
            // model => ModelType.StaticMember
            if (_simpleMemberAccessCache.TryGetValue(memberExpression.Member, out var result))
            {
                return result;
            }

            var func = expression.Compile();
            result = model => func(model);
            result = _simpleMemberAccessCache.GetOrAdd(memberExpression.Member, result);

            return result;
        }

        private static Func<TModel, object> CompileFromSimpleMemberAccess(
            Expression<Func<TModel, TResult>> expression,
            MemberExpression memberExpression)
        {
            // Input: () => m.Member
            // Output: () => (m == null) ? null : m.Member
            if (_simpleMemberAccessCache.TryGetValue(memberExpression.Member, out var result))
            {
                return result;
            }

            result = _simpleMemberAccessCache.GetOrAdd(memberExpression.Member, Rewrite(expression, memberExpression));
            return result;
        }

        private static Func<TModel, object> CompileForChainedMemberAccess(
            Expression<Func<TModel, TResult>> expression,
            MemberExpression memberExpression)
        {
            // Input: () => m.Member1.Member2
            // Output: () => (m == null || m.Member1 == null) ? null : m.Member1.Member2
            var key = new MemberExpressionCacheKey(typeof(TModel), memberExpression);
            if (_chainedMemberAccessCache.TryGetValue(key, out var result))
            {
                return result;
            }

            var cacheableKey = key.MakeCacheable();
            result = _chainedMemberAccessCache.GetOrAdd(cacheableKey, Rewrite(expression, memberExpression));
            return result;
        }

        private static Func<TModel, object> CompileCapturedConstant(MemberExpression memberExpression, ConstantExpression constantExpression)
        {
            // model => {const} (captured local variable)
            if (!_constMemberAccessCache.TryGetValue(memberExpression.Member, out var result))
            {
                // rewrite as capturedLocal => ((TDeclaringType)capturedLocal)
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

        private static Func<TModel, object> Rewrite(
            Expression<Func<TModel, TResult>> expression,
            MemberExpression memberExpression)
        {
            Expression combinedNullTest = null;
            var currentExpression = memberExpression;

            while (currentExpression != null)
            {
                AddNullCheck(currentExpression.Expression, ref combinedNullTest);

                if (currentExpression.Expression is MemberExpression leftExpression)
                {
                    currentExpression = leftExpression;
                }
                else
                {
                    break;
                }
            }

            var body = expression.Body;

            // Cast the entire expression to object in case Member is a value type. This is required for us to be able to
            // express the null conditional statement m == null ? null : (object)m.IntValue
            if (body.Type.IsValueType)
            {
                body = Expression.Convert(body, typeof(object));
            }

            if (combinedNullTest != null)
            {
                Debug.Assert(combinedNullTest.Type == typeof(bool));
                body = Expression.Condition(
                    combinedNullTest,
                    Expression.Constant(value: null, body.Type),
                    body);
            }

            var rewrittenExpression = Expression.Lambda<Func<TModel, object>>(body, expression.Parameters);
            return rewrittenExpression.Compile();
        }

        private static void AddNullCheck(Expression invokingExpression, ref Expression combinedNullTest)
        {
            var type = invokingExpression.Type;
            var isNullableValueType = type.IsValueType && Nullable.GetUnderlyingType(type) != null;
            if (type.IsValueType && !isNullableValueType)
            {
                // struct.Member where struct is not nullable. Do nothing.
                return;
            }

            // NullableStruct.Member or Class.Member
            // type is Nullable ? (value == null) : object.ReferenceEquals(value, null)
            var nullTest = isNullableValueType ?
                Expression.Equal(invokingExpression, NullExpression) :
                Expression.ReferenceEqual(invokingExpression, NullExpression);

            if (combinedNullTest == null)
            {
                combinedNullTest = nullTest;
            }
            else
            {
                // m == null || m.Member == null
                combinedNullTest = Expression.OrElse(nullTest, combinedNullTest);
            }
        }
    }
}
