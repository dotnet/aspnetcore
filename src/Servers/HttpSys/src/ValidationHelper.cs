// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;

namespace Microsoft.AspNetCore.Server.HttpSys;

internal static class ValidationHelper
{
    public static string ExceptionMessage(Exception exception)
    {
        if (exception == null)
        {
            return string.Empty;
        }
        if (exception.InnerException == null)
        {
            return exception.Message;
        }
        return exception.Message + " (" + ExceptionMessage(exception.InnerException) + ")";
    }

    public static string ToString(object objectValue)
    {
        if (objectValue == null)
        {
            return "(null)";
        }
        else if (objectValue is string s && s.Length == 0)
        {
            return "(string.empty)";
        }
        else if (objectValue is Exception ex)
        {
            return ExceptionMessage(ex);
        }
        else if (objectValue is IntPtr ptr)
        {
            return "0x" + ptr.ToString("x", CultureInfo.InvariantCulture);
        }
        else
        {
            return objectValue.ToString()!;
        }
    }

    public static string HashString(object objectValue)
    {
        if (objectValue == null)
        {
            return "(null)";
        }
        else if (objectValue is string && ((string)objectValue).Length == 0)
        {
            return "(string.empty)";
        }
        else
        {
            return objectValue.GetHashCode().ToString(NumberFormatInfo.InvariantInfo);
        }
    }
}
