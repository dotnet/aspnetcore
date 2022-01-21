// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Extensions.Internal;

#nullable enable

namespace Microsoft.AspNetCore.Http;

internal sealed class ParameterBindingMethodCache
{
    private static readonly MethodInfo ConvertValueTaskMethod = typeof(ParameterBindingMethodCache).GetMethod(nameof(ConvertValueTask), BindingFlags.NonPublic | BindingFlags.Static)!;
    private static readonly MethodInfo ConvertValueTaskOfNullableResultMethod = typeof(ParameterBindingMethodCache).GetMethod(nameof(ConvertValueTaskOfNullableResult), BindingFlags.NonPublic | BindingFlags.Static)!;

    internal static readonly ParameterExpression TempSourceStringExpr = Expression.Variable(typeof(string), "tempSourceString");
    internal static readonly ParameterExpression HttpContextExpr = Expression.Parameter(typeof(HttpContext), "httpContext");

    private readonly MethodInfo _enumTryParseMethod;

    // Since this is shared source, the cache won't be shared between RequestDelegateFactory and the ApiDescriptionProvider sadly :(
    private readonly ConcurrentDictionary<Type, Func<ParameterExpression, Expression>?> _stringMethodCallCache = new();
    private readonly ConcurrentDictionary<Type, (Func<ParameterInfo, Expression>?, int)> _bindAsyncMethodCallCache = new();

    // If IsDynamicCodeSupported is false, we can't use the static Enum.TryParse<T> since there's no easy way for
    // this code to generate the specific instantiation for any enums used
    public ParameterBindingMethodCache() : this(preferNonGenericEnumParseOverload: !RuntimeFeature.IsDynamicCodeSupported)
    {
    }

    // This is for testing
    public ParameterBindingMethodCache(bool preferNonGenericEnumParseOverload)
    {
        _enumTryParseMethod = GetEnumTryParseMethod(preferNonGenericEnumParseOverload);
    }

    public bool HasTryParseMethod(ParameterInfo parameter)
    {
        var nonNullableParameterType = Nullable.GetUnderlyingType(parameter.ParameterType) ?? parameter.ParameterType;
        return FindTryParseMethod(nonNullableParameterType) is not null;
    }

    public bool HasBindAsyncMethod(ParameterInfo parameter) =>
        FindBindAsyncMethod(parameter).Expression is not null;

    public Func<ParameterExpression, Expression>? FindTryParseMethod(Type type)
    {
        Func<ParameterExpression, Expression>? Finder(Type type)
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
                // We generate `DateTimeStyles.AdjustToUniversal | DateTimeStyles.AllowWhiteSpaces ` to
                // support parsing types into the UTC timezone for DateTime. We don't assume the timezone
                // on the original value which will cause the parser to set the `Kind` property on the
                // `DateTime` as `Unspecified` indicating that it was parsed from an ambiguous timezone.
                //
                // `DateTimeOffset`s are always in UTC and don't allow specifying an `Unspecific` kind.
                // For this, we always assume that the original value is already in UTC to avoid resolving
                // the offset incorrectly depending on the timezone of the machine. We don't bother mapping
                // it to UTC in this case. In the event that the original timestamp is not in UTC, it's offset
                // value will be maintained.
                //
                // DateOnly and TimeOnly types do not support conversion to Utc so we
                // default to `DateTimeStyles.AllowWhiteSpaces`.
                var dateTimeStyles = type switch
                {
                    Type t when t == typeof(DateTime) => DateTimeStyles.AdjustToUniversal | DateTimeStyles.AllowWhiteSpaces,
                    Type t when t == typeof(DateTimeOffset) => DateTimeStyles.AssumeUniversal | DateTimeStyles.AllowWhiteSpaces,
                    _ => DateTimeStyles.AllowWhiteSpaces
                };

                return (expression) => Expression.Call(
                    methodInfo!,
                    TempSourceStringExpr,
                    Expression.Constant(CultureInfo.InvariantCulture),
                    Expression.Constant(dateTimeStyles),
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

            methodInfo = GetStaticMethodFromHierarchy(type, "TryParse", new[] { typeof(string), typeof(IFormatProvider), type.MakeByRefType() }, ValidateReturnType);

            if (methodInfo is not null)
            {
                return (expression) => Expression.Call(
                    methodInfo,
                    TempSourceStringExpr,
                    Expression.Constant(CultureInfo.InvariantCulture),
                    expression);
            }

            methodInfo = GetStaticMethodFromHierarchy(type, "TryParse", new[] { typeof(string), type.MakeByRefType() }, ValidateReturnType);

            if (methodInfo is not null)
            {
                return (expression) => Expression.Call(methodInfo, TempSourceStringExpr, expression);
            }

            if (GetAnyMethodFromHierarchy(type, "TryParse") is MethodInfo invalidMethod)
            {
                var stringBuilder = new StringBuilder();
                stringBuilder.AppendLine(CultureInfo.InvariantCulture, $"TryParse method found on {TypeNameHelper.GetTypeDisplayName(type, fullName: false)} with incorrect format. Must be a static method with format");
                stringBuilder.AppendLine(CultureInfo.InvariantCulture, $"bool TryParse(string, IFormatProvider, out {TypeNameHelper.GetTypeDisplayName(type, fullName: false)})");
                stringBuilder.AppendLine(CultureInfo.InvariantCulture, $"bool TryParse(string, out {TypeNameHelper.GetTypeDisplayName(type, fullName: false)})");
                stringBuilder.AppendLine("but found");
                stringBuilder.Append(invalidMethod.IsStatic ? "static " : "not-static ");
                stringBuilder.Append(invalidMethod.ToString());

                throw new InvalidOperationException(stringBuilder.ToString());
            }

            return null;

            static bool ValidateReturnType(MethodInfo methodInfo)
            {
                return methodInfo.ReturnType.Equals(typeof(bool));
            }
        }

        return _stringMethodCallCache.GetOrAdd(type, Finder);
    }

    public (Expression? Expression, int ParamCount) FindBindAsyncMethod(ParameterInfo parameter)
    {
        static (Func<ParameterInfo, Expression>?, int) Finder(Type nonNullableParameterType)
        {
            var hasParameterInfo = true;
            // There should only be one BindAsync method with these parameters since C# does not allow overloading on return type.
            var methodInfo = GetStaticMethodFromHierarchy(nonNullableParameterType, "BindAsync", new[] { typeof(HttpContext), typeof(ParameterInfo) }, ValidateReturnType);
            if (methodInfo is null)
            {
                hasParameterInfo = false;
                methodInfo = GetStaticMethodFromHierarchy(nonNullableParameterType, "BindAsync", new[] { typeof(HttpContext) }, ValidateReturnType);
            }

            // We're looking for a method with the following signatures:
            // public static ValueTask<{type}> BindAsync(HttpContext context, ParameterInfo parameter)
            // public static ValueTask<Nullable<{type}>> BindAsync(HttpContext context, ParameterInfo parameter)
            if (methodInfo is not null)
            {
                var valueTaskResultType = methodInfo.ReturnType.GetGenericArguments()[0];

                // ValueTask<{type}>?
                if (valueTaskResultType == nonNullableParameterType)
                {
                    return ((parameter) =>
                    {
                        MethodCallExpression typedCall;
                        if (hasParameterInfo)
                        {
                            // parameter is being intentionally shadowed. We never want to use the outer ParameterInfo inside
                            // this Func because the ParameterInfo varies after it's been cached for a given parameter type.
                            typedCall = Expression.Call(methodInfo, HttpContextExpr, Expression.Constant(parameter));
                        }
                        else
                        {
                            typedCall = Expression.Call(methodInfo, HttpContextExpr);
                        }
                        return Expression.Call(ConvertValueTaskMethod.MakeGenericMethod(nonNullableParameterType), typedCall);
                    }, hasParameterInfo ? 2 : 1);
                }
                // ValueTask<Nullable<{type}>>?
                else if (valueTaskResultType.IsGenericType &&
                         valueTaskResultType.GetGenericTypeDefinition() == typeof(Nullable<>) &&
                         valueTaskResultType.GetGenericArguments()[0] == nonNullableParameterType)
                {
                    return ((parameter) =>
                    {
                        MethodCallExpression typedCall;
                        if (hasParameterInfo)
                        {
                            // parameter is being intentionally shadowed. We never want to use the outer ParameterInfo inside
                            // this Func because the ParameterInfo varies after it's been cached for a given parameter type.
                            typedCall = Expression.Call(methodInfo, HttpContextExpr, Expression.Constant(parameter));
                        }
                        else
                        {
                            typedCall = Expression.Call(methodInfo, HttpContextExpr);
                        }
                        return Expression.Call(ConvertValueTaskOfNullableResultMethod.MakeGenericMethod(nonNullableParameterType), typedCall);
                    }, hasParameterInfo ? 2 : 1);
                }
            }

            if (GetAnyMethodFromHierarchy(nonNullableParameterType, "BindAsync") is MethodInfo invalidBindMethod)
            {
                var stringBuilder = new StringBuilder();
                stringBuilder.AppendLine(CultureInfo.InvariantCulture, $"BindAsync method found on {TypeNameHelper.GetTypeDisplayName(nonNullableParameterType, fullName: false)} with incorrect format. Must be a static method with format");
                stringBuilder.AppendLine(CultureInfo.InvariantCulture, $"ValueTask<{TypeNameHelper.GetTypeDisplayName(nonNullableParameterType, fullName: false)}> BindAsync(HttpContext context, ParameterInfo parameter)");
                stringBuilder.AppendLine(CultureInfo.InvariantCulture, $"ValueTask<{TypeNameHelper.GetTypeDisplayName(nonNullableParameterType, fullName: false)}> BindAsync(HttpContext context)");
                stringBuilder.AppendLine(CultureInfo.InvariantCulture, $"ValueTask<{TypeNameHelper.GetTypeDisplayName(nonNullableParameterType, fullName: false)}?> BindAsync(HttpContext context, ParameterInfo parameter)");
                stringBuilder.AppendLine(CultureInfo.InvariantCulture, $"ValueTask<{TypeNameHelper.GetTypeDisplayName(nonNullableParameterType, fullName: false)}?> BindAsync(HttpContext context)");
                stringBuilder.AppendLine("but found");
                stringBuilder.Append(invalidBindMethod.IsStatic ? "static " : "not-static");
                stringBuilder.Append(invalidBindMethod.ToString());

                throw new InvalidOperationException(stringBuilder.ToString());
            }

            return (null, 0);
        }

        var nonNullableParameterType = Nullable.GetUnderlyingType(parameter.ParameterType) ?? parameter.ParameterType;
        var (method, paramCount) = _bindAsyncMethodCallCache.GetOrAdd(nonNullableParameterType, Finder);
        return (method?.Invoke(parameter), paramCount);

        static bool ValidateReturnType(MethodInfo methodInfo)
        {
            return methodInfo.ReturnType.IsGenericType &&
                methodInfo.ReturnType.GetGenericTypeDefinition() == typeof(ValueTask<>);
        }
    }

    private static MethodInfo? GetStaticMethodFromHierarchy(Type type, string name, Type[] parameterTypes, Func<MethodInfo, bool> validateReturnType)
    {
        bool IsMatch(MethodInfo? method) => method is not null && !method.IsAbstract && validateReturnType(method);

        var methodInfo = type.GetMethod(name, BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy, parameterTypes);

        if (IsMatch(methodInfo))
        {
            return methodInfo;
        }

        var candidateInterfaceMethodInfo = default(MethodInfo);

        // Check all interfaces for implementations. Fail if there are duplicates.
        foreach (var implementedInterface in type.GetInterfaces())
        {
            var interfaceMethod = implementedInterface.GetMethod(name, BindingFlags.Public | BindingFlags.Static, parameterTypes);

            if (IsMatch(interfaceMethod))
            {
                if (candidateInterfaceMethodInfo is not null)
                {
                    throw new InvalidOperationException($"{TypeNameHelper.GetTypeDisplayName(type, fullName: false)} implements multiple interfaces defining a static {interfaceMethod} method causing ambiguity.");
                }

                candidateInterfaceMethodInfo = interfaceMethod;
            }
        }

        return candidateInterfaceMethodInfo;
    }

    private static MethodInfo? GetAnyMethodFromHierarchy(Type type, string name)
    {
        // Find first incorrectly formatted method
        var methodInfo = type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.FlattenHierarchy)
            .FirstOrDefault(methodInfo => methodInfo.Name == name);

        if (methodInfo is not null)
        {
            return methodInfo;
        }

        foreach (var implementedInterface in type.GetInterfaces())
        {
            var interfaceMethod = implementedInterface.GetMethod(name, BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);

            if (interfaceMethod is not null)
            {
                return interfaceMethod;
            }
        }

        return null;
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

    private static ValueTask<object?> ConvertValueTask<T>(ValueTask<T> typedValueTask)
    {
        if (typedValueTask.IsCompletedSuccessfully)
        {
            var result = typedValueTask.GetAwaiter().GetResult();
            return new ValueTask<object?>(result);
        }

        static async ValueTask<object?> ConvertAwaited(ValueTask<T> typedValueTask) => await typedValueTask;
        return ConvertAwaited(typedValueTask);
    }

    private static ValueTask<object?> ConvertValueTaskOfNullableResult<T>(ValueTask<Nullable<T>> typedValueTask) where T : struct
    {
        if (typedValueTask.IsCompletedSuccessfully)
        {
            var result = typedValueTask.GetAwaiter().GetResult();
            return new ValueTask<object?>(result);
        }

        static async ValueTask<object?> ConvertAwaited(ValueTask<Nullable<T>> typedValueTask) => await typedValueTask;
        return ConvertAwaited(typedValueTask);
    }
}
