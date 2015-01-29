// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// Represents information used by a formatter for participating in
    /// output content negotiation and in writing out the response.
    /// </summary>
    public class OutputFormatterContext
    {
        /// <summary>
        /// The return value of the action method.
        /// </summary>
        public object Object { get; set; }

        /// <summary>
        /// The declared return type of the action.
        /// </summary>
        public Type DeclaredType { get; set; }

        /// <summary>
        /// Action context associated with the current call.
        /// </summary>
        public ActionContext ActionContext { get; set; }

        /// <summary>
        /// The encoding which is chosen by the selected formatter.
        /// </summary>
        public Encoding SelectedEncoding { get; set; }

        /// <summary>
        /// The content type which is chosen by the selected formatter.
        /// </summary>
        public MediaTypeHeaderValue SelectedContentType { get; set; }
    }
}
