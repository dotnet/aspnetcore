// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using System;
using System.Collections.Generic;
using System.Globalization;

namespace Microsoft.AspNet.Abstractions.Security
{
    /// <summary>
    /// Contains information describing an authentication provider.
    /// </summary>
    public class AuthenticationDescription
    {
        private const string CaptionPropertyKey = "Caption";
        private const string AuthenticationTypePropertyKey = "AuthenticationType";

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthenticationDescription"/> class
        /// </summary>
        public AuthenticationDescription()
        {
            Dictionary = new Dictionary<string, object>(StringComparer.Ordinal);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthenticationDescription"/> class
        /// </summary>
        /// <param name="properties"></param>
        public AuthenticationDescription(IDictionary<string, object> properties)
        {
            if (properties == null)
            {
                throw new ArgumentNullException("properties");
            }
            Dictionary = properties;
        }

        /// <summary>
        /// Contains metadata about the authentication provider.
        /// </summary>
        public IDictionary<string, object> Dictionary { get; private set; }

        /// <summary>
        /// Gets or sets the name used to reference the authentication middleware instance.
        /// </summary>
        public string AuthenticationType
        {
            get { return GetString(AuthenticationTypePropertyKey); }
            set { Dictionary[AuthenticationTypePropertyKey] = value; }
        }

        /// <summary>
        /// Gets or sets the display name for the authentication provider.
        /// </summary>
        public string Caption
        {
            get { return GetString(CaptionPropertyKey); }
            set { Dictionary[CaptionPropertyKey] = value; }
        }

        private string GetString(string name)
        {
            object value;
            if (Dictionary.TryGetValue(name, out value))
            {
                return Convert.ToString(value, CultureInfo.InvariantCulture);
            }
            return null;
        }
    }
}
