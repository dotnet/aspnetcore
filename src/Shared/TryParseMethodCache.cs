// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Numerics;
using System.Reflection;

#nullable enable

namespace Microsoft.AspNetCore.Http
{
    internal class TryParseMethodCache
    {
        private readonly MethodInfo _enumTryParseMethod;

        // Since this is shared source, the cache won't be shared between RequestDelegateFactory and the ApiDescriptionProvider sadly :(
        private readonly ConcurrentDictionary<Type, Func<Expression, Expression>?> _methodCallCache = new();

        internal readonly ParameterExpression TempSourceStringExpr = Expression.Variable(typeof(string), "tempSourceString");

        public TryParseMethodCache() : this(preferNonGenericEnumParseOverload: false)
        {
        }

        public TryParseMethodCache(bool preferNonGenericEnumParseOverload)
        {
            _enumTryParseMethod = GetEnumTryParseMethod(preferNonGenericEnumParseOverload);
        }

        public bool HasTryParseMethod(ParameterInfo parameter)
        {
            var nonNullableParameterType = Nullable.GetUnderlyingType(parameter.ParameterType) ?? parameter.ParameterType;
            return FindTryParseMethod(nonNullableParameterType) is not null;
        }

        public Func<Expression, Expression>? FindTryParseMethod(Type type)
        {
            Func<Expression, Expression>? Finder(Type type)
            {
                MethodInfo? methodInfo;

                if (type.IsEnum)
                {
                    if (_enumTryParseMethod.IsGenericMethod)
                    {
                        methodInfo = _enumTryParseMethod.MakeGenericMethod(type);

                        return (expression) => Expression.Call(methodInfo!, TempSourceStringExpr, expression);
                    }

                    return (expression) =>
                    {
                        var enumAsObject = Expression.Variable(typeof(object), "enumAsObject");
                        var success = Expression.Variable(typeof(bool), "success");

                        // object enumAsObject;
                        // bool success;
                        // success = Enum.TryParse(type, tempSourceString, out enumAsObject);
                        // parsedValue = success ? (Type)enumAsObject : default;
                        // return success;

                        return Expression.Block(new[] { success, enumAsObject },
                            Expression.Assign(success, Expression.Call(_enumTryParseMethod, Expression.Constant(type), TempSourceStringExpr, enumAsObject)),
                            Expression.Assign(expression,
                                Expression.Condition(success, Expression.Convert(enumAsObject, type), Expression.Default(type))),
                            success);
                    };

                }

                if (TryGetDateTimeTryParseMethod(type, out methodInfo))
                {
                    return (expression) => Expression.Call(
                        methodInfo!,
                        TempSourceStringExpr,
                        Expression.Constant(CultureInfo.InvariantCulture),
                        Expression.Constant(DateTimeStyles.None),
                        expression);
                }

                if (TryGetNumberStylesTryGetMethod(type, out methodInfo, out var numberStyle))
                {
                    return (expression) => Expression.Call(
                        methodInfo!,
                        TempSourceStringExpr,
                        Expression.Constant(numberStyle),
                        Expression.Constant(CultureInfo.InvariantCulture),
                        expression);
                }

                methodInfo = type.GetMethod("TryParse", BindingFlags.Public | BindingFlags.Static, new[] { typeof(string), typeof(IFormatProvider), type.MakeByRefType() });

                if (methodInfo != null)
                {
                    return (expression) => Expression.Call(
                        methodInfo,
                        TempSourceStringExpr,
                        Expression.Constant(CultureInfo.InvariantCulture),
                        expression);
                }

                methodInfo = type.GetMethod("TryParse", BindingFlags.Public | BindingFlags.Static, new[] { typeof(string), type.MakeByRefType() });

                if (methodInfo != null)
                {
                    return (expression) => Expression.Call(methodInfo, TempSourceStringExpr, expression);
                }

                return null;
            }

            return _methodCallCache.GetOrAdd(type, Finder);
        }

        private static MethodInfo GetEnumTryParseMethod(bool preferNonGenericEnumParseOverload)
        {
            var staticEnumMethods = typeof(Enum).GetMethods(BindingFlags.Public | BindingFlags.Static);

            // With NativeAOT, if there's no static usage of Enum.TryParse<T>, it will be removed
            // we fallback to the non-generic version if that is the case
            MethodInfo? genericCandidate = null;
            MethodInfo? nonGenericCandidate = null;

            foreach (var method in staticEnumMethods)
            {
                if (method.Name != nameof(Enum.TryParse) || method.ReturnType != typeof(bool))
                {
                    continue;
                }

                var tryParseParameters = method.GetParameters();

                // Enum.TryParse<T>(string, out object)
                if (method.IsGenericMethod &&
                    tryParseParameters.Length == 2 &&
                    tryParseParameters[0].ParameterType == typeof(string) &&
                    tryParseParameters[1].IsOut)
                {
                    genericCandidate = method;
                }

                // Enum.TryParse(type, string, out object)
                if (!method.IsGenericMethod &&
                    tryParseParameters.Length == 3 &&
                    tryParseParameters[0].ParameterType == typeof(Type) &&
                    tryParseParameters[1].ParameterType == typeof(string) &&
                    tryParseParameters[2].IsOut)
                {
                    nonGenericCandidate = method;
                }
            }

            if (genericCandidate is null && nonGenericCandidate is null)
            {
                Debug.Fail("No suitable System.Enum.TryParse method not found.");
                throw new Exception("No suitable System.Enum.TryParse method not found.");
            }

            if (preferNonGenericEnumParseOverload)
            {
                return nonGenericCandidate!;
            }

            return genericCandidate ?? nonGenericCandidate!;
        }

        private static bool TryGetDateTimeTryParseMethod(Type type, [NotNullWhen(true)] out MethodInfo? methodInfo)
        {
            methodInfo = null;
            if (type != typeof(DateTime) && type != typeof(DateOnly) &&
                 type != typeof(DateTimeOffset) && type != typeof(TimeOnly))
            {
                return false;
            }

            var staticTryParseDateMethod = type.GetMethod(
                "TryParse",
                BindingFlags.Public | BindingFlags.Static,
                new[] { typeof(string), typeof(IFormatProvider), typeof(DateTimeStyles), type.MakeByRefType() });

            methodInfo = staticTryParseDateMethod;

            return methodInfo != null;
        }

        private static bool TryGetNumberStylesTryGetMethod(Type type, [NotNullWhen(true)] out MethodInfo? method, [NotNullWhen(true)] out NumberStyles? numberStyles)
        {
            method = null;
            numberStyles = null;

            if (!UseTryParseWithNumberStyleOption(type))
            {
                return false;
            }

            var staticMethods = type.GetMethods(BindingFlags.Public | BindingFlags.Static)
                   .Where(m => m.Name == "TryParse" && m.ReturnType == typeof(bool))
                   .OrderByDescending(m => m.GetParameters().Length);

            var numberStylesToUse = NumberStyles.Integer;
            var methodToUse = default(MethodInfo);

            foreach (var methodInfo in staticMethods)
            {
                var tryParseParameters = methodInfo.GetParameters();

                if (tryParseParameters.Length == 4 &&
                    tryParseParameters[0].ParameterType == typeof(string) &&
                    tryParseParameters[1].ParameterType == typeof(NumberStyles) &&
                    tryParseParameters[2].ParameterType == typeof(IFormatProvider) &&
                    tryParseParameters[3].IsOut &&
                    tryParseParameters[3].ParameterType == type.MakeByRefType())
                {
                    if (type == typeof(int) || type == typeof(short) || type == typeof(IntPtr) ||
                        type == typeof(long) || type == typeof(byte) || type == typeof(sbyte) ||
                        type == typeof(ushort) || type == typeof(uint) || type == typeof(ulong) ||
                        type == typeof(BigInteger))
                    {
                        numberStylesToUse = NumberStyles.Integer;
                    }

                    if (type == typeof(double) || type == typeof(float) || type == typeof(Half))
                    {
                        numberStylesToUse = NumberStyles.AllowThousands | NumberStyles.Float;
                    }

                    if (type == typeof(decimal))
                    {
                        numberStylesToUse = NumberStyles.Number;
                    }

                    methodToUse = methodInfo!;
                    break;
                }
            }

            numberStyles = numberStylesToUse!;
            method = methodToUse!;

            return true;
        }

        internal static bool UseTryParseWithNumberStyleOption(Type type)
            => type == typeof(int) ||
                type == typeof(double) ||
                type == typeof(decimal) ||
                type == typeof(float) ||
                type == typeof(Half) ||
                type == typeof(short) ||
                type == typeof(long) ||
                type == typeof(IntPtr) ||
                type == typeof(byte) ||
                type == typeof(sbyte) ||
                type == typeof(ushort) ||
                type == typeof(uint) ||
                type == typeof(ulong) ||
                type == typeof(BigInteger);
    }
}
