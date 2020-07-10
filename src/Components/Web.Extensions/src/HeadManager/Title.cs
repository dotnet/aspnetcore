// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Components.Web.Extensions
{
    /// <summary>
    /// A component that changes the title of the document.
    /// </summary>
    public class Title : HeadElementBase
    {
        internal override object ElementKey => "title";

        /// <summary>
        /// Gets or sets the value to use as the document's title.
        /// </summary>
        [Parameter]
        public string Value { get; set; } = string.Empty;

        internal override async ValueTask<object> GetInitialStateAsync()
        {
            return await HeadManager.GetTitleAsync();
        }

        internal override ValueTask ResetInitialStateAsync(object initialState)
        {
            return HeadManager.SetTitleAsync(initialState);
        }

        internal override async ValueTask ApplyAsync()
        {
            await HeadManager.SetTitleAsync(Value);
        }
    }
}
