// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;

namespace Microsoft.AspNetCore.Authentication
{
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
        public AuthenticationProperties(IDictionary<string, string> items)
            : this(items, parameters: null)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthenticationProperties"/> class.
        /// </summary>
        /// <param name="items">State values dictionary to use.</param>
        /// <param name="parameters">Parameters dictionary to use.</param>
        public AuthenticationProperties(IDictionary<string, string> items, IDictionary<string, object> parameters)
        {
            Items = items ?? new Dictionary<string, string>(StringComparer.Ordinal);
            Parameters = parameters ?? new Dictionary<string, object>(StringComparer.Ordinal);
        }

        /// <summary>
        /// State values about the authentication session.
        /// </summary>
        public IDictionary<string, string> Items { get; }

        /// <summary>
        /// Collection of parameters that are passed to the authentication handler. These are not intended for
        /// serialization or persistence, only for flowing data between call sites.
        /// </summary>
        public IDictionary<string, object> Parameters { get; }

        /// <summary>
        /// Gets or sets whether the authentication session is persisted across multiple requests.
        /// </summary>
        public bool IsPersistent
        {
            get => GetString(IsPersistentKey) != null;
            set => SetString(IsPersistentKey, value ? string.Empty : null);
        }

        /// <summary>
        /// Gets or sets the full path or absolute URI to be used as an http redirect response value.
        /// </summary>
        public string RedirectUri
        {
            get => GetString(RedirectUriKey);
            set => SetString(RedirectUriKey, value);
        }

        /// <summary>
        /// Gets or sets the time at which the authentication ticket was issued.
        /// </summary>
        public DateTimeOffset? IssuedUtc
        {
            get => GetDateTimeOffset(IssuedUtcKey);
            set => SetDateTimeOffset(IssuedUtcKey, value);
        }

        /// <summary>
        /// Gets or sets the time at which the authentication ticket expires.
        /// </summary>
        public DateTimeOffset? ExpiresUtc
        {
            get => GetDateTimeOffset(ExpiresUtcKey);
            set => SetDateTimeOffset(ExpiresUtcKey, value);
        }

        /// <summary>
        /// Gets or sets if refreshing the authentication session should be allowed.
        /// </summary>
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
        public string GetString(string key)
        {
            return Items.TryGetValue(key, out string value) ? value : null;
        }

        /// <summary>
        /// Set a string value in the <see cref="Items"/> collection.
        /// </summary>
        /// <param name="key">Property key.</param>
        /// <param name="value">Value to set or <c>null</c> to remove the property.</param>
        public void SetString(string key, string value)
        {
            if (value != null)
            {
                Items[key] = value;
            }
            else if (Items.ContainsKey(key))
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
        public T GetParameter<T>(string key)
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
        /// Get a bool value from the <see cref="Items"/> collection.
        /// </summary>
        /// <param name="key">Property key.</param>
        /// <returns>Retrieved value or <c>null</c> if the property is not set.</returns>
        protected bool? GetBool(string key)
        {
            if (Items.TryGetValue(key, out string value) && bool.TryParse(value, out bool boolValue))
            {
                return boolValue;
            }
            return null;
        }

        /// <summary>
        /// Set a bool value in the <see cref="Items"/> collection.
        /// </summary>
        /// <param name="key">Property key.</param>
        /// <param name="value">Value to set or <c>null</c> to remove the property.</param>
        protected void SetBool(string key, bool? value)
        {
            if (value.HasValue)
            {
                Items[key] = value.Value.ToString();
            }
            else if (Items.ContainsKey(key))
            {
                Items.Remove(key);
            }
        }

        /// <summary>
        /// Get a DateTimeOffset value from the <see cref="Items"/> collection.
        /// </summary>
        /// <param name="key">Property key.</param>
        /// <returns>Retrieved value or <c>null</c> if the property is not set.</returns>
        protected DateTimeOffset? GetDateTimeOffset(string key)
        {
            if (Items.TryGetValue(key, out string value)
                && DateTimeOffset.TryParseExact(value, UtcDateTimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTimeOffset dateTimeOffset))
            {
                return dateTimeOffset;
            }
            return null;
        }

        /// <summary>
        /// Set a DateTimeOffset value in the <see cref="Items"/> collection.
        /// </summary>
        /// <param name="key">Property key.</param>
        /// <param name="value">Value to set or <c>null</c> to remove the property.</param>
        protected void SetDateTimeOffset(string key, DateTimeOffset? value)
        {
            if (value.HasValue)
            {
                Items[key] = value.Value.ToString(UtcDateTimeFormat, CultureInfo.InvariantCulture);
            }
            else if (Items.ContainsKey(key))
            {
                Items.Remove(key);
            }
        }
    }
}
