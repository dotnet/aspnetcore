// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// Specifies what HTTP methods an action supports.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class AcceptVerbsAttribute : Attribute, IActionHttpMethodProvider
    {
        private readonly IEnumerable<string> _httpMethods;

        /// <summary>
        /// Initializes a new instance of the <see cref="AcceptVerbsAttribute" /> class.
        /// </summary>
        /// <param name="method">The HTTP method the action supports.</param>
        public AcceptVerbsAttribute([NotNull] string method)
            : this(new string[] { method })
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AcceptVerbsAttribute" /> class.
        /// </summary>
        /// <param name="methods">The HTTP methods the action supports.</param>
        public AcceptVerbsAttribute(params string[] methods)
        {
            // TODO: This assumes that the methods are exactly same as standard Http Methods.
            // The Http Abstractions should take care of these.
            _httpMethods = methods.Select(method => method.ToUpperInvariant());
        }
      
        /// <summary>
        /// Gets the HTTP methods the action supports.
        /// </summary>
        public IEnumerable<string> HttpMethods
        {
            get
            {
                return _httpMethods;
            }
        }
    }
}