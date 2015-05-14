// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Framework.Internal;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNet.Authentication.MicrosoftAccount
{
    /// <summary>
    /// Contains static methods that allow to extract user's information from a <see cref="JObject"/>
    /// instance retrieved from Google after a successful authentication process.
    /// </summary>
    public static class MicrosoftAccountAuthenticationHelper
    {
        /// <summary>
        /// Gets the Microsoft Account user ID.
        /// </summary>
        public static string GetId([NotNull] JObject user) => user.Value<string>("id");

        /// <summary>
        /// Gets the user's name.
        /// </summary>
        public static string GetName([NotNull] JObject user) => user.Value<string>("name");

        /// <summary>
        /// Gets the user's first name.
        /// </summary>
        public static string GetFirstName([NotNull] JObject user) => user.Value<string>("first_name");

        /// <summary>
        /// Gets the user's last name.
        /// </summary>
        public static string GetLastName([NotNull] JObject user) => user.Value<string>("last_name");

        /// <summary>
        /// Gets the user's email address.
        /// </summary>
        public static string GetEmail([NotNull] JObject user) => user.Value<JObject>("emails")
                                                                    ?.Value<string>("preferred");
    }
}
