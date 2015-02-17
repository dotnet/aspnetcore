// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNet.Mvc.TagHelpers.Internal
{
    /// <summary>
    /// Static creation methods for <see cref="ModeAttributes{TMode}"/>.
    /// </summary>
    public static class ModeAttributes
    {
        /// <summary>
        /// Creates an <see cref="ModeAttributes{TMode}"/>/
        /// </summary>
        public static ModeAttributes<TMode> Create<TMode>(TMode mode, IEnumerable<string> attributes)
        {
            return new ModeAttributes<TMode>
            {
                Mode = mode,
                Attributes = attributes
            };
        }
    }
}