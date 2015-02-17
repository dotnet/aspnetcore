// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNet.Mvc.TagHelpers.Internal
{
    /// <summary>
    /// Static creation methods for <see cref="ModeMatchAttributes{TMode}"/>.
    /// </summary>
    public static class ModeMatchAttributes
    {
        /// <summary>
        /// Creates an <see cref="ModeMatchAttributes{TMode}"/>.
        /// </summary>
        public static ModeMatchAttributes<TMode> Create<TMode>(
           TMode mode,
           IEnumerable<string> presentAttributes)
        {
            return Create(mode, presentAttributes, missingAttributes: null);
        }

        /// <summary>
        /// Creates an <see cref="ModeMatchAttributes{TMode}"/>.
        /// </summary>
        public static ModeMatchAttributes<TMode> Create<TMode>(
            TMode mode,
            IEnumerable<string> presentAttributes,
            IEnumerable<string> missingAttributes)
        {
            return new ModeMatchAttributes<TMode>
            {
                Mode = mode,
                PresentAttributes = presentAttributes,
                MissingAttributes = missingAttributes
            };
        }
    }
}