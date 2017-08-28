// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Mvc.Razor.Compilation
{
    public class CompiledViewDescriptor
    {
        /// <summary>
        /// The normalized application relative path of the view.
        /// </summary>
        public string RelativePath { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="RazorViewAttribute"/> decorating the view.
        /// </summary>
        public RazorViewAttribute ViewAttribute { get; set; }

        /// <summary>
        /// <see cref="IChangeToken"/> instances that indicate when this result has expired.
        /// </summary>
        public IList<IChangeToken> ExpirationTokens { get; set; }

        /// <summary>
        /// Gets a value that determines if the view is precompiled.
        /// </summary>
        public bool IsPrecompiled { get; set; }
    }
}