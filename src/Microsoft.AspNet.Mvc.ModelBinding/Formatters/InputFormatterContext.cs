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
using System.Text;
using Microsoft.AspNet.Http;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class InputFormatterContext
    {
        public InputFormatterContext([NotNull] HttpContext httpContext,
                                     [NotNull] ModelMetadata metadata, 
                                     [NotNull] ModelStateDictionary modelState)
        {
            HttpContext = httpContext;
            Metadata = metadata;
            ModelState = modelState;
        }

        public HttpContext HttpContext { get; private set; }

        public ModelMetadata Metadata { get; private set; }

        public ModelStateDictionary ModelState { get; private set; }

        public object Model { get; set; }
    }
}
