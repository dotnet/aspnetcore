// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.


using System.Collections.Generic;
using Microsoft.AspNet.Http;
using Microsoft.Extensions.Primitives;

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
        /// Detailed information about each exception in the stack.
        /// </summary>
        public IEnumerable<ErrorDetails> ErrorDetails { get; set; }

        /// <summary>
        /// Parsed query data.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "Model class contains collection")]
        public IQueryCollection Query { get; set; }
        
        /// <summary>
        /// Request cookies.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "Model class contains collection")]
        public IRequestCookieCollection Cookies { get; set; }
        
        /// <summary>
        /// Request headers.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "Model class contains collection")]
        public IDictionary<string, StringValues> Headers { get; set; }
    }
}
