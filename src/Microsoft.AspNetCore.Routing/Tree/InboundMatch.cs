// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.AspNetCore.Routing.Template;

namespace Microsoft.AspNetCore.Routing.Tree
{
    /// <summary>
    /// A candidate route to match incoming URLs in a <see cref="TreeRouter"/>.
    /// </summary>
    [DebuggerDisplay("{DebuggerToString(),nq}")]
    public class InboundMatch
    {
        /// <summary>
        /// Gets or sets the <see cref="InboundRouteEntry"/>.
        /// </summary>
        public InboundRouteEntry Entry { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="TemplateMatcher"/>.
        /// </summary>
        public TemplateMatcher TemplateMatcher { get; set; }

        private string DebuggerToString()
        {
            return TemplateMatcher?.Template?.TemplateText;
        }
    }
}
