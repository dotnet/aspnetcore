// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc.ApplicationParts
{
    /// <summary>
    /// A part of an MVC application.
    /// </summary>
    public abstract class ApplicationPart
    {
        /// <summary>
        /// Gets the <see cref="ApplicationPart"/> name.
        /// </summary>
        public abstract string Name { get; }
    }
}
