// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;

namespace Microsoft.AspNetCore.Components.Server
{
    /// <summary>
    /// Represents the result of a prerendering an <see cref="IComponent"/>.
    /// </summary>
    public sealed class ComponentPrerenderResult
    {
        private readonly IEnumerable<string> _result;

        internal ComponentPrerenderResult(IEnumerable<string> result)
        {
            _result = result;
        }

        /// <summary>
        /// Writes the prerendering result to the given <paramref name="writer"/>.
        /// </summary>
        /// <param name="writer">The <see cref="TextWriter"/> the results will be written to.</param>
        public void WriteTo(TextWriter writer)
        {
            foreach (var element in _result)
            {
                writer.Write(element);
            }
        }
    }
}
