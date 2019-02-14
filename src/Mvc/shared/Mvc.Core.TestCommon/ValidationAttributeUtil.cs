// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.ComponentModel.DataAnnotations;

namespace Microsoft.AspNetCore.Mvc.ModelBinding
{
    public static class ValidationAttributeUtil
    {
        public static string GetRequiredErrorMessage(string field)
        {
            var attr = new RequiredAttribute();
            return attr.FormatErrorMessage(field);
        }

        public static string GetStringLengthErrorMessage(int? minimumLength, int maximumLength, string field)
        {
            var attr = new StringLengthAttribute(maximumLength);
            if (minimumLength != null)
            {
                attr.MinimumLength = (int)minimumLength;
            }

            return attr.FormatErrorMessage(field);
        }

        public static string GetRegExErrorMessage(string pattern, string field)
        {
            var attr = new RegularExpressionAttribute(pattern);
            return attr.FormatErrorMessage(field);
        }

        public static string GetRangeErrorMessage(int min, int max, string field)
        {
            var attr = new RangeAttribute(min, max);
            return attr.FormatErrorMessage(field);
        }
    }
}