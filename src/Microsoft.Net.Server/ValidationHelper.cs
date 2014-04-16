// -----------------------------------------------------------------------
// <copyright file="ValidationHelper.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Globalization;

namespace Microsoft.Net.Server
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
            else if (objectValue is string && ((string)objectValue).Length == 0)
            {
                return "(string.empty)";
            }
            else if (objectValue is Exception)
            {
                return ExceptionMessage(objectValue as Exception);
            }
            else if (objectValue is IntPtr)
            {
                return "0x" + ((IntPtr)objectValue).ToString("x");
            }
            else
            {
                return objectValue.ToString();
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
