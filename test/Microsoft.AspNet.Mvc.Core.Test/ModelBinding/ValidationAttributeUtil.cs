// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.ComponentModel.DataAnnotations;

namespace Microsoft.AspNet.Mvc.ModelBinding
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

        public static string GetMaxLengthErrorMessage(int maximumLength, string field)
        {
            var attr = new MaxLengthAttribute(maximumLength);
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