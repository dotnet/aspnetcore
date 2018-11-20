// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;

namespace Microsoft.AspNetCore.Http.Authentication
{
    /// <summary>
    /// Contains information describing an authentication provider.
    /// </summary>
    public class AuthenticationDescription
    {
        private const string DisplayNamePropertyKey = "DisplayName";
        private const string AuthenticationSchemePropertyKey = "AuthenticationScheme";

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthenticationDescription"/> class
        /// </summary>
        public AuthenticationDescription()
            : this(items: null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthenticationDescription"/> class
        /// </summary>
        /// <param name="items"></param>
        public AuthenticationDescription(IDictionary<string, object> items)
        {
            Items = items ?? new Dictionary<string, object>(StringComparer.Ordinal); ;
        }

        /// <summary>
        /// Contains metadata about the authentication provider.
        /// </summary>
        public IDictionary<string, object> Items { get; }

        /// <summary>
        /// Gets or sets the name used to reference the authentication middleware instance.
        /// </summary>
        public string AuthenticationScheme
        {
            get { return GetString(AuthenticationSchemePropertyKey); }
            set { Items[AuthenticationSchemePropertyKey] = value; }
        }

        /// <summary>
        /// Gets or sets the display name for the authentication provider.
        /// </summary>
        public string DisplayName
        {
            get { return GetString(DisplayNamePropertyKey); }
            set { Items[DisplayNamePropertyKey] = value; }
        }

        private string GetString(string name)
        {
            object value;
            if (Items.TryGetValue(name, out value))
            {
                return Convert.ToString(value, CultureInfo.InvariantCulture);
            }
            return null;
        }
    }
}
