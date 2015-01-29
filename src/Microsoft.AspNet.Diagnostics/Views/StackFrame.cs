// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.


using System.Collections.Generic;

namespace Microsoft.AspNet.Diagnostics.Views
{
    /// <summary>
    /// Detailed exception stack information used to generate a view
    /// </summary>
    public class StackFrame
    {
        /// <summary>
        /// Function containing instruction
        /// </summary>
        public string Function { get; set; }

        /// <summary>
        /// File containing the instruction
        /// </summary>
        public string File { get; set; }

        /// <summary>
        /// The line number of the instruction
        /// </summary>
        public int Line { get; set; }

        /// <summary>
        /// The line preceeding the frame line
        /// </summary>
        public int PreContextLine { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public IEnumerable<string> PreContextCode { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public IEnumerable<string> ContextCode { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public IEnumerable<string> PostContextCode { get; set; }

        /// <summary>
        /// Specific error details for this stack frame.
        /// </summary>
        public string ErrorDetails { get; set; }
    }
}
