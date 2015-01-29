// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Reflection;

namespace Microsoft.AspNet.Mvc
{
    internal static class TypeExtensions
    {
        // NOTE: Do not make #105 worse! Do not add new extension methods that conflict w/ .NET 4.5 methods. The
        // exising NETFX_CORE || ASPNETCORE50 methods should go away (soon).
#if NETFX_CORE || ASPNETCORE50
        private static bool EqualTo([NotNull] this Type[] t1, [NotNull] Type[] t2)
        {
            if (t1.Length != t2.Length)
            {
                return false;
            }

            for (var idx = 0; idx < t1.Length; ++idx)
            {
                if (t1[idx] != t2[idx])
                {
                    return false;
                }
            }

            return true;
        }

        public static ConstructorInfo GetConstructor([NotNull] this Type type, Type[] types)
        {
            return type.GetTypeInfo().DeclaredConstructors
                                     .Where(c => c.IsPublic)
                                     .SingleOrDefault(c => c.GetParameters()
                                                            .Select(p => p.ParameterType).ToArray().EqualTo(types));
        }
#endif

        public static Type BaseType([NotNull] this Type type)
        {
            return type.GetTypeInfo().BaseType;
        }

        public static Type ExtractGenericInterface([NotNull] this Type queryType, Type interfaceType)
        {
            Func<Type, bool> matchesInterface =
                t => t.IsGenericType() && t.GetGenericTypeDefinition() == interfaceType;
            return (matchesInterface(queryType)) ?
                queryType :
                queryType.GetInterfaces().FirstOrDefault(matchesInterface);
        }

#if NETFX_CORE || ASPNETCORE50
        public static Type[] GetGenericArguments([NotNull] this Type type)
        {
            return type.GetTypeInfo().GenericTypeArguments;
        }

        public static Type[] GetInterfaces([NotNull] this Type type)
        {
            return type.GetTypeInfo().ImplementedInterfaces.ToArray();
        }

        public static bool IsAssignableFrom([NotNull] this Type type, [NotNull] Type c)
        {
            return type.GetTypeInfo().IsAssignableFrom(c.GetTypeInfo());
        }
#endif

        public static bool IsEnum([NotNull] this Type type)
        {
            return type.GetTypeInfo().IsEnum;
        }

        public static bool IsGenericType([NotNull] this Type type)
        {
            return type.GetTypeInfo().IsGenericType;
        }

        public static bool IsInterface([NotNull] this Type type)
        {
            return type.GetTypeInfo().IsInterface;
        }

        public static bool IsValueType([NotNull] this Type type)
        {
            return type.GetTypeInfo().IsValueType;
        }

        public static bool IsCompatibleWith([NotNull] this Type type, object value)
        {
            return (value == null && AllowsNullValue(type)) ||
                (value != null && type.GetTypeInfo().IsAssignableFrom(value.GetType().GetTypeInfo()));
        }

        public static bool IsNullableValueType([NotNull] this Type type)
        {
            return Nullable.GetUnderlyingType(type) != null;
        }

        public static bool AllowsNullValue([NotNull] this Type type)
        {
            return (!type.GetTypeInfo().IsValueType || IsNullableValueType(type));
        }

        public static Type[] GetTypeArgumentsIfMatch([NotNull] Type closedType, Type matchingOpenType)
        {
            var closedTypeInfo = closedType.GetTypeInfo();
            if (!closedTypeInfo.IsGenericType)
            {
                return null;
            }

            var openType = closedType.GetGenericTypeDefinition();
            return (matchingOpenType == openType) ? closedTypeInfo.GenericTypeArguments : null;
        }
    }
}
