// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Authentication
{
    /// <summary>
    /// Used to redirect authentication methods to another scheme
    /// </summary>
    public class VirtualSchemeOptions
    {
        public string Default { get; set; }

        public string Authenticate { get; set; }
        public string Challenge { get; set; }
        public string Forbid { get; set; }
        public string SignIn { get; set; }
        public string SignOut { get; set; }

        /// <summary>
        /// Used to select a default scheme to target based on the request.
        /// </summary>
        public Func<HttpContext, string> DefaultSelector { get; set; }


        /// <summary>
        /// Check that the options are valid. Should throw an exception if things are not ok.
        /// </summary>
        public virtual void Validate() { }
    }
}