// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Microsoft.AspNet.Http.Authentication
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
            : this(dictionary: null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthenticationProperties"/> class
        /// </summary>
        /// <param name="dictionary"></param>
        public AuthenticationProperties(IDictionary<string, string> dictionary)
        {
            Dictionary = dictionary ?? new Dictionary<string, string>(StringComparer.Ordinal);
        }

        /// <summary>
        /// State values about the authentication session.
        /// </summary>
        public IDictionary<string, string> Dictionary { get; private set; }

        /// <summary>
        /// Gets or sets whether the authentication session is persisted across multiple requests.
        /// </summary>
        public bool IsPersistent
        {
            get { return Dictionary.ContainsKey(IsPersistentKey); }
            set
            {
                if (Dictionary.ContainsKey(IsPersistentKey))
                {
                    if (!value)
                    {
                        Dictionary.Remove(IsPersistentKey);
                    }
                }
                else
                {
                    if (value)
                    {
                        Dictionary.Add(IsPersistentKey, string.Empty);
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets the full path or absolute URI to be used as an http redirect response value. 
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1056:UriPropertiesShouldNotBeStrings", Justification = "By design")]
        public string RedirectUri
        {
            get
            {
                string value;
                return Dictionary.TryGetValue(RedirectUriKey, out value) ? value : null;
            }
            set
            {
                if (value != null)
                {
                    Dictionary[RedirectUriKey] = value;
                }
                else
                {
                    if (Dictionary.ContainsKey(RedirectUriKey))
                    {
                        Dictionary.Remove(RedirectUriKey);
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
                if (Dictionary.TryGetValue(IssuedUtcKey, out value))
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
                    Dictionary[IssuedUtcKey] = value.Value.ToString(UtcDateTimeFormat, CultureInfo.InvariantCulture);
                }
                else
                {
                    if (Dictionary.ContainsKey(IssuedUtcKey))
                    {
                        Dictionary.Remove(IssuedUtcKey);
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
                if (Dictionary.TryGetValue(ExpiresUtcKey, out value))
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
                    Dictionary[ExpiresUtcKey] = value.Value.ToString(UtcDateTimeFormat, CultureInfo.InvariantCulture);
                }
                else
                {
                    if (Dictionary.ContainsKey(ExpiresUtcKey))
                    {
                        Dictionary.Remove(ExpiresUtcKey);
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
                if (Dictionary.TryGetValue(RefreshKey, out value))
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
                    Dictionary[RefreshKey] = value.Value.ToString();
                }
                else
                {
                    if (Dictionary.ContainsKey(RefreshKey))
                    {
                        Dictionary.Remove(RefreshKey);
                    }
                }
            }
        }
    }
}
