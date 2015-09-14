// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Framework.Internal;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNet.Authentication.Facebook
{
    /// <summary>
    /// Contains static methods that allow to extract user's information from a <see cref="JObject"/>
    /// instance retrieved from Facebook after a successful authentication process.
    /// </summary>
    public static class FacebookHelper
    {
        /// <summary>
        /// Gets the Facebook user ID.
        /// </summary>
        public static string GetId([NotNull] JObject user) => user.Value<string>("id");

        /// <summary>
        /// Gets the user's name.
        /// </summary>
        public static string GetName([NotNull] JObject user) => user.Value<string>("name");

        /// <summary>
        /// Gets the user's link.
        /// </summary>
        public static string GetLink([NotNull] JObject user) => user.Value<string>("link");

        /// <summary>
        /// Gets the Facebook username.
        /// </summary>
        public static string GetUserName([NotNull] JObject user) => user.Value<string>("username");

        /// <summary>
        /// Gets the Facebook email.
        /// </summary>
        public static string GetEmail([NotNull] JObject user) => user.Value<string>("email");
    }
}
