// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text.RegularExpressions;

namespace Common
{
    public static class ErrorParsing
    {
        /// <summary>
        /// Trim the full value of an error/exception message down to just the message.
        /// </summary>
        /// <param name="fullErrorMsg">The complete error message</param>
        /// <returns>The message of the error.</returns>
        public static string GetExceptionMessage(string fullErrorMsg)
        {
            if (string.IsNullOrEmpty(fullErrorMsg))
            {
                throw new ArgumentException("String cannot be null or empty.", nameof(fullErrorMsg));
            }

            // Don't include the stacktrace, it's likely to be different between runs.
            var parts = fullErrorMsg.Split(new string[] { "   at " }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length <= 0)
            {
                throw new ArgumentException("The stacktrace was not included in the exception message.");
            }
            var exceptionMessage = parts[0];
            exceptionMessage = exceptionMessage.Trim();

            // De-uniquify the port
            return Regex.Replace(exceptionMessage, @"127.0.0.1(:\d*)?", "127.0.0.1").Trim();
        }
    }
}
