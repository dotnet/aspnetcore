// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.


namespace Microsoft.AspNet.Diagnostics
{
    /// <summary>
    /// Options for the ErrorPageMiddleware.
    /// </summary>
    public class ErrorPageOptions
    {
        /// <summary>
        /// Create an instance with the default options settings.
        /// </summary>
        public ErrorPageOptions()
        {
            SourceCodeLineCount = 6;
        }

        /// <summary>
        /// Returns a new instance of ErrorPageOptions with all visibility options enabled by default.
        /// </summary>
        public static ErrorPageOptions ShowAll => new ErrorPageOptions
                                                      {
                                                          ShowExceptionDetails = true,
                                                          ShowSourceCode = true,
                                                          ShowQuery = true,
                                                          ShowCookies = true,
                                                          ShowHeaders = true,
                                                          ShowEnvironment = true,
                                                      };

        /// <summary>
        /// Enables the display of exception types, messages, and stack traces.
        /// </summary>
        public bool ShowExceptionDetails { get; set; }

        /// <summary>
        /// Enabled the display of local source code around exception stack frames.
        /// </summary>
        public bool ShowSourceCode { get; set; }

        /// <summary>
        /// Determines how many lines of code to include before and after the line of code
        /// present in an exception's stack frame. Only applies when symbols are available and 
        /// source code referenced by the exception stack trace is present on the server.
        /// </summary>
        public int SourceCodeLineCount { get; set; }

        /// <summary>
        /// Enables the enumeration of any parsed query values.
        /// </summary>
        public bool ShowQuery { get; set; }

        /// <summary>
        /// Enables the enumeration of any parsed request cookies.
        /// </summary>
        public bool ShowCookies { get; set; }

        /// <summary>
        /// Enables the enumeration of the request headers.
        /// </summary>
        public bool ShowHeaders { get; set; }

        /// <summary>
        /// Enables the enumeration of the OWIN environment values.
        /// </summary>
        public bool ShowEnvironment { get; set; }
    }
}
