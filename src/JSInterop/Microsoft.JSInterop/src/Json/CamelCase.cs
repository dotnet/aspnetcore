// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.JSInterop
{
    internal static class CamelCase
    {
        public static string MemberNameToCamelCase(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentException(
                    $"The value '{value ?? "null"}' is not a valid member name.",
                    nameof(value));
            }

            // If we don't need to modify the value, bail out without creating a char array
            if (!char.IsUpper(value[0]))
            {
                return value;
            }

            // We have to modify at least one character
            var chars = value.ToCharArray();

            var length = chars.Length;
            if (length < 2 || !char.IsUpper(chars[1]))
            {
                // Only the first character needs to be modified
                // Note that this branch is functionally necessary, because the 'else' branch below
                // never looks at char[1]. It's always looking at the n+2 character.
                chars[0] = char.ToLowerInvariant(chars[0]);
            }
            else
            {
                // If chars[0] and chars[1] are both upper, then we'll lowercase the first char plus
                // any consecutive uppercase ones, stopping if we find any char that is followed by a
                // non-uppercase one
                var i = 0;
                while (i < length)
                {
                    chars[i] = char.ToLowerInvariant(chars[i]);

                    i++;

                    // If the next-plus-one char isn't also uppercase, then we're now on the last uppercase, so stop
                    if (i < length - 1 && !char.IsUpper(chars[i + 1]))
                    {
                        break;
                    }
                }
            }

            return new string(chars);
        }
    }
}
