// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using static Microsoft.AspNetCore.Components.BindConverter;

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Contains extension methods for two-way binding using <see cref="EventCallback"/>. For internal use only.
/// </summary>
//
// NOTE: for number parsing, the HTML5 spec dictates that <input type="number"> the DOM will represent
// number values as floating point numbers using `.` as the period separator. This is NOT culture sensitive.
// Put another way, the user might see `,` as their decimal separator, but the value available in events
// to JS code is always similar to what .NET parses with InvariantCulture.
//
// See: https://www.w3.org/TR/html5/sec-forms.html#number-state-typenumber
// See: https://www.w3.org/TR/html5/infrastructure.html#valid-floating-point-number
//
// For now we're not necessarily handling this correctly since we parse the same way for number and text.
public static class EventCallbackFactoryBinderExtensions
{
    /// <summary>
    /// For internal use only.
    /// </summary>
    /// <param name="factory"></param>
    /// <param name="receiver"></param>
    /// <param name="setter"></param>
    /// <param name="existingValue"></param>
    /// <param name="culture"></param>
    /// <returns></returns>
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Required to maintain compatibility")]
    public static EventCallback<ChangeEventArgs> CreateBinder(
        this EventCallbackFactory factory,
        object receiver,
        Action<string?> setter,
        string existingValue,
        CultureInfo? culture = null)
    {
        return CreateBinderCore<string?>(factory, receiver, setter, culture, ConvertToString);
    }

    /// <summary>
    /// For internal use only.
    /// </summary>
    /// <param name="factory"></param>
    /// <param name="receiver"></param>
    /// <param name="setter"></param>
    /// <param name="existingValue"></param>
    /// <param name="culture"></param>
    /// <returns></returns>
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Required to maintain compatibility")]
    public static EventCallback<ChangeEventArgs> CreateBinder(
        this EventCallbackFactory factory,
        object receiver,
        Func<string?, Task> setter,
        string existingValue,
        CultureInfo? culture = null)
    {
        return CreateBinderCoreAsync<string?>(factory, receiver, setter, culture, ConvertToString);
    }

    /// <summary>
    /// For internal use only.
    /// </summary>
    /// <param name="factory"></param>
    /// <param name="receiver"></param>
    /// <param name="setter"></param>
    /// <param name="existingValue"></param>
    /// <param name="culture"></param>
    /// <returns></returns>
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Required to maintain compatibility")]
    public static EventCallback<ChangeEventArgs> CreateBinder(
        this EventCallbackFactory factory,
        object receiver,
        Action<bool> setter,
        bool existingValue,
        CultureInfo? culture = null)
    {
        return CreateBinderCore<bool>(factory, receiver, setter, culture, ConvertToBool);
    }

    /// <summary>
    /// For internal use only.
    /// </summary>
    /// <param name="factory"></param>
    /// <param name="receiver"></param>
    /// <param name="setter"></param>
    /// <param name="existingValue"></param>
    /// <param name="culture"></param>
    /// <returns></returns>
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Required to maintain compatibility")]
    public static EventCallback<ChangeEventArgs> CreateBinder(
    this EventCallbackFactory factory,
    object receiver,
    Func<bool, Task> setter,
    bool existingValue,
    CultureInfo? culture = null)
    {
        return CreateBinderCoreAsync<bool>(factory, receiver, setter, culture, ConvertToBool);
    }

    /// <summary>
    /// For internal use only.
    /// </summary>
    /// <param name="factory"></param>
    /// <param name="receiver"></param>
    /// <param name="setter"></param>
    /// <param name="existingValue"></param>
    /// <param name="culture"></param>
    /// <returns></returns>
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Required to maintain compatibility")]
    public static EventCallback<ChangeEventArgs> CreateBinder(
        this EventCallbackFactory factory,
        object receiver,
        Action<bool?> setter,
        bool? existingValue,
        CultureInfo? culture = null)
    {
        return CreateBinderCore<bool?>(factory, receiver, setter, culture, ConvertToNullableBool);
    }

    /// <summary>
    /// For internal use only.
    /// </summary>
    /// <param name="factory"></param>
    /// <param name="receiver"></param>
    /// <param name="setter"></param>
    /// <param name="existingValue"></param>
    /// <param name="culture"></param>
    /// <returns></returns>
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Required to maintain compatibility")]
    public static EventCallback<ChangeEventArgs> CreateBinder(
        this EventCallbackFactory factory,
        object receiver,
        Func<bool?, Task> setter,
        bool? existingValue,
        CultureInfo? culture = null)
    {
        return CreateBinderCoreAsync<bool?>(factory, receiver, setter, culture, ConvertToNullableBool);
    }

    /// <summary>
    /// For internal use only.
    /// </summary>
    /// <param name="factory"></param>
    /// <param name="receiver"></param>
    /// <param name="setter"></param>
    /// <param name="existingValue"></param>
    /// <param name="culture"></param>
    /// <returns></returns>
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Required to maintain compatibility")]
    public static EventCallback<ChangeEventArgs> CreateBinder(
        this EventCallbackFactory factory,
        object receiver,
        Action<int> setter,
        int existingValue,
        CultureInfo? culture = null)
    {
        return CreateBinderCore<int>(factory, receiver, setter, culture, ConvertToInt);
    }

    /// <summary>
    /// For internal use only.
    /// </summary>
    /// <param name="factory"></param>
    /// <param name="receiver"></param>
    /// <param name="setter"></param>
    /// <param name="existingValue"></param>
    /// <param name="culture"></param>
    /// <returns></returns>
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Required to maintain compatibility")]
    public static EventCallback<ChangeEventArgs> CreateBinder(
        this EventCallbackFactory factory,
        object receiver,
        Func<int, Task> setter,
        int existingValue,
        CultureInfo? culture = null)
    {
        return CreateBinderCoreAsync<int>(factory, receiver, setter, culture, ConvertToInt);
    }

    /// <summary>
    /// For internal use only.
    /// </summary>
    /// <param name="factory"></param>
    /// <param name="receiver"></param>
    /// <param name="setter"></param>
    /// <param name="existingValue"></param>
    /// <param name="culture"></param>
    /// <returns></returns>
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Required to maintain compatibility")]
    public static EventCallback<ChangeEventArgs> CreateBinder(
        this EventCallbackFactory factory,
        object receiver,
        Action<int?> setter,
        int? existingValue,
        CultureInfo? culture = null)
    {
        return CreateBinderCore<int?>(factory, receiver, setter, culture, ConvertToNullableInt);
    }

    /// <summary>
    /// For internal use only.
    /// </summary>
    /// <param name="factory"></param>
    /// <param name="receiver"></param>
    /// <param name="setter"></param>
    /// <param name="existingValue"></param>
    /// <param name="culture"></param>
    /// <returns></returns>
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Required to maintain compatibility")]
    public static EventCallback<ChangeEventArgs> CreateBinder(
        this EventCallbackFactory factory,
        object receiver,
        Func<int?, Task> setter,
        int? existingValue,
        CultureInfo? culture = null)
    {
        return CreateBinderCoreAsync<int?>(factory, receiver, setter, culture, ConvertToNullableInt);
    }

    /// <summary>
    /// For internal use only.
    /// </summary>
    /// <param name="factory"></param>
    /// <param name="receiver"></param>
    /// <param name="setter"></param>
    /// <param name="existingValue"></param>
    /// <param name="culture"></param>
    /// <returns></returns>
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Required to maintain compatibility")]
    public static EventCallback<ChangeEventArgs> CreateBinder(
        this EventCallbackFactory factory,
        object receiver,
        Action<long> setter,
        long existingValue,
        CultureInfo? culture = null)
    {
        return CreateBinderCore<long>(factory, receiver, setter, culture, ConvertToLong);
    }

    /// <summary>
    /// For internal use only.
    /// </summary>
    /// <param name="factory"></param>
    /// <param name="receiver"></param>
    /// <param name="setter"></param>
    /// <param name="existingValue"></param>
    /// <param name="culture"></param>
    /// <returns></returns>
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Required to maintain compatibility")]
    public static EventCallback<ChangeEventArgs> CreateBinder(
        this EventCallbackFactory factory,
        object receiver,
        Func<long, Task> setter,
        long existingValue,
        CultureInfo? culture = null)
    {
        return CreateBinderCoreAsync<long>(factory, receiver, setter, culture, ConvertToLong);
    }

    /// <summary>
    /// For internal use only.
    /// </summary>
    /// <param name="factory"></param>
    /// <param name="receiver"></param>
    /// <param name="setter"></param>
    /// <param name="existingValue"></param>
    /// <param name="culture"></param>
    /// <returns></returns>
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Required to maintain compatibility")]
    public static EventCallback<ChangeEventArgs> CreateBinder(
        this EventCallbackFactory factory,
        object receiver,
        Action<short> setter,
        short existingValue,
        CultureInfo? culture = null)
    {
        return CreateBinderCore<short>(factory, receiver, setter, culture, ConvertToShort);
    }

    /// <summary>
    /// For internal use only.
    /// </summary>
    /// <param name="factory"></param>
    /// <param name="receiver"></param>
    /// <param name="setter"></param>
    /// <param name="existingValue"></param>
    /// <param name="culture"></param>
    /// <returns></returns>
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Required to maintain compatibility")]
    public static EventCallback<ChangeEventArgs> CreateBinder(
        this EventCallbackFactory factory,
        object receiver,
        Func<short, Task> setter,
        short existingValue,
        CultureInfo? culture = null)
    {
        return CreateBinderCoreAsync<short>(factory, receiver, setter, culture, ConvertToShort);
    }

    /// <summary>
    /// For internal use only.
    /// </summary>
    /// <param name="factory"></param>
    /// <param name="receiver"></param>
    /// <param name="setter"></param>
    /// <param name="existingValue"></param>
    /// <param name="culture"></param>
    /// <returns></returns>
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Required to maintain compatibility")]
    public static EventCallback<ChangeEventArgs> CreateBinder(
        this EventCallbackFactory factory,
        object receiver,
        Action<long?> setter,
        long? existingValue,
        CultureInfo? culture = null)
    {
        return CreateBinderCore<long?>(factory, receiver, setter, culture, ConvertToNullableLong);
    }

    /// <summary>
    /// For internal use only.
    /// </summary>
    /// <param name="factory"></param>
    /// <param name="receiver"></param>
    /// <param name="setter"></param>
    /// <param name="existingValue"></param>
    /// <param name="culture"></param>
    /// <returns></returns>
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Required to maintain compatibility")]
    public static EventCallback<ChangeEventArgs> CreateBinder(
        this EventCallbackFactory factory,
        object receiver,
        Func<long?, Task> setter,
        long? existingValue,
        CultureInfo? culture = null)
    {
        return CreateBinderCoreAsync<long?>(factory, receiver, setter, culture, ConvertToNullableLong);
    }

    /// <summary>
    /// For internal use only.
    /// </summary>
    /// <param name="factory"></param>
    /// <param name="receiver"></param>
    /// <param name="setter"></param>
    /// <param name="existingValue"></param>
    /// <param name="culture"></param>
    /// <returns></returns>
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Required to maintain compatibility")]
    public static EventCallback<ChangeEventArgs> CreateBinder(
        this EventCallbackFactory factory,
        object receiver,
        Action<short?> setter,
        short? existingValue,
        CultureInfo? culture = null)
    {
        return CreateBinderCore<short?>(factory, receiver, setter, culture, ConvertToNullableShort);
    }

    /// <summary>
    /// For internal use only.
    /// </summary>
    /// <param name="factory"></param>
    /// <param name="receiver"></param>
    /// <param name="setter"></param>
    /// <param name="existingValue"></param>
    /// <param name="culture"></param>
    /// <returns></returns>
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Required to maintain compatibility")]
    public static EventCallback<ChangeEventArgs> CreateBinder(
        this EventCallbackFactory factory,
        object receiver,
        Func<short?, Task> setter,
        short? existingValue,
        CultureInfo? culture = null)
    {
        return CreateBinderCoreAsync<short?>(factory, receiver, setter, culture, ConvertToNullableShort);
    }

    /// <summary>
    /// For internal use only.
    /// </summary>
    /// <param name="factory"></param>
    /// <param name="receiver"></param>
    /// <param name="setter"></param>
    /// <param name="existingValue"></param>
    /// <param name="culture"></param>
    /// <returns></returns>
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Required to maintain compatibility")]
    public static EventCallback<ChangeEventArgs> CreateBinder(
        this EventCallbackFactory factory,
        object receiver,
        Action<float> setter,
        float existingValue,
        CultureInfo? culture = null)
    {
        return CreateBinderCore<float>(factory, receiver, setter, culture, ConvertToFloat);
    }

    /// <summary>
    /// For internal use only.
    /// </summary>
    /// <param name="factory"></param>
    /// <param name="receiver"></param>
    /// <param name="setter"></param>
    /// <param name="existingValue"></param>
    /// <param name="culture"></param>
    /// <returns></returns>
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Required to maintain compatibility")]
    public static EventCallback<ChangeEventArgs> CreateBinder(
        this EventCallbackFactory factory,
        object receiver,
        Func<float, Task> setter,
        float existingValue,
        CultureInfo? culture = null)
    {
        return CreateBinderCoreAsync<float>(factory, receiver, setter, culture, ConvertToFloat);
    }

    /// <summary>
    /// For internal use only.
    /// </summary>
    /// <param name="factory"></param>
    /// <param name="receiver"></param>
    /// <param name="setter"></param>
    /// <param name="existingValue"></param>
    /// <param name="culture"></param>
    /// <returns></returns>
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Required to maintain compatibility")]
    public static EventCallback<ChangeEventArgs> CreateBinder(
        this EventCallbackFactory factory,
        object receiver,
        Action<float?> setter,
        float? existingValue,
        CultureInfo? culture = null)
    {
        return CreateBinderCore<float?>(factory, receiver, setter, culture, ConvertToNullableFloat);
    }

    /// <summary>
    /// For internal use only.
    /// </summary>
    /// <param name="factory"></param>
    /// <param name="receiver"></param>
    /// <param name="setter"></param>
    /// <param name="existingValue"></param>
    /// <param name="culture"></param>
    /// <returns></returns>
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Required to maintain compatibility")]
    public static EventCallback<ChangeEventArgs> CreateBinder(
        this EventCallbackFactory factory,
        object receiver,
        Func<float?, Task> setter,
        float? existingValue,
        CultureInfo? culture = null)
    {
        return CreateBinderCoreAsync<float?>(factory, receiver, setter, culture, ConvertToNullableFloat);
    }

    /// <summary>
    /// For internal use only.
    /// </summary>
    /// <param name="factory"></param>
    /// <param name="receiver"></param>
    /// <param name="setter"></param>
    /// <param name="existingValue"></param>
    /// <param name="culture"></param>
    /// <returns></returns>
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Required to maintain compatibility")]
    public static EventCallback<ChangeEventArgs> CreateBinder(
        this EventCallbackFactory factory,
        object receiver,
        Action<double> setter,
        double existingValue,
        CultureInfo? culture = null)
    {
        return CreateBinderCore<double>(factory, receiver, setter, culture, ConvertToDoubleDelegate);
    }

    /// <summary>
    /// For internal use only.
    /// </summary>
    /// <param name="factory"></param>
    /// <param name="receiver"></param>
    /// <param name="setter"></param>
    /// <param name="existingValue"></param>
    /// <param name="culture"></param>
    /// <returns></returns>
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Required to maintain compatibility")]
    public static EventCallback<ChangeEventArgs> CreateBinder(
        this EventCallbackFactory factory,
        object receiver,
        Func<double, Task> setter,
        double existingValue,
        CultureInfo? culture = null)
    {
        return CreateBinderCoreAsync<double>(factory, receiver, setter, culture, ConvertToDoubleDelegate);
    }

    /// <summary>
    /// For internal use only.
    /// </summary>
    /// <param name="factory"></param>
    /// <param name="receiver"></param>
    /// <param name="setter"></param>
    /// <param name="existingValue"></param>
    /// <param name="culture"></param>
    /// <returns></returns>
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Required to maintain compatibility")]
    public static EventCallback<ChangeEventArgs> CreateBinder(
        this EventCallbackFactory factory,
        object receiver,
        Action<double?> setter,
        double? existingValue,
        CultureInfo? culture = null)
    {
        return CreateBinderCore<double?>(factory, receiver, setter, culture, ConvertToNullableDoubleDelegate);
    }

    /// <summary>
    /// For internal use only.
    /// </summary>
    /// <param name="factory"></param>
    /// <param name="receiver"></param>
    /// <param name="setter"></param>
    /// <param name="existingValue"></param>
    /// <param name="culture"></param>
    /// <returns></returns>
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Required to maintain compatibility")]
    public static EventCallback<ChangeEventArgs> CreateBinder(
        this EventCallbackFactory factory,
        object receiver,
        Func<double?, Task> setter,
        double? existingValue,
        CultureInfo? culture = null)
    {
        return CreateBinderCoreAsync<double?>(factory, receiver, setter, culture, ConvertToNullableDoubleDelegate);
    }

    /// <summary>
    /// For internal use only.
    /// </summary>
    /// <param name="factory"></param>
    /// <param name="receiver"></param>
    /// <param name="setter"></param>
    /// <param name="existingValue"></param>
    /// <param name="culture"></param>
    /// <returns></returns>
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Required to maintain compatibility")]
    public static EventCallback<ChangeEventArgs> CreateBinder(
        this EventCallbackFactory factory,
        object receiver,
        Action<decimal> setter,
        decimal existingValue,
        CultureInfo? culture = null)
    {
        return CreateBinderCore<decimal>(factory, receiver, setter, culture, ConvertToDecimal);
    }

    /// <summary>
    /// For internal use only.
    /// </summary>
    /// <param name="factory"></param>
    /// <param name="receiver"></param>
    /// <param name="setter"></param>
    /// <param name="existingValue"></param>
    /// <param name="culture"></param>
    /// <returns></returns>
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Required to maintain compatibility")]
    public static EventCallback<ChangeEventArgs> CreateBinder(
        this EventCallbackFactory factory,
        object receiver,
        Func<decimal, Task> setter,
        decimal existingValue,
        CultureInfo? culture = null)
    {
        return CreateBinderCoreAsync<decimal>(factory, receiver, setter, culture, ConvertToDecimal);
    }

    /// <summary>
    /// For internal use only.
    /// </summary>
    /// <param name="factory"></param>
    /// <param name="receiver"></param>
    /// <param name="setter"></param>
    /// <param name="existingValue"></param>
    /// <param name="culture"></param>
    /// <returns></returns>
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Required to maintain compatibility")]
    public static EventCallback<ChangeEventArgs> CreateBinder(
        this EventCallbackFactory factory,
        object receiver,
        Action<decimal?> setter,
        decimal? existingValue,
        CultureInfo? culture = null)
    {
        return CreateBinderCore<decimal?>(factory, receiver, setter, culture, ConvertToNullableDecimal);
    }

    /// <summary>
    /// For internal use only.
    /// </summary>
    /// <param name="factory"></param>
    /// <param name="receiver"></param>
    /// <param name="setter"></param>
    /// <param name="existingValue"></param>
    /// <param name="culture"></param>
    /// <returns></returns>
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Required to maintain compatibility")]
    public static EventCallback<ChangeEventArgs> CreateBinder(
        this EventCallbackFactory factory,
        object receiver,
        Func<decimal?, Task> setter,
        decimal? existingValue,
        CultureInfo? culture = null)
    {
        return CreateBinderCoreAsync<decimal?>(factory, receiver, setter, culture, ConvertToNullableDecimal);
    }

    /// <summary>
    /// For internal use only.
    /// </summary>
    /// <param name="factory"></param>
    /// <param name="receiver"></param>
    /// <param name="setter"></param>
    /// <param name="existingValue"></param>
    /// <param name="culture"></param>
    /// <returns></returns>
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Required to maintain compatibility")]
    public static EventCallback<ChangeEventArgs> CreateBinder(
        this EventCallbackFactory factory,
        object receiver,
        Action<DateTime> setter,
        DateTime existingValue,
        CultureInfo? culture = null)
    {
        return CreateBinderCore<DateTime>(factory, receiver, setter, culture, ConvertToDateTime);
    }

    /// <summary>
    /// For internal use only.
    /// </summary>
    /// <param name="factory"></param>
    /// <param name="receiver"></param>
    /// <param name="setter"></param>
    /// <param name="existingValue"></param>
    /// <param name="culture"></param>
    /// <returns></returns>
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Required to maintain compatibility")]
    public static EventCallback<ChangeEventArgs> CreateBinder(
        this EventCallbackFactory factory,
        object receiver,
        Func<DateTime, Task> setter,
        DateTime existingValue,
        CultureInfo? culture = null)
    {
        return CreateBinderCoreAsync<DateTime>(factory, receiver, setter, culture, ConvertToDateTime);
    }

    /// <summary>
    /// For internal use only.
    /// </summary>
    /// <param name="factory"></param>
    /// <param name="receiver"></param>
    /// <param name="setter"></param>
    /// <param name="existingValue"></param>
    /// <param name="format"></param>
    /// <param name="culture"></param>
    /// <returns></returns>
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Required to maintain compatibility")]
    public static EventCallback<ChangeEventArgs> CreateBinder(
        this EventCallbackFactory factory,
        object receiver,
        Action<DateTime> setter,
        DateTime existingValue,
        string format,
        CultureInfo? culture = null)
    {
        return CreateBinderCore<DateTime>(factory, receiver, setter, culture, format, ConvertToDateTimeWithFormat);
    }

    /// <summary>
    /// For internal use only.
    /// </summary>
    /// <param name="factory"></param>
    /// <param name="receiver"></param>
    /// <param name="setter"></param>
    /// <param name="existingValue"></param>
    /// <param name="format"></param>
    /// <param name="culture"></param>
    /// <returns></returns>
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Required to maintain compatibility")]
    public static EventCallback<ChangeEventArgs> CreateBinder(
        this EventCallbackFactory factory,
        object receiver,
        Func<DateTime, Task> setter,
        DateTime existingValue,
        string format,
        CultureInfo? culture = null)
    {
        return CreateBinderCoreAsync<DateTime>(factory, receiver, setter, culture, format, ConvertToDateTimeWithFormat);
    }

    /// <summary>
    /// For internal use only.
    /// </summary>
    /// <param name="factory"></param>
    /// <param name="receiver"></param>
    /// <param name="setter"></param>
    /// <param name="existingValue"></param>
    /// <param name="culture"></param>
    /// <returns></returns>
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Required to maintain compatibility")]
    public static EventCallback<ChangeEventArgs> CreateBinder(
        this EventCallbackFactory factory,
        object receiver,
        Action<DateTime?> setter,
        DateTime? existingValue,
        CultureInfo? culture = null)
    {
        return CreateBinderCore<DateTime?>(factory, receiver, setter, culture, ConvertToNullableDateTime);
    }

    /// <summary>
    /// For internal use only.
    /// </summary>
    /// <param name="factory"></param>
    /// <param name="receiver"></param>
    /// <param name="setter"></param>
    /// <param name="existingValue"></param>
    /// <param name="culture"></param>
    /// <returns></returns>
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Required to maintain compatibility")]
    public static EventCallback<ChangeEventArgs> CreateBinder(
        this EventCallbackFactory factory,
        object receiver,
        Func<DateTime?, Task> setter,
        DateTime? existingValue,
        CultureInfo? culture = null)
    {
        return CreateBinderCoreAsync<DateTime?>(factory, receiver, setter, culture, ConvertToNullableDateTime);
    }

    /// <summary>
    /// For internal use only.
    /// </summary>
    /// <param name="factory"></param>
    /// <param name="receiver"></param>
    /// <param name="setter"></param>
    /// <param name="existingValue"></param>
    /// <param name="format"></param>
    /// <param name="culture"></param>
    /// <returns></returns>
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Required to maintain compatibility")]
    public static EventCallback<ChangeEventArgs> CreateBinder(
        this EventCallbackFactory factory,
        object receiver,
        Action<DateTime?> setter,
        DateTime? existingValue,
        string format,
        CultureInfo? culture = null)
    {
        return CreateBinderCore<DateTime?>(factory, receiver, setter, culture, format, ConvertToNullableDateTimeWithFormat);
    }

    /// <summary>
    /// For internal use only.
    /// </summary>
    /// <param name="factory"></param>
    /// <param name="receiver"></param>
    /// <param name="setter"></param>
    /// <param name="existingValue"></param>
    /// <param name="format"></param>
    /// <param name="culture"></param>
    /// <returns></returns>
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Required to maintain compatibility")]
    public static EventCallback<ChangeEventArgs> CreateBinder(
        this EventCallbackFactory factory,
        object receiver,
        Func<DateTime?, Task> setter,
        DateTime? existingValue,
        string format,
        CultureInfo? culture = null)
    {
        return CreateBinderCoreAsync<DateTime?>(factory, receiver, setter, culture, format, ConvertToNullableDateTimeWithFormat);
    }

    /// <summary>
    /// For internal use only.
    /// </summary>
    /// <param name="factory"></param>
    /// <param name="receiver"></param>
    /// <param name="setter"></param>
    /// <param name="existingValue"></param>
    /// <param name="culture"></param>
    /// <returns></returns>
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Required to maintain compatibility")]
    public static EventCallback<ChangeEventArgs> CreateBinder(
        this EventCallbackFactory factory,
        object receiver,
        Action<DateTimeOffset> setter,
        DateTimeOffset existingValue,
        CultureInfo? culture = null)
    {
        return CreateBinderCore<DateTimeOffset>(factory, receiver, setter, culture, ConvertToDateTimeOffset);
    }

    /// <summary>
    /// For internal use only.
    /// </summary>
    /// <param name="factory"></param>
    /// <param name="receiver"></param>
    /// <param name="setter"></param>
    /// <param name="existingValue"></param>
    /// <param name="culture"></param>
    /// <returns></returns>
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Required to maintain compatibility")]
    public static EventCallback<ChangeEventArgs> CreateBinder(
        this EventCallbackFactory factory,
        object receiver,
        Func<DateTimeOffset, Task> setter,
        DateTimeOffset existingValue,
        CultureInfo? culture = null)
    {
        return CreateBinderCoreAsync<DateTimeOffset>(factory, receiver, setter, culture, ConvertToDateTimeOffset);
    }

    /// <summary>
    /// For internal use only.
    /// </summary>
    /// <param name="factory"></param>
    /// <param name="receiver"></param>
    /// <param name="setter"></param>
    /// <param name="existingValue"></param>
    /// <param name="format"></param>
    /// <param name="culture"></param>
    /// <returns></returns>
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Required to maintain compatibility")]
    public static EventCallback<ChangeEventArgs> CreateBinder(
        this EventCallbackFactory factory,
        object receiver,
        Action<DateTimeOffset> setter,
        DateTimeOffset existingValue,
        string format,
        CultureInfo? culture = null)
    {
        return CreateBinderCore<DateTimeOffset>(factory, receiver, setter, culture, format, ConvertToDateTimeOffsetWithFormat);
    }

    /// <summary>
    /// For internal use only.
    /// </summary>
    /// <param name="factory"></param>
    /// <param name="receiver"></param>
    /// <param name="setter"></param>
    /// <param name="existingValue"></param>
    /// <param name="format"></param>
    /// <param name="culture"></param>
    /// <returns></returns>
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Required to maintain compatibility")]
    public static EventCallback<ChangeEventArgs> CreateBinder(
        this EventCallbackFactory factory,
        object receiver,
        Func<DateTimeOffset, Task> setter,
        DateTimeOffset existingValue,
        string format,
        CultureInfo? culture = null)
    {
        return CreateBinderCoreAsync<DateTimeOffset>(factory, receiver, setter, culture, format, ConvertToDateTimeOffsetWithFormat);
    }

    /// <summary>
    /// For internal use only.
    /// </summary>
    /// <param name="factory"></param>
    /// <param name="receiver"></param>
    /// <param name="setter"></param>
    /// <param name="existingValue"></param>
    /// <param name="culture"></param>
    /// <returns></returns>
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Required to maintain compatibility")]
    public static EventCallback<ChangeEventArgs> CreateBinder(
        this EventCallbackFactory factory,
        object receiver,
        Action<DateTimeOffset?> setter,
        DateTimeOffset? existingValue,
        CultureInfo? culture = null)
    {
        return CreateBinderCore<DateTimeOffset?>(factory, receiver, setter, culture, ConvertToNullableDateTimeOffset);
    }

    /// <summary>
    /// For internal use only.
    /// </summary>
    /// <param name="factory"></param>
    /// <param name="receiver"></param>
    /// <param name="setter"></param>
    /// <param name="existingValue"></param>
    /// <param name="culture"></param>
    /// <returns></returns>
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Required to maintain compatibility")]
    public static EventCallback<ChangeEventArgs> CreateBinder(
        this EventCallbackFactory factory,
        object receiver,
        Func<DateTimeOffset?, Task> setter,
        DateTimeOffset? existingValue,
        CultureInfo? culture = null)
    {
        return CreateBinderCoreAsync<DateTimeOffset?>(factory, receiver, setter, culture, ConvertToNullableDateTimeOffset);
    }

    /// <summary>
    /// For internal use only.
    /// </summary>
    /// <param name="factory"></param>
    /// <param name="receiver"></param>
    /// <param name="setter"></param>
    /// <param name="existingValue"></param>
    /// <param name="format"></param>
    /// <param name="culture"></param>
    /// <returns></returns>
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Required to maintain compatibility")]
    public static EventCallback<ChangeEventArgs> CreateBinder(
        this EventCallbackFactory factory,
        object receiver,
        Action<DateTimeOffset?> setter,
        DateTimeOffset? existingValue,
        string format,
        CultureInfo? culture = null)
    {
        return CreateBinderCore<DateTimeOffset?>(factory, receiver, setter, culture, format, ConvertToNullableDateTimeOffsetWithFormat);
    }

    /// <summary>
    /// For internal use only.
    /// </summary>
    /// <param name="factory"></param>
    /// <param name="receiver"></param>
    /// <param name="setter"></param>
    /// <param name="existingValue"></param>
    /// <param name="format"></param>
    /// <param name="culture"></param>
    /// <returns></returns>
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Required to maintain compatibility")]
    public static EventCallback<ChangeEventArgs> CreateBinder(
        this EventCallbackFactory factory,
        object receiver,
        Func<DateTimeOffset?, Task> setter,
        DateTimeOffset? existingValue,
        string format,
        CultureInfo? culture = null)
    {
        return CreateBinderCoreAsync<DateTimeOffset?>(factory, receiver, setter, culture, format, ConvertToNullableDateTimeOffsetWithFormat);
    }

    /// <summary>
    /// For internal use only.
    /// </summary>
    /// <param name="factory"></param>
    /// <param name="receiver"></param>
    /// <param name="setter"></param>
    /// <param name="existingValue"></param>
    /// <param name="culture"></param>
    /// <returns></returns>
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Required to maintain compatibility")]
    public static EventCallback<ChangeEventArgs> CreateBinder(
        this EventCallbackFactory factory,
        object receiver,
        Action<DateOnly> setter,
        DateOnly existingValue,
        CultureInfo? culture = null)
    {
        return CreateBinderCore<DateOnly>(factory, receiver, setter, culture, ConvertToDateOnly);
    }

    /// <summary>
    /// For internal use only.
    /// </summary>
    /// <param name="factory"></param>
    /// <param name="receiver"></param>
    /// <param name="setter"></param>
    /// <param name="existingValue"></param>
    /// <param name="culture"></param>
    /// <returns></returns>
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Required to maintain compatibility")]
    public static EventCallback<ChangeEventArgs> CreateBinder(
        this EventCallbackFactory factory,
        object receiver,
        Func<DateOnly, Task> setter,
        DateOnly existingValue,
        CultureInfo? culture = null)
    {
        return CreateBinderCoreAsync<DateOnly>(factory, receiver, setter, culture, ConvertToDateOnly);
    }

    /// <summary>
    /// For internal use only.
    /// </summary>
    /// <param name="factory"></param>
    /// <param name="receiver"></param>
    /// <param name="setter"></param>
    /// <param name="existingValue"></param>
    /// <param name="format"></param>
    /// <param name="culture"></param>
    /// <returns></returns>
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Required to maintain compatibility")]
    public static EventCallback<ChangeEventArgs> CreateBinder(
        this EventCallbackFactory factory,
        object receiver,
        Action<DateOnly> setter,
        DateOnly existingValue,
        string format,
        CultureInfo? culture = null)
    {
        return CreateBinderCore<DateOnly>(factory, receiver, setter, culture, format, ConvertToDateOnlyWithFormat);
    }

    /// <summary>
    /// For internal use only.
    /// </summary>
    /// <param name="factory"></param>
    /// <param name="receiver"></param>
    /// <param name="setter"></param>
    /// <param name="existingValue"></param>
    /// <param name="format"></param>
    /// <param name="culture"></param>
    /// <returns></returns>
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Required to maintain compatibility")]
    public static EventCallback<ChangeEventArgs> CreateBinder(
        this EventCallbackFactory factory,
        object receiver,
        Func<DateOnly, Task> setter,
        DateOnly existingValue,
        string format,
        CultureInfo? culture = null)
    {
        return CreateBinderCoreAsync<DateOnly>(factory, receiver, setter, culture, format, ConvertToDateOnlyWithFormat);
    }

    /// <summary>
    /// For internal use only.
    /// </summary>
    /// <param name="factory"></param>
    /// <param name="receiver"></param>
    /// <param name="setter"></param>
    /// <param name="existingValue"></param>
    /// <param name="culture"></param>
    /// <returns></returns>
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Required to maintain compatibility")]
    public static EventCallback<ChangeEventArgs> CreateBinder(
        this EventCallbackFactory factory,
        object receiver,
        Action<DateOnly?> setter,
        DateOnly? existingValue,
        CultureInfo? culture = null)
    {
        return CreateBinderCore<DateOnly?>(factory, receiver, setter, culture, ConvertToNullableDateOnly);
    }

    /// <summary>
    /// For internal use only.
    /// </summary>
    /// <param name="factory"></param>
    /// <param name="receiver"></param>
    /// <param name="setter"></param>
    /// <param name="existingValue"></param>
    /// <param name="culture"></param>
    /// <returns></returns>
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Required to maintain compatibility")]
    public static EventCallback<ChangeEventArgs> CreateBinder(
        this EventCallbackFactory factory,
        object receiver,
        Func<DateOnly?, Task> setter,
        DateOnly? existingValue,
        CultureInfo? culture = null)
    {
        return CreateBinderCoreAsync<DateOnly?>(factory, receiver, setter, culture, ConvertToNullableDateOnly);
    }

    /// <summary>
    /// For internal use only.
    /// </summary>
    /// <param name="factory"></param>
    /// <param name="receiver"></param>
    /// <param name="setter"></param>
    /// <param name="existingValue"></param>
    /// <param name="format"></param>
    /// <param name="culture"></param>
    /// <returns></returns>
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Required to maintain compatibility")]
    public static EventCallback<ChangeEventArgs> CreateBinder(
        this EventCallbackFactory factory,
        object receiver,
        Action<DateOnly?> setter,
        DateOnly? existingValue,
        string format,
        CultureInfo? culture = null)
    {
        return CreateBinderCore<DateOnly?>(factory, receiver, setter, culture, format, ConvertToNullableDateOnlyWithFormat);
    }

    /// <summary>
    /// For internal use only.
    /// </summary>
    /// <param name="factory"></param>
    /// <param name="receiver"></param>
    /// <param name="setter"></param>
    /// <param name="existingValue"></param>
    /// <param name="format"></param>
    /// <param name="culture"></param>
    /// <returns></returns>
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Required to maintain compatibility")]
    public static EventCallback<ChangeEventArgs> CreateBinder(
        this EventCallbackFactory factory,
        object receiver,
        Func<DateOnly?, Task> setter,
        DateOnly? existingValue,
        string format,
        CultureInfo? culture = null)
    {
        return CreateBinderCoreAsync<DateOnly?>(factory, receiver, setter, culture, format, ConvertToNullableDateOnlyWithFormat);
    }

    /// <summary>
    /// For internal use only.
    /// </summary>
    /// <param name="factory"></param>
    /// <param name="receiver"></param>
    /// <param name="setter"></param>
    /// <param name="existingValue"></param>
    /// <param name="culture"></param>
    /// <returns></returns>
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Required to maintain compatibility")]
    public static EventCallback<ChangeEventArgs> CreateBinder(
        this EventCallbackFactory factory,
        object receiver,
        Action<TimeOnly> setter,
        TimeOnly existingValue,
        CultureInfo? culture = null)
    {
        return CreateBinderCore<TimeOnly>(factory, receiver, setter, culture, ConvertToTimeOnly);
    }

    /// <summary>
    /// For internal use only.
    /// </summary>
    /// <param name="factory"></param>
    /// <param name="receiver"></param>
    /// <param name="setter"></param>
    /// <param name="existingValue"></param>
    /// <param name="culture"></param>
    /// <returns></returns>
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Required to maintain compatibility")]
    public static EventCallback<ChangeEventArgs> CreateBinder(
        this EventCallbackFactory factory,
        object receiver,
        Func<TimeOnly, Task> setter,
        TimeOnly existingValue,
        CultureInfo? culture = null)
    {
        return CreateBinderCoreAsync<TimeOnly>(factory, receiver, setter, culture, ConvertToTimeOnly);
    }

    /// <summary>
    /// For internal use only.
    /// </summary>
    /// <param name="factory"></param>
    /// <param name="receiver"></param>
    /// <param name="setter"></param>
    /// <param name="existingValue"></param>
    /// <param name="format"></param>
    /// <param name="culture"></param>
    /// <returns></returns>
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Required to maintain compatibility")]
    public static EventCallback<ChangeEventArgs> CreateBinder(
        this EventCallbackFactory factory,
        object receiver,
        Action<TimeOnly> setter,
        TimeOnly existingValue,
        string format,
        CultureInfo? culture = null)
    {
        return CreateBinderCore<TimeOnly>(factory, receiver, setter, culture, format, ConvertToTimeOnlyWithFormat);
    }

    /// <summary>
    /// For internal use only.
    /// </summary>
    /// <param name="factory"></param>
    /// <param name="receiver"></param>
    /// <param name="setter"></param>
    /// <param name="existingValue"></param>
    /// <param name="format"></param>
    /// <param name="culture"></param>
    /// <returns></returns>
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Required to maintain compatibility")]
    public static EventCallback<ChangeEventArgs> CreateBinder(
        this EventCallbackFactory factory,
        object receiver,
        Func<TimeOnly, Task> setter,
        TimeOnly existingValue,
        string format,
        CultureInfo? culture = null)
    {
        return CreateBinderCoreAsync<TimeOnly>(factory, receiver, setter, culture, format, ConvertToTimeOnlyWithFormat);
    }

    /// <summary>
    /// For internal use only.
    /// </summary>
    /// <param name="factory"></param>
    /// <param name="receiver"></param>
    /// <param name="setter"></param>
    /// <param name="existingValue"></param>
    /// <param name="culture"></param>
    /// <returns></returns>
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Required to maintain compatibility")]
    public static EventCallback<ChangeEventArgs> CreateBinder(
        this EventCallbackFactory factory,
        object receiver,
        Action<TimeOnly?> setter,
        TimeOnly? existingValue,
        CultureInfo? culture = null)
    {
        return CreateBinderCore<TimeOnly?>(factory, receiver, setter, culture, ConvertToNullableTimeOnly);
    }

    /// <summary>
    /// For internal use only.
    /// </summary>
    /// <param name="factory"></param>
    /// <param name="receiver"></param>
    /// <param name="setter"></param>
    /// <param name="existingValue"></param>
    /// <param name="culture"></param>
    /// <returns></returns>
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Required to maintain compatibility")]
    public static EventCallback<ChangeEventArgs> CreateBinder(
        this EventCallbackFactory factory,
        object receiver,
        Func<TimeOnly?, Task> setter,
        TimeOnly? existingValue,
        CultureInfo? culture = null)
    {
        return CreateBinderCoreAsync<TimeOnly?>(factory, receiver, setter, culture, ConvertToNullableTimeOnly);
    }

    /// <summary>
    /// For internal use only.
    /// </summary>
    /// <param name="factory"></param>
    /// <param name="receiver"></param>
    /// <param name="setter"></param>
    /// <param name="existingValue"></param>
    /// <param name="format"></param>
    /// <param name="culture"></param>
    /// <returns></returns>
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Required to maintain compatibility")]
    public static EventCallback<ChangeEventArgs> CreateBinder(
        this EventCallbackFactory factory,
        object receiver,
        Action<TimeOnly?> setter,
        TimeOnly? existingValue,
        string format,
        CultureInfo? culture = null)
    {
        return CreateBinderCore<TimeOnly?>(factory, receiver, setter, culture, format, ConvertToNullableTimeOnlyWithFormat);
    }

    /// <summary>
    /// For internal use only.
    /// </summary>
    /// <param name="factory"></param>
    /// <param name="receiver"></param>
    /// <param name="setter"></param>
    /// <param name="existingValue"></param>
    /// <param name="format"></param>
    /// <param name="culture"></param>
    /// <returns></returns>
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Required to maintain compatibility")]
    public static EventCallback<ChangeEventArgs> CreateBinder(
        this EventCallbackFactory factory,
        object receiver,
        Func<TimeOnly?, Task> setter,
        TimeOnly? existingValue,
        string format,
        CultureInfo? culture = null)
    {
        return CreateBinderCoreAsync<TimeOnly?>(factory, receiver, setter, culture, format, ConvertToNullableTimeOnlyWithFormat);
    }

    /// <summary>
    /// For internal use only.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="factory"></param>
    /// <param name="receiver"></param>
    /// <param name="setter"></param>
    /// <param name="existingValue"></param>
    /// <param name="culture"></param>
    /// <returns></returns>
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Required to maintain compatibility")]
    public static EventCallback<ChangeEventArgs> CreateBinder<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(
        this EventCallbackFactory factory,
        object receiver,
        Action<T> setter,
        T existingValue,
        CultureInfo? culture = null)
    {
        return CreateBinderCore<T>(factory, receiver, setter, culture, ParserDelegateCache.Get<T>());
    }

    /// <summary>
    /// For internal use only.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="factory"></param>
    /// <param name="receiver"></param>
    /// <param name="setter"></param>
    /// <param name="existingValue"></param>
    /// <param name="culture"></param>
    /// <returns></returns>
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Required to maintain compatibility")]
    public static EventCallback<ChangeEventArgs> CreateBinder<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(
        this EventCallbackFactory factory,
        object receiver,
        Func<T, Task> setter,
        T existingValue,
        CultureInfo? culture = null)
    {
        return CreateBinderCoreAsync<T>(factory, receiver, setter, culture, ParserDelegateCache.Get<T>());
    }

    private static EventCallback<ChangeEventArgs> CreateBinderCore<T>(
        this EventCallbackFactory factory,
        object receiver,
        Action<T> setter,
        CultureInfo? culture,
        BindConverter.BindParser<T> converter)
    {
        Action<ChangeEventArgs> callback = e =>
        {
            T? value = default;
            var converted = false;
            try
            {
                converted = converter(e.Value, culture, out value);
            }
            catch
            {
            }

            // We only invoke the setter if the conversion didn't throw, or if the newly-entered value is empty.
            // If the user entered some non-empty value we couldn't parse, we leave the state of the .NET field
            // unchanged, which for a two-way binding results in the UI reverting to its previous valid state
            // because the diff will see the current .NET output no longer matches the render tree since we
            // patched it to reflect the state of the UI.
            //
            // This reversion behavior is valuable because alternatives are problematic:
            // - If we assigned default(T) on failure, the user would lose whatever data they were editing,
            //   for example if they accidentally pressed an alphabetical key while editing a number with
            //   @bind:event="oninput"
            // - If the diff mechanism didn't revert to the previous good value, the user wouldn't necessarily
            //   know that the data they are submitting is different from what they think they've typed
            if (converted)
            {
                setter(value!);
            }
            else if (string.Empty.Equals(e.Value))
            {
                setter(default!);
            }
        };
        return factory.Create<ChangeEventArgs>(receiver, callback);
    }

    private static EventCallback<ChangeEventArgs> CreateBinderCoreAsync<T>(
        this EventCallbackFactory factory,
        object receiver,
        Func<T, Task> setter,
        CultureInfo? culture,
        BindConverter.BindParser<T> converter)
    {
        Func<ChangeEventArgs, Task> callback = async e =>
        {
            T? value = default;
            var converted = false;
            try
            {
                converted = converter(e.Value, culture, out value);
            }
            catch
            {
            }

            // We only invoke the setter if the conversion didn't throw, or if the newly-entered value is empty.
            // If the user entered some non-empty value we couldn't parse, we leave the state of the .NET field
            // unchanged, which for a two-way binding results in the UI reverting to its previous valid state
            // because the diff will see the current .NET output no longer matches the render tree since we
            // patched it to reflect the state of the UI.
            //
            // This reversion behavior is valuable because alternatives are problematic:
            // - If we assigned default(T) on failure, the user would lose whatever data they were editing,
            //   for example if they accidentally pressed an alphabetical key while editing a number with
            //   @bind:event="oninput"
            // - If the diff mechanism didn't revert to the previous good value, the user wouldn't necessarily
            //   know that the data they are submitting is different from what they think they've typed
            if (converted)
            {
                await setter(value!);
            }
            else if (string.Empty.Equals(e.Value))
            {
                await setter(default!);
            }
        };
        return factory.Create<ChangeEventArgs>(receiver, callback);
    }

    private static EventCallback<ChangeEventArgs> CreateBinderCore<T>(
        this EventCallbackFactory factory,
        object receiver,
        Action<T> setter,
        CultureInfo? culture,
        string format,
        BindConverter.BindParserWithFormat<T> converter)
    {
        Action<ChangeEventArgs> callback = e =>
        {
            T? value = default;
            var converted = false;
            try
            {
                converted = converter(e.Value, culture, format, out value);
            }
            catch
            {
            }

            // We only invoke the setter if the conversion didn't throw, or if the newly-entered value is empty.
            // If the user entered some non-empty value we couldn't parse, we leave the state of the .NET field
            // unchanged, which for a two-way binding results in the UI reverting to its previous valid state
            // because the diff will see the current .NET output no longer matches the render tree since we
            // patched it to reflect the state of the UI.
            //
            // This reversion behavior is valuable because alternatives are problematic:
            // - If we assigned default(T) on failure, the user would lose whatever data they were editing,
            //   for example if they accidentally pressed an alphabetical key while editing a number with
            //   @bind:event="oninput"
            // - If the diff mechanism didn't revert to the previous good value, the user wouldn't necessarily
            //   know that the data they are submitting is different from what they think they've typed
            if (converted)
            {
                setter(value!);
            }
            else if (string.Empty.Equals(e.Value))
            {
                setter(default!);
            }
        };
        return factory.Create<ChangeEventArgs>(receiver, callback);
    }

    private static EventCallback<ChangeEventArgs> CreateBinderCoreAsync<T>(
        this EventCallbackFactory factory,
        object receiver,
        Func<T, Task> setter,
        CultureInfo? culture,
        string format,
        BindConverter.BindParserWithFormat<T> converter)
    {
        Func<ChangeEventArgs, Task> callback = async e =>
        {
            T? value = default;
            var converted = false;
            try
            {
                converted = converter(e.Value, culture, format, out value);
            }
            catch
            {
            }

            // We only invoke the setter if the conversion didn't throw, or if the newly-entered value is empty.
            // If the user entered some non-empty value we couldn't parse, we leave the state of the .NET field
            // unchanged, which for a two-way binding results in the UI reverting to its previous valid state
            // because the diff will see the current .NET output no longer matches the render tree since we
            // patched it to reflect the state of the UI.
            //
            // This reversion behavior is valuable because alternatives are problematic:
            // - If we assigned default(T) on failure, the user would lose whatever data they were editing,
            //   for example if they accidentally pressed an alphabetical key while editing a number with
            //   @bind:event="oninput"
            // - If the diff mechanism didn't revert to the previous good value, the user wouldn't necessarily
            //   know that the data they are submitting is different from what they think they've typed
            if (converted)
            {
                await setter(value!);
            }
            else if (string.Empty.Equals(e.Value))
            {
                await setter(default!);
            }
        };
        return factory.Create<ChangeEventArgs>(receiver, callback);
    }
}
