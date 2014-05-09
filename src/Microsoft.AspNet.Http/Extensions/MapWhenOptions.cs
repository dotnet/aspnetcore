// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;

namespace Microsoft.AspNet.Builder.Extensions
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