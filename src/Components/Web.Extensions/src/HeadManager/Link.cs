// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Components.Web.Extensions
{
    /// <summary>
    /// A component that adds a link element to the HTML head.
    /// </summary>
    public class Link : HeadElementBase
    {
        // Link components should never override each other, so they have unique keys.
        internal override object ElementKey { get; } = new object();

        /// <summary>
        /// Gets or sets a collection of additional attributes that will be applied to the link element.
        /// </summary>
        [Parameter(CaptureUnmatchedValues = true)]
        public IReadOnlyDictionary<string, object>? Attributes { get; set; }

        internal override ValueTask ApplyAsync()
        {
            return HeadManager.SetLinkElementAsync(ElementKey.GetHashCode(), Attributes);
        }

        internal override ValueTask<object?> GetInitialStateAsync()
        {
            return ValueTask.FromResult<object?>(null);
        }

        internal override ValueTask ResetStateAsync(object? initialState)
        {
            return HeadManager.DeleteLinkElementAsync(ElementKey.GetHashCode());
        }
    }
}
