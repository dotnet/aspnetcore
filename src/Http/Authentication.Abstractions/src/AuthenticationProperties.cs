// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text.Json.Serialization;

namespace Microsoft.AspNetCore.Authentication;

/// <summary>
/// Dictionary used to store state values about the authentication session.
/// </summary>
public class AuthenticationProperties
{
    internal const string IssuedUtcKey = ".issued";
    internal const string ExpiresUtcKey = ".expires";
    internal const string IsPersistentKey = ".persistent";
    internal const string RedirectUriKey = ".redirect";
    internal const string RefreshKey = ".refresh";
    internal const string UtcDateTimeFormat = "r";

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthenticationProperties"/> class.
    /// </summary>
    public AuthenticationProperties()
        : this(items: null, parameters: null)
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthenticationProperties"/> class.
    /// </summary>
    /// <param name="items">State values dictionary to use.</param>
    [JsonConstructor]
    public AuthenticationProperties(IDictionary<string, string?> items)
        : this(items, parameters: null)
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthenticationProperties"/> class.
    /// </summary>
    /// <param name="items">State values dictionary to use.</param>
    /// <param name="parameters">Parameters dictionary to use.</param>
    public AuthenticationProperties(IDictionary<string, string?>? items, IDictionary<string, object?>? parameters)
    {
        Items = items ?? new Dictionary<string, string?>(StringComparer.Ordinal);
        Parameters = parameters ?? new Dictionary<string, object?>(StringComparer.Ordinal);
    }

    /// <summary>
    /// Return a copy.
    /// </summary>
    /// <returns>A copy.</returns>
    public AuthenticationProperties Clone()
        => new AuthenticationProperties(
            new Dictionary<string, string?>(Items, StringComparer.Ordinal),
            new Dictionary<string, object?>(Parameters, StringComparer.Ordinal));

    /// <summary>
    /// State values about the authentication session.
    /// </summary>
    public IDictionary<string, string?> Items { get; }

    /// <summary>
    /// Collection of parameters that are passed to the authentication handler. These are not intended for
    /// serialization or persistence, only for flowing data between call sites.
    /// </summary>
    [JsonIgnore]
    public IDictionary<string, object?> Parameters { get; }

    /// <summary>
    /// Gets or sets whether the authentication session is persisted across multiple requests.
    /// </summary>
    [JsonIgnore]
    public bool IsPersistent
    {
        get => GetString(IsPersistentKey) != null;
        set => SetString(IsPersistentKey, value ? string.Empty : null);
    }

    /// <summary>
    /// Gets or sets the full path or absolute URI to be used as an http redirect response value.
    /// </summary>
    [JsonIgnore]
    public string? RedirectUri
    {
        get => GetString(RedirectUriKey);
        set => SetString(RedirectUriKey, value);
    }

    /// <summary>
    /// Gets or sets the time at which the authentication ticket was issued.
    /// </summary>
    [JsonIgnore]
    public DateTimeOffset? IssuedUtc
    {
        get => GetDateTimeOffset(IssuedUtcKey);
        set => SetDateTimeOffset(IssuedUtcKey, value);
    }

    /// <summary>
    /// Gets or sets the time at which the authentication ticket expires.
    /// </summary>
    [JsonIgnore]
    public DateTimeOffset? ExpiresUtc
    {
        get => GetDateTimeOffset(ExpiresUtcKey);
        set => SetDateTimeOffset(ExpiresUtcKey, value);
    }

    /// <summary>
    /// Gets or sets if refreshing the authentication session should be allowed.
    /// </summary>
    [JsonIgnore]
    public bool? AllowRefresh
    {
        get => GetBool(RefreshKey);
        set => SetBool(RefreshKey, value);
    }

    /// <summary>
    /// Get a string value from the <see cref="Items"/> collection.
    /// </summary>
    /// <param name="key">Property key.</param>
    /// <returns>Retrieved value or <c>null</c> if the property is not set.</returns>
    public string? GetString(string key)
    {
        return Items.TryGetValue(key, out var value) ? value : null;
    }

    /// <summary>
    /// Set or remove a string value from the <see cref="Items"/> collection.
    /// </summary>
    /// <param name="key">Property key.</param>
    /// <param name="value">Value to set or <see langword="null" /> to remove the property.</param>
    public void SetString(string key, string? value)
    {
        if (value != null)
        {
            Items[key] = value;
        }
        else
        {
            Items.Remove(key);
        }
    }

    /// <summary>
    /// Get a parameter from the <see cref="Parameters"/> collection.
    /// </summary>
    /// <typeparam name="T">Parameter type.</typeparam>
    /// <param name="key">Parameter key.</param>
    /// <returns>Retrieved value or the default value if the property is not set.</returns>
    public T? GetParameter<T>(string key)
        => Parameters.TryGetValue(key, out var obj) && obj is T value ? value : default;

    /// <summary>
    /// Set a parameter value in the <see cref="Parameters"/> collection.
    /// </summary>
    /// <typeparam name="T">Parameter type.</typeparam>
    /// <param name="key">Parameter key.</param>
    /// <param name="value">Value to set.</param>
    public void SetParameter<T>(string key, T value)
        => Parameters[key] = value;

    /// <summary>
    /// Get a nullable <see cref="bool"/> from the <see cref="Items"/> collection.
    /// </summary>
    /// <param name="key">Property key.</param>
    /// <returns>Retrieved value or <see langword="null" /> if the property is not set.</returns>
    protected bool? GetBool(string key)
    {
        if (Items.TryGetValue(key, out var value) && bool.TryParse(value, out var boolValue))
        {
            return boolValue;
        }
        return null;
    }

    /// <summary>
    /// Set or remove a <see cref="bool"/> value in the <see cref="Items"/> collection.
    /// </summary>
    /// <param name="key">Property key.</param>
    /// <param name="value">Value to set or <see langword="null" /> to remove the property.</param>
    protected void SetBool(string key, bool? value)
    {
        if (value.HasValue)
        {
            Items[key] = value.GetValueOrDefault().ToString();
        }
        else
        {
            Items.Remove(key);
        }
    }

    /// <summary>
    /// Get a nullable <see cref="DateTimeOffset"/> value from the <see cref="Items"/> collection.
    /// </summary>
    /// <param name="key">Property key.</param>
    /// <returns>Retrieved value or <see langword="null" /> if the property is not set.</returns>
    protected DateTimeOffset? GetDateTimeOffset(string key)
    {
        if (Items.TryGetValue(key, out var value)
            && DateTimeOffset.TryParseExact(value, UtcDateTimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dateTimeOffset))
        {
            return dateTimeOffset;
        }
        return null;
    }

    /// <summary>
    /// Sets or removes a <see cref="DateTimeOffset" /> value in the <see cref="Items"/> collection.
    /// </summary>
    /// <param name="key">Property key.</param>
    /// <param name="value">Value to set or <see langword="null" /> to remove the property.</param>
    protected void SetDateTimeOffset(string key, DateTimeOffset? value)
    {
        if (value.HasValue)
        {
            Items[key] = value.GetValueOrDefault().ToString(UtcDateTimeFormat, CultureInfo.InvariantCulture);
        }
        else
        {
            Items.Remove(key);
        }
    }
}
