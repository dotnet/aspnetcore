// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Components
{
    /// <summary>
    /// Supplies information about an error event that is being raised.
    /// </summary>
    public class UIErrorEventArgs : UIEventArgs
    {
        /// <summary>
        /// Gets a a human-readable error message describing the problem.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Gets the name of the script file in which the error occurred.
        /// </summary>
        public string Filename { get; set; }

        /// <summary>
        /// Gets the line number of the script file on which the error occurred.
        /// </summary>
        public int Lineno { get; set; }

        /// <summary>
        /// Gets the column number of the script file on which the error occurred.
        /// </summary>
        public int Colno { get; set; }
    }
}
