// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.


namespace Microsoft.AspNet.Diagnostics
{
    /// <summary>
    /// Options for the ErrorPageMiddleware
    /// </summary>
    public class ErrorPageOptions
    {
        private bool _defaultVisibility;

        private bool? _showExceptionDetails;
        private bool? _showSourceCode;
        private bool? _showQuery;
        private bool? _showCookies;
        private bool? _showHeaders;
        private bool? _showEnvironment;

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
        public static ErrorPageOptions ShowAll
        {
            get
            {
                // We don't use a static instance because it's mutable.
                return new ErrorPageOptions()
                {
                    ShowExceptionDetails = true,
                    ShowSourceCode = true,
                    ShowQuery = true,
                    ShowCookies = true,
                    ShowHeaders = true,
                    ShowEnvironment = true,
                };
            }
        }

        /// <summary>
        /// Enables the display of exception types, messages, and stack traces.
        /// </summary>
        public bool ShowExceptionDetails
        {
            get { return _showExceptionDetails ?? _defaultVisibility; }
            set { _showExceptionDetails = value; }
        }

        /// <summary>
        /// Enabled the display of local source code around exception stack frames.
        /// </summary>
        public bool ShowSourceCode
        {
            get { return _showSourceCode ?? _defaultVisibility; }
            set { _showSourceCode = value; }
        }

        /// <summary>
        /// Determines how many lines of code to include before and after the line of code
        /// present in an exception's stack frame. Only applies when symbols are available and 
        /// source code referenced by the exception stack trace is present on the server.
        /// </summary>
        public int SourceCodeLineCount { get; set; }

        /// <summary>
        /// Enables the enumeration of any parsed query values.
        /// </summary>
        public bool ShowQuery
        {
            get { return _showQuery ?? _defaultVisibility; }
            set { _showQuery = value; }
        }

        /// <summary>
        /// Enables the enumeration of any parsed request cookies.
        /// </summary>
        public bool ShowCookies
        {
            get { return _showCookies ?? _defaultVisibility; }
            set { _showCookies = value; }
        }

        /// <summary>
        /// Enables the enumeration of the request headers.
        /// </summary>
        public bool ShowHeaders
        {
            get { return _showHeaders ?? _defaultVisibility; }
            set { _showHeaders = value; }
        }

        /// <summary>
        /// Enables the enumeration of the OWIN environment values.
        /// </summary>
        public bool ShowEnvironment
        {
            get { return _showEnvironment ?? _defaultVisibility; }
            set { _showEnvironment = value; }
        }

        /// <summary>
        /// Sets the default visibility for options not otherwise specified.
        /// </summary>
        /// <param name="isVisible"></param>
        public void SetDefaultVisibility(bool isVisible)
        {
            _defaultVisibility = isVisible;
        }
    }
}
