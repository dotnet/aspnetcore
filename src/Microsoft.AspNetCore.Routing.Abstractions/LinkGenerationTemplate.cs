// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Routing
{
    /// <summary>
    /// Defines a contract to generate a URL from a template.
    /// </summary>
    public abstract class LinkGenerationTemplate
    {
        /// <summary>
        /// Generates a URL with an absolute path from the specified route values.
        /// </summary>
        /// <param name="values">An object that contains route values.</param>
        /// <returns>The generated URL.</returns>
        public string MakeUrl(object values)
        {
            return MakeUrl(values, options: null);
        }

        /// <summary>
        /// Generates a URL with an absolute path from the specified route values and link options.
        /// </summary>
        /// <param name="values">An object that contains route values.</param>
        /// <param name="options">The <see cref="LinkOptions"/>.</param>
        /// <returns>The generated URL.</returns>
        public abstract string MakeUrl(object values, LinkOptions options);
    }
}