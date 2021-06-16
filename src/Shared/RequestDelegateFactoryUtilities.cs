// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Reflection;

namespace Microsoft.AspNetCore.Http
{
    // REVIEW: Better name?
    internal static class RequestDelegateFactoryUtilities
    {
        private static readonly MethodInfo EnumTryParseMethod = GetEnumTryParseMethod();

        // Since this is shared source, the cache won't be shared between RequestDelegateFactory and the ApiDescriptionProvider sadly :(
        private static readonly ConcurrentDictionary<Type, MethodInfo?> TryParseMethodCache = new();

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

                var staticMethods = type.GetMethods(BindingFlags.Public | BindingFlags.Static);

                foreach (var method in staticMethods)
                {
                    if (method.Name != "TryParse" || method.ReturnType != typeof(bool))
                    {
                        continue;
                    }

                    var tryParseParameters = method.GetParameters();

                    if (tryParseParameters.Length == 2 &&
                        tryParseParameters[0].ParameterType == typeof(string) &&
                        tryParseParameters[1].IsOut &&
                        tryParseParameters[1].ParameterType == type.MakeByRefType())
                    {
                        return method;
                    }
                }

                return null;
            }

            return TryParseMethodCache.GetOrAdd(type, Finder);
        }

        private static MethodInfo GetEnumTryParseMethod()
        {
            var staticEnumMethods = typeof(Enum).GetMethods(BindingFlags.Public | BindingFlags.Static);

            foreach (var method in staticEnumMethods)
            {
                if (!method.IsGenericMethod || method.Name != "TryParse" || method.ReturnType != typeof(bool))
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

            throw new Exception("static bool System.Enum.TryParse<TEnum>(string? value, out TEnum result) does not exist!!?!?");
        }
    }
}
