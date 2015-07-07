// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// Provides programmatic configuration for formatter mappings in the MVC framework.
    /// </summary>
    public class MvcFormatterMappingOptions
    {
        /// <summary>
        /// Used to specify mapping between the URL Format and corresponding
        /// <see cref="Net.Http.Headers.MediaTypeHeaderValue"/>.
        /// </summary>
        public FormatterMappings FormatterMappings { get; } = new FormatterMappings();
    }
}