// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNet.Mvc.TagHelpers.Internal
{
    /// <summary>
    /// Result of determining the mode an <see cref="AspNet.Razor.TagHelpers.ITagHelper"/> will run in.
    /// </summary>
    /// <typeparam name="TMode">The type representing the <see cref="AspNet.Razor.TagHelpers.ITagHelper"/>'s mode.</typeparam>
    public class ModeMatchResult<TMode>
    {
        /// <summary>
        /// Modes that were missing attributes but had at least one attribute present.
        /// </summary>
        public IList<ModeMatchAttributes<TMode>> PartialMatches { get; } = new List<ModeMatchAttributes<TMode>>();

        /// <summary>
        /// Modes that had all attributes present.
        /// </summary>
        public IList<ModeMatchAttributes<TMode>> FullMatches { get; } = new List<ModeMatchAttributes<TMode>>();

        /// <summary>
        /// Attributes that are present in at least one mode in <see cref="PartialMatches"/>, but in no modes in
        /// <see cref="FullMatches"/>.
        /// </summary>
        public IList<string> PartiallyMatchedAttributes { get; } = new List<string>();
    }
}
