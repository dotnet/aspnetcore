// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Components.Web.Extensions.Head
{
    /// <summary>
    /// A component that adds a link tag to the HTML head.
    /// </summary>
    public sealed class Link : HeadTagBase
    {
        /// <inheritdoc />
        protected override string TagName => "link";
    }
}
