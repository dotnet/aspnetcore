// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.


using System;
using System.Collections.Generic;
using Microsoft.AspNet.Http;

namespace Microsoft.AspNet.Diagnostics.Views
{
    /// <summary>
    /// Holds data to be displayed on the error page.
    /// </summary>
    public class ErrorPageModel
    {
        /// <summary>
        /// Options for what output to display.
        /// </summary>
        public ErrorPageOptions Options { get; set; }

        /// <summary>
        /// Detailed information about each exception in the stack
        /// </summary>
        public IEnumerable<ErrorDetails> ErrorDetails { get; set; }

        /// <summary>
        /// Parsed query data
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "Model class contains collection")]
        public IReadableStringCollection Query { get; set; }

        /* TODO:
        /// <summary>
        /// Request cookies
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "Model class contains collection")]
        public RequestCookieCollection Cookies { get; set; }
        */
        /// <summary>
        /// Request headers
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "Model class contains collection")]
        public IDictionary<string, string[]> Headers { get; set; }
        /* TODO:
        /// <summary>
        /// The request environment
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "Model class contains collection")]
        public HttpContext Environment { get; set; }
        */
    }
}
