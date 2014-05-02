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
using System.Threading.Tasks;

namespace Microsoft.AspNet.Abstractions.Extensions
{
    /// <summary>
    /// Options for the MapWhen middleware
    /// </summary>
    public class MapWhenOptions
    {
        /// <summary>
        /// The user callback that determines if the branch should be taken
        /// </summary>
        public Func<HttpContext, bool> Predicate { get; set; }

        /// <summary>
        /// The async user callback that determines if the branch should be taken
        /// </summary>
        public Func<HttpContext, Task<bool>> PredicateAsync { get; set; }

        /// <summary>
        /// The branch taken for a positive match
        /// </summary>
        public RequestDelegate Branch { get; set; }
    }
}