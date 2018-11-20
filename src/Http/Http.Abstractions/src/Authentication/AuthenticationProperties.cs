// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;

namespace Microsoft.AspNetCore.Http.Authentication
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
            get { return Items.ContainsKey(IsPersistentKey); }
            set
            {
                if (Items.ContainsKey(IsPersistentKey))
                {
                    if (!value)
                    {
                        Items.Remove(IsPersistentKey);
                    }
                }
                else
                {
                    if (value)
                    {
                        Items.Add(IsPersistentKey, string.Empty);
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets the full path or absolute URI to be used as an HTTP redirect response value.
        /// </summary>
        public string RedirectUri
        {
            get
            {
                string value;
                return Items.TryGetValue(RedirectUriKey, out value) ? value : null;
            }
            set
            {
                if (value != null)
                {
                    Items[RedirectUriKey] = value;
                }
                else
                {
                    if (Items.ContainsKey(RedirectUriKey))
                    {
                        Items.Remove(RedirectUriKey);
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets the time at which the authentication ticket was issued.
        /// </summary>
        public DateTimeOffset? IssuedUtc
        {
            get
            {
                string value;
                if (Items.TryGetValue(IssuedUtcKey, out value))
                {
                    DateTimeOffset dateTimeOffset;
                    if (DateTimeOffset.TryParseExact(value, UtcDateTimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out dateTimeOffset))
                    {
                        return dateTimeOffset;
                    }
                }
                return null;
            }
            set
            {
                if (value.HasValue)
                {
                    Items[IssuedUtcKey] = value.Value.ToString(UtcDateTimeFormat, CultureInfo.InvariantCulture);
                }
                else
                {
                    if (Items.ContainsKey(IssuedUtcKey))
                    {
                        Items.Remove(IssuedUtcKey);
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets the time at which the authentication ticket expires.
        /// </summary>
        public DateTimeOffset? ExpiresUtc
        {
            get
            {
                string value;
                if (Items.TryGetValue(ExpiresUtcKey, out value))
                {
                    DateTimeOffset dateTimeOffset;
                    if (DateTimeOffset.TryParseExact(value, UtcDateTimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out dateTimeOffset))
                    {
                        return dateTimeOffset;
                    }
                }
                return null;
            }
            set
            {
                if (value.HasValue)
                {
                    Items[ExpiresUtcKey] = value.Value.ToString(UtcDateTimeFormat, CultureInfo.InvariantCulture);
                }
                else
                {
                    if (Items.ContainsKey(ExpiresUtcKey))
                    {
                        Items.Remove(ExpiresUtcKey);
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets if refreshing the authentication session should be allowed.
        /// </summary>
        public bool? AllowRefresh
        {
            get
            {
                string value;
                if (Items.TryGetValue(RefreshKey, out value))
                {
                    bool refresh;
                    if (bool.TryParse(value, out refresh))
                    {
                        return refresh;
                    }
                }
                return null;
            }
            set
            {
                if (value.HasValue)
                {
                    Items[RefreshKey] = value.Value.ToString();
                }
                else
                {
                    if (Items.ContainsKey(RefreshKey))
                    {
                        Items.Remove(RefreshKey);
                    }
                }
            }
        }
    }
}
