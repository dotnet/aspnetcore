// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
    internal static class TryParseMethodCache
    {
        private static readonly MethodInfo EnumTryParseMethod = GetEnumTryParseMethod();

        // Since this is shared source, the cache won't be shared between RequestDelegateFactory and the ApiDescriptionProvider sadly :(
        private static readonly ConcurrentDictionary<Type, Func<Expression, MethodCallExpression>?> MethodCallCache = new();
        internal static readonly ParameterExpression TempSourceStringExpr = Expression.Variable(typeof(string), "tempSourceString");

        public static bool HasTryParseMethod(ParameterInfo parameter)
        {
            var nonNullableParameterType = Nullable.GetUnderlyingType(parameter.ParameterType) ?? parameter.ParameterType;
            return FindTryParseMethod(nonNullableParameterType) is not null;
        }

        public static Func<Expression, MethodCallExpression>? FindTryParseMethod(Type type)
        {
            static Func<Expression, MethodCallExpression>? Finder(Type type)
            {
                MethodInfo? methodInfo;

                if (type.IsEnum)
                {
                    methodInfo = EnumTryParseMethod.MakeGenericMethod(type);
                    if (methodInfo != null)
                    {
                        return (expression) => Expression.Call(methodInfo!, TempSourceStringExpr, expression);
                    }

                    return null;
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

            return MethodCallCache.GetOrAdd(type, Finder);
        }

        private static MethodInfo GetEnumTryParseMethod()
        {
            var staticEnumMethods = typeof(Enum).GetMethods(BindingFlags.Public | BindingFlags.Static);

            foreach (var method in staticEnumMethods)
            {
                if (!method.IsGenericMethod || method.Name != nameof(Enum.TryParse) || method.ReturnType != typeof(bool))
                {
                    continue;
                }

                var tryParseParameters = method.GetParameters();

                if (tryParseParameters.Length == 2 &&
                    tryParseParameters[0].ParameterType == typeof(string) &&
                    tryParseParameters[1].IsOut)
                {
                    return method;
                }
            }

            Debug.Fail("static bool System.Enum.TryParse<TEnum>(string? value, out TEnum result) not found.");
            throw new Exception("static bool System.Enum.TryParse<TEnum>(string? value, out TEnum result) not found.");
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
