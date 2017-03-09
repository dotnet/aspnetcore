// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Testing;

namespace Microsoft.AspNetCore.Mvc
{
    public static class PlatformNormalizer
    {
        // Mono issue - https://github.com/aspnet/External/issues/19
        public static string NormalizeContent(string input)
        {
            if (TestPlatformHelper.IsMono)
            {
                var equivalents = new Dictionary<string, string> {
                    {
                        "The [0-9a-zA-Z ]+ field is required.", "RequiredAttribute_ValidationError"
                    },
                    {
                        "'[0-9a-zA-Z ]+' and '[0-9a-zA-Z ]+' do not match.", "CompareAttribute_MustMatch"
                    },
                    {
                        "The field [0-9a-zA-Z ]+ must be a string with a minimum length of [0-9]+ and a " +
                            "maximum length of [0-9]+.",
                        "StringLengthAttribute_ValidationErrorIncludingMinimum"
                    },
                };

                var result = input;

                foreach (var kvp in equivalents)
                {
                    result = Regex.Replace(result, kvp.Key, kvp.Value);
                }

                return result;
            }

            return input;
        }
    }
}