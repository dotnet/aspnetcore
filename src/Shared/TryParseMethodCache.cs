// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Reflection;

namespace Microsoft.AspNetCore.Http
{
    internal static class TryParseMethodCache
    {
        private static readonly MethodInfo EnumTryParseMethod = GetEnumTryParseMethod();

        // Since this is shared source, the cache won't be shared between RequestDelegateFactory and the ApiDescriptionProvider sadly :(
        private static readonly ConcurrentDictionary<Type, MethodInfo?> Cache = new();

        public static bool HasTryParseMethod(ParameterInfo parameter)
        {
            var nonNullableParameterType = Nullable.GetUnderlyingType(parameter.ParameterType) ?? parameter.ParameterType;
            return FindTryParseMethod(nonNullableParameterType) is not null;
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

                if (UseTryParseWithDateTimeStyleOptions(type))
                {
                    return GetDateTimeTryPareMethod(type);
                }

                if (TryGetNumberStylesTryGetMethod(type, out var methodInfo))
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

            return Cache.GetOrAdd(type, Finder);
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

        private static MethodInfo GetDateTimeTryPareMethod(Type type)
        {
            if (type != typeof(DateTime) && type != typeof(DateOnly) &&
                 type != typeof(DateTimeOffset) && type != typeof(TimeOnly))
            {
                Debug.Fail("Parameter is not of type of DateTime, DateOnly, DateTimeOffset, TimeOnly!");
                throw new Exception("Parameter is not of type of DateTime, DateOnly, DateTimeOffset, TimeOnly !");
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
                    return method;
                }
            }

            Debug.Fail("static bool TryParse(string?, IFormatProvider, DateTimeStyles, out DateTime result) does not exit!!?!?");
            throw new Exception("static bool TryParse(string?, IFormatProvider, DateTimeStyles, out DateTime result) does not exit!!?!?");
        }

        private static bool TryGetNumberStylesTryGetMethod(Type type, out MethodInfo? method)
        {
            method = null;

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

        internal static NumberStyles SetRightNumberStyles(Type type)
        {
            if (!UseTryParseWithNumberStyleOption(type))
            {
                throw new InvalidOperationException("Incorrect type !");
            }

            if (type == typeof(int) || type == typeof(short) || type == typeof(IntPtr) ||
                type == typeof(long) || type == typeof(byte) || type == typeof(sbyte) ||
                type == typeof(ushort) || type == typeof(uint) || type == typeof(ulong) ||
                type == typeof(BigInteger))
            {
                return NumberStyles.Integer;
            }

            if (type == typeof(double) || type == typeof(float) || type == typeof(Half))
            {
                return NumberStyles.AllowThousands | NumberStyles.Float;
            }

            if (type == typeof(decimal))
            {
                return NumberStyles.Number;
            }

            return NumberStyles.None;
        }
    }
}
