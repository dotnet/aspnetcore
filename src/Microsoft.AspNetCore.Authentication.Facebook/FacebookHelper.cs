// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNetCore.Authentication.Facebook
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
        public static string GetId(JObject user)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            return user.Value<string>("id");
        }

        /// <summary>
        /// Gets the user's min age.
        /// </summary>
        public static string GetAgeRangeMin(JObject user)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            return TryGetValue(user, "age_range", "min");
        }

        /// <summary>
        /// Gets the user's max age.
        /// </summary>
        public static string GetAgeRangeMax(JObject user)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            return TryGetValue(user, "age_range", "max");
        }

        /// <summary>
        /// Gets the user's birthday.
        /// </summary>
        public static string GetBirthday(JObject user)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            return user.Value<string>("birthday");
        }

        /// <summary>
        /// Gets the Facebook email.
        /// </summary>
        public static string GetEmail(JObject user)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            return user.Value<string>("email");
        }

        /// <summary>
        /// Gets the user's first name.
        /// </summary>
        public static string GetFirstName(JObject user)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            return user.Value<string>("first_name");
        }

        /// <summary>
        /// Gets the user's gender.
        /// </summary>
        public static string GetGender(JObject user)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            return user.Value<string>("gender");
        }

        /// <summary>
        /// Gets the user's family name.
        /// </summary>
        public static string GetLastName(JObject user)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            return user.Value<string>("last_name");
        }

        /// <summary>
        /// Gets the user's link.
        /// </summary>
        public static string GetLink(JObject user)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            return user.Value<string>("link");
        }

        /// <summary>
        /// Gets the user's location.
        /// </summary>
        public static string GetLocation(JObject user)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            return TryGetValue(user, "location", "name");
        }

        /// <summary>
        /// Gets the user's locale.
        /// </summary>
        public static string GetLocale(JObject user)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            return user.Value<string>("locale");
        }

        /// <summary>
        /// Gets the user's middle name.
        /// </summary>
        public static string GetMiddleName(JObject user)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            return user.Value<string>("middle_name");
        }

        /// <summary>
        /// Gets the user's name.
        /// </summary>
        public static string GetName(JObject user)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            return user.Value<string>("name");
        }

        /// <summary>
        /// Gets the user's timezone.
        /// </summary>
        public static string GetTimeZone(JObject user)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            return user.Value<string>("timezone");
        }

        // Get the given subProperty from a property.
        private static string TryGetValue(JObject user, string propertyName, string subProperty)
        {
            JToken value;
            if (user.TryGetValue(propertyName, out value))
            {
                var subObject = JObject.Parse(value.ToString());
                if (subObject != null && subObject.TryGetValue(subProperty, out value))
                {
                    return value.ToString();
                }
            }
            return null;
        }

    }
}
