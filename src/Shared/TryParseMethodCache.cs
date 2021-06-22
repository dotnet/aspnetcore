// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Numerics;
using System.Reflection;

namespace Microsoft.AspNetCore.Http
{
    internal static class TryParseMethodCache
    {
        private static readonly MethodInfo EnumTryParseMethod = GetEnumTryParseMethod();

        // Since this is shared source, the cache won't be shared between RequestDelegateFactory and the ApiDescriptionProvider sadly :(
        private static readonly ConcurrentDictionary<Type, Func<ParameterExpression, Expression, MethodCallExpression>?> MethodCallCache = new(); 

        public static bool HasTryParseMethod(ParameterInfo parameter)
        {
            return FindTryParseMethodCall(parameter) is not null;
        }

        // TODO: Use InvariantCulture where possible? Or is CurrentCulture fine because it's more flexible?
        public static MethodInfo? FindTryParseMethod(Type type)
        {
            static MethodInfo? Finder(Type type)
            {
                if (type.IsEnum)
                {
                    return EnumTryParseMethod.MakeGenericMethod(type);
                }

                if (TryGetDateTimeTryPareMethod(type, out var methodDateInfo))
                {
                    return methodDateInfo;
                }

                if (TryGetNumberStylesTryGetMethod(type, out var methodInfo, out var _))
                {
                    return methodInfo;
                }

                var staticMethods = type.GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .OrderByDescending(m => m.GetParameters().Length); ;

                foreach (var method in staticMethods)
                {
                    if (method.Name != "TryParse" || method.ReturnType != typeof(bool))
                    {
                        continue;
                    }

                    var tryParseParameters = method.GetParameters();

                    if (tryParseParameters.Length == 3 &&
                        tryParseParameters[0].ParameterType == typeof(string) &&
                        tryParseParameters[1].ParameterType == typeof(IFormatProvider) &&
                        tryParseParameters[2].IsOut &&
                        tryParseParameters[2].ParameterType == type.MakeByRefType())
                    {
                        return method;
                    }
                    else if (tryParseParameters.Length == 2 &&
                        tryParseParameters[0].ParameterType == typeof(string) &&
                        tryParseParameters[1].IsOut &&
                        tryParseParameters[1].ParameterType == type.MakeByRefType())
                    {
                        return method;
                    }
                }

                return null;
            }

            return Finder(type);
        }

        public static Func<ParameterExpression, Expression, MethodCallExpression>? FindTryParseMethodCall(ParameterInfo parameter)
        {
            static Func<ParameterExpression, Expression, MethodCallExpression>? Finder(Type type)
            {
                MethodInfo? methodInfo;

                if (type.IsEnum)
                {
                    methodInfo = EnumTryParseMethod.MakeGenericMethod(type);
                    if (methodInfo != null)
                    {
                        return (parameterExpression, expression) => Expression.Call(methodInfo!, parameterExpression, expression);
                    }

                    return null;
                }

                if (TryGetDateTimeTryPareMethod(type, out methodInfo))
                {
                    return (parameterExpression, expression) => Expression.Call(
                        methodInfo!,
                        parameterExpression,
                        Expression.Constant(CultureInfo.InvariantCulture),
                        Expression.Constant(DateTimeStyles.None),
                        expression);
                }

                if (TryGetNumberStylesTryGetMethod(type, out methodInfo, out var numberStyle))
                {
                    return (parameterExpression, expression) => Expression.Call(
                        methodInfo!,
                        parameterExpression,
                        Expression.Constant(numberStyle),
                        Expression.Constant(CultureInfo.InvariantCulture),
                        expression);
                }

                var staticMethods = type.GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .OrderByDescending(m => m.GetParameters().Length); ;

                foreach (var method in staticMethods)
                {
                    if (method.Name != "TryParse" || method.ReturnType != typeof(bool))
                    {
                        continue;
                    }

                    var tryParseParameters = method.GetParameters();

                    if (tryParseParameters.Length == 3 &&
                        tryParseParameters[0].ParameterType == typeof(string) &&
                        tryParseParameters[1].ParameterType == typeof(IFormatProvider) &&
                        tryParseParameters[2].IsOut &&
                        tryParseParameters[2].ParameterType == type.MakeByRefType())
                    {
                        return (parameterExpression, expression) => Expression.Call(
                            method,
                            parameterExpression,
                            Expression.Constant(CultureInfo.InvariantCulture),
                            expression);
                    }
                    else if (tryParseParameters.Length == 2 &&
                        tryParseParameters[0].ParameterType == typeof(string) &&
                        tryParseParameters[1].IsOut &&
                        tryParseParameters[1].ParameterType == type.MakeByRefType())
                    {
                        return (parameterExpression, expression) => Expression.Call(method, parameterExpression, expression);
                    }
                }

                return null;
            }

            var underlyingType = Nullable.GetUnderlyingType(parameter.ParameterType) ?? parameter.ParameterType;
            return MethodCallCache.GetOrAdd(underlyingType, Finder);
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

        private static bool TryGetDateTimeTryPareMethod(Type type, out MethodInfo? methodInfo)
        {
            methodInfo = null;
            if (type != typeof(DateTime) && type != typeof(DateOnly) &&
                 type != typeof(DateTimeOffset) && type != typeof(TimeOnly))
            {
                return false;
            }

            var staticDateMethods = type.GetMethods(BindingFlags.Public | BindingFlags.Static)
                   .Where(m => m.Name == "TryParse" && m.ReturnType == typeof(bool))
                   .OrderByDescending(m => m.GetParameters().Length);

            foreach (var method in staticDateMethods)
            {
                var tryParseParameters = method.GetParameters();

                if (tryParseParameters.Length == 4 &&
                    tryParseParameters[0].ParameterType == typeof(string) &&
                    tryParseParameters[1].ParameterType == typeof(IFormatProvider) &&
                    tryParseParameters[2].ParameterType == typeof(DateTimeStyles) &&
                    tryParseParameters[3].IsOut &&
                    tryParseParameters[3].ParameterType == type.MakeByRefType())
                {
                    methodInfo = method;
                    break;
                }
            }

            return methodInfo != null;
        }

        private static bool TryGetNumberStylesTryGetMethod(Type type, out MethodInfo? method, out NumberStyles? numberStyles)
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
                        numberStyles = NumberStyles.Integer;
                    }

                    if (type == typeof(double) || type == typeof(float) || type == typeof(Half))
                    {
                        numberStyles = NumberStyles.AllowThousands | NumberStyles.Float;
                    }

                    if (type == typeof(decimal))
                    {
                        numberStyles = NumberStyles.Number;
                    }

                    method = methodInfo;
                    break;
                }
            }

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

        internal static bool UseTryParseWithDateTimeStyleOptions(Type type)
            => type == typeof(DateTime) ||
               type == typeof(DateTimeOffset) ||
               type == typeof(DateOnly) ||
               type == typeof(TimeOnly);
    }
}
