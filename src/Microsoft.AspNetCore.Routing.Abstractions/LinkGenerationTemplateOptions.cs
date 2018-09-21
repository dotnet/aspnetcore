// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Routing
{
    /// <summary>
    /// Contains options for creating a <see cref="LinkGenerationTemplate" />.
    /// </summary>
    public class LinkGenerationTemplateOptions
    {
        /// <summary>
        /// Gets or sets a value indicating whether the template will use route values from the current request
        /// when generating a URI.
        /// </summary>
        public bool UseAmbientValues { get; set; }
    }
}
