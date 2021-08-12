// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq.Expressions;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;

#nullable enable

namespace Microsoft.AspNetCore.Http
{
    internal sealed class TryParseMethodCache
    {
        private readonly MethodInfo _enumTryParseMethod;

        // Since this is shared source, the cache won't be shared between RequestDelegateFactory and the ApiDescriptionProvider sadly :(
        private readonly ConcurrentDictionary<Type, Func<Expression, Expression>?> _methodCallCache = new();

        internal readonly ParameterExpression TempSourceStringExpr = Expression.Variable(typeof(string), "tempSourceString");

        // If IsDynamicCodeSupported is false, we can't use the static Enum.TryParse<T> since there's no easy way for
        // this code to generate the specific instantiation for any enums used
        public TryParseMethodCache() : this(preferNonGenericEnumParseOverload: !RuntimeFeature.IsDynamicCodeSupported)
        {
        }

        // This is for testing
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
            MethodInfo? methodInfo = null;

            if (preferNonGenericEnumParseOverload)
            {
                methodInfo = typeof(Enum).GetMethod(
                                nameof(Enum.TryParse),
                                BindingFlags.Public | BindingFlags.Static,
                                new[] { typeof(Type), typeof(string), typeof(object).MakeByRefType() });
            }
            else
            {
                methodInfo = typeof(Enum).GetMethod(
                               nameof(Enum.TryParse),
                               genericParameterCount: 1,
                               new[] { typeof(string), Type.MakeGenericMethodParameter(0).MakeByRefType() });
            }

            if (methodInfo is null)
            {
                Debug.Fail("No suitable System.Enum.TryParse method found.");
                throw new MissingMethodException("No suitable System.Enum.TryParse method found.");
            }

            return methodInfo!;
        }

        private static bool TryGetDateTimeTryParseMethod(Type type, [NotNullWhen(true)] out MethodInfo? methodInfo)
        {
            methodInfo = null;

            if (type == typeof(DateTime))
            {
                methodInfo = typeof(DateTime).GetMethod(
                     nameof(DateTime.TryParse),
                     BindingFlags.Public | BindingFlags.Static,
                     new[] { typeof(string), typeof(IFormatProvider), typeof(DateTimeStyles), typeof(DateTime).MakeByRefType() });
            }
            else if (type == typeof(DateTimeOffset))
            {
                methodInfo = typeof(DateTimeOffset).GetMethod(
                     nameof(DateTimeOffset.TryParse),
                     BindingFlags.Public | BindingFlags.Static,
                     new[] { typeof(string), typeof(IFormatProvider), typeof(DateTimeStyles), typeof(DateTimeOffset).MakeByRefType() });
            }
            else if (type == typeof(DateOnly))
            {
                methodInfo = typeof(DateOnly).GetMethod(
                     nameof(DateOnly.TryParse),
                     BindingFlags.Public | BindingFlags.Static,
                     new[] { typeof(string), typeof(IFormatProvider), typeof(DateTimeStyles), typeof(DateOnly).MakeByRefType() });
            }
            else if (type == typeof(TimeOnly))
            {
                methodInfo = typeof(TimeOnly).GetMethod(
                     nameof(TimeOnly.TryParse),
                     BindingFlags.Public | BindingFlags.Static,
                     new[] { typeof(string), typeof(IFormatProvider), typeof(DateTimeStyles), typeof(TimeOnly).MakeByRefType() });
            }

            return methodInfo != null;
        }

        private static bool TryGetNumberStylesTryGetMethod(Type type, [NotNullWhen(true)] out MethodInfo? method, [NotNullWhen(true)] out NumberStyles? numberStyles)
        {
            method = null;
            numberStyles = NumberStyles.Integer;

            if (type == typeof(long))
            {
                method = typeof(long).GetMethod(
                          nameof(long.TryParse),
                          BindingFlags.Public | BindingFlags.Static,
                          new[] { typeof(string), typeof(NumberStyles), typeof(IFormatProvider), typeof(long).MakeByRefType() });
            }
            else if (type == typeof(ulong))
            {
                method = typeof(ulong).GetMethod(
                          nameof(ulong.TryParse),
                          BindingFlags.Public | BindingFlags.Static,
                          new[] { typeof(string), typeof(NumberStyles), typeof(IFormatProvider), typeof(ulong).MakeByRefType() });
            }
            else if (type == typeof(int))
            {
                method = typeof(int).GetMethod(
                          nameof(int.TryParse),
                          BindingFlags.Public | BindingFlags.Static,
                          new[] { typeof(string), typeof(NumberStyles), typeof(IFormatProvider), typeof(int).MakeByRefType() });
            }
            else if (type == typeof(uint))
            {
                method = typeof(uint).GetMethod(
                          nameof(uint.TryParse),
                          BindingFlags.Public | BindingFlags.Static,
                          new[] { typeof(string), typeof(NumberStyles), typeof(IFormatProvider), typeof(uint).MakeByRefType() });
            }
            else if (type == typeof(short))
            {
                method = typeof(short).GetMethod(
                          nameof(short.TryParse),
                          BindingFlags.Public | BindingFlags.Static,
                          new[] { typeof(string), typeof(NumberStyles), typeof(IFormatProvider), typeof(short).MakeByRefType() });
            }
            else if (type == typeof(ushort))
            {
                method = typeof(ushort).GetMethod(
                          nameof(ushort.TryParse),
                          BindingFlags.Public | BindingFlags.Static,
                          new[] { typeof(string), typeof(NumberStyles), typeof(IFormatProvider), typeof(ushort).MakeByRefType() });
            }
            else if (type == typeof(byte))
            {
                method = typeof(byte).GetMethod(
                          nameof(byte.TryParse),
                          BindingFlags.Public | BindingFlags.Static,
                          new[] { typeof(string), typeof(NumberStyles), typeof(IFormatProvider), typeof(byte).MakeByRefType() });
            }
            else if (type == typeof(sbyte))
            {
                method = typeof(sbyte).GetMethod(
                          nameof(sbyte.TryParse),
                          BindingFlags.Public | BindingFlags.Static,
                          new[] { typeof(string), typeof(NumberStyles), typeof(IFormatProvider), typeof(sbyte).MakeByRefType() });
            }
            else if (type == typeof(double))
            {
                method = typeof(double).GetMethod(
                          nameof(double.TryParse),
                          BindingFlags.Public | BindingFlags.Static,
                          new[] { typeof(string), typeof(NumberStyles), typeof(IFormatProvider), typeof(double).MakeByRefType() });

                numberStyles = NumberStyles.AllowThousands | NumberStyles.Float;
            }
            else if (type == typeof(float))
            {
                method = typeof(float).GetMethod(
                          nameof(float.TryParse),
                          BindingFlags.Public | BindingFlags.Static,
                          new[] { typeof(string), typeof(NumberStyles), typeof(IFormatProvider), typeof(float).MakeByRefType() });

                numberStyles = NumberStyles.AllowThousands | NumberStyles.Float;
            }
            else if (type == typeof(Half))
            {
                method = typeof(Half).GetMethod(
                          nameof(Half.TryParse),
                          BindingFlags.Public | BindingFlags.Static,
                          new[] { typeof(string), typeof(NumberStyles), typeof(IFormatProvider), typeof(Half).MakeByRefType() });

                numberStyles = NumberStyles.AllowThousands | NumberStyles.Float;
            }
            else if (type == typeof(decimal))
            {
                method = typeof(decimal).GetMethod(
                          nameof(decimal.TryParse),
                          BindingFlags.Public | BindingFlags.Static,
                          new[] { typeof(string), typeof(NumberStyles), typeof(IFormatProvider), typeof(decimal).MakeByRefType() });

                numberStyles = NumberStyles.Number;
            }
            else if (type == typeof(IntPtr))
            {
                method = typeof(IntPtr).GetMethod(
                          nameof(IntPtr.TryParse),
                          BindingFlags.Public | BindingFlags.Static,
                          new[] { typeof(string), typeof(NumberStyles), typeof(IFormatProvider), typeof(IntPtr).MakeByRefType() });
            }
            else if (type == typeof(BigInteger))
            {
                method = typeof(BigInteger).GetMethod(
                          nameof(BigInteger.TryParse),
                          BindingFlags.Public | BindingFlags.Static,
                          new[] { typeof(string), typeof(NumberStyles), typeof(IFormatProvider), typeof(BigInteger).MakeByRefType() });
            }

            return method != null;
        }
    }
}
