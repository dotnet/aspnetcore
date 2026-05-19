// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;

namespace Microsoft.AspNetCore.Mvc.ModelBinding;

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
