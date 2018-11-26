// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Builder.Extensions
{
    /// <summary>
    /// Options for the <see cref="MapWhenMiddleware"/>.
    /// </summary>
    public class MapWhenOptions
    {
        private Func<HttpContext, bool> _predicate;

        /// <summary>
        /// The user callback that determines if the branch should be taken.
        /// </summary>
        public Func<HttpContext, bool> Predicate
        {
            get
            {
                return _predicate;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                _predicate = value;
            }
        }

        /// <summary>
        /// The branch taken for a positive match.
        /// </summary>
        public RequestDelegate Branch { get; set; }
    }
}