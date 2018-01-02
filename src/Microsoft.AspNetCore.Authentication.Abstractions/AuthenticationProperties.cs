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
        /// Initializes a new instance of the <see cref="AuthenticationProperties"/> class
        /// </summary>
        public AuthenticationProperties()
            : this(items: null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthenticationProperties"/> class
        /// </summary>
        /// <param name="items"></param>
        public AuthenticationProperties(IDictionary<string, string> items)
        {
            Items = items ?? new Dictionary<string, string>(StringComparer.Ordinal);
        }

        /// <summary>
        /// State values about the authentication session.
        /// </summary>
        public IDictionary<string, string> Items { get; }

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

        private string GetString(string key)
        {
            return Items.TryGetValue(key, out string value) ? value : null;
        }

        private void SetString(string key, string value)
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

        private bool? GetBool(string key)
        {
            if (Items.TryGetValue(key, out string value) && bool.TryParse(value, out bool refresh))
            {
                return refresh;
            }
            return null;
        }

        private void SetBool(string key, bool? value)
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

        private DateTimeOffset? GetDateTimeOffset(string key)
        {
            if (Items.TryGetValue(key, out string value)
                && DateTimeOffset.TryParseExact(value, UtcDateTimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTimeOffset dateTimeOffset))
            {
                return dateTimeOffset;
            }
            return null;
        }

        private void SetDateTimeOffset(string key, DateTimeOffset? value)
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
