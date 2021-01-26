// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;

namespace Microsoft.AspNetCore.Server.HttpSys
{
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
}
