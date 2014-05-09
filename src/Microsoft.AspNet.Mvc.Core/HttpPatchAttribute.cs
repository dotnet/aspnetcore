// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNet.Mvc
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class HttpPatchAttribute : Attribute, IActionHttpMethodProvider
    {
        private static readonly IEnumerable<string> _supportedMethods = new string[] { "PATCH" };

        public IEnumerable<string> HttpMethods
        {
            get { return _supportedMethods; }
        }
    }
}