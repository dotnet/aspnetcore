// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using Microsoft.AspNetCore.Rewrite.UrlActions;

namespace Microsoft.AspNetCore.Rewrite.ApacheModRewrite
{
    internal class CookieActionFactory
    {
        /// <summary>
        ///  Creates a <see cref="ChangeCookieAction" /> <see href="https://httpd.apache.org/docs/current/rewrite/flags.html#flag_co" /> for details.
        /// </summary>
        /// <param name="flagValue">The flag</param>
        /// <returns>The action</returns>
        public ChangeCookieAction Create(string flagValue)
        {
            if (string.IsNullOrEmpty(flagValue))
            {
                throw new ArgumentException(nameof(flagValue));
            }

            var i = 0;
            var separator = ':';
            if (flagValue[0] == ';')
            {
                separator = ';';
                i++;
            }

            ChangeCookieAction action = null;
            var currentField = Fields.Name;
            var start = i;
            for (; i < flagValue.Length; i++)
            {
                if (flagValue[i] == separator)
                {
                    var length = i - start;
                    SetActionOption(flagValue.Substring(start, length).Trim(), currentField, ref action);

                    currentField++;
                    start = i + 1;
                }
            }

            if (i != start)
            {
                SetActionOption(flagValue.Substring(start).Trim(new[] { ' ', separator }), currentField, ref action);
            }

            if (currentField < Fields.Domain)
            {
                throw new FormatException(Resources.FormatError_InvalidChangeCookieFlag(flagValue));
            }

            return action;
        }

        private static void SetActionOption(string value, Fields tokenType, ref ChangeCookieAction action)
        {
            switch (tokenType)
            {
                case Fields.Name:
                    action = new ChangeCookieAction(value);
                    break;
                case Fields.Value:
                    action.Value = value;
                    break;
                case Fields.Domain:
                    // despite what spec says, an empty domain field is allowed in mod_rewrite
                    // by specifying NAME:VALUE:;
                    action.Domain = string.IsNullOrEmpty(value) || value == ";"
                        ? null
                        : value;
                    break;
                case Fields.Lifetime:
                    if (string.IsNullOrEmpty(value))
                    {
                        break;
                    }

                    uint minutes;
                    if (!uint.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out minutes))
                    {
                        throw new FormatException(Resources.FormatError_CouldNotParseInteger(value));
                    }

                    action.Lifetime = TimeSpan.FromMinutes(minutes);
                    break;
                case Fields.Path:
                    action.Path = value;
                    break;
                case Fields.Secure:
                    action.Secure = "secure".Equals(value, StringComparison.OrdinalIgnoreCase)
                        || "true".Equals(value, StringComparison.OrdinalIgnoreCase)
                        || value == "1";
                    break;
                case Fields.HttpOnly:
                    action.HttpOnly = "httponly".Equals(value, StringComparison.OrdinalIgnoreCase)
                        || "true".Equals(value, StringComparison.OrdinalIgnoreCase)
                        || value == "1";
                    break;
            }
        }

        // order matters
        // see https://httpd.apache.org/docs/current/rewrite/flags.html#flag_co
        private enum Fields
        {
            Name,
            Value,
            Domain,
            Lifetime,
            Path,
            Secure,
            HttpOnly
        }
    }
}