// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.FileProviders;

namespace Microsoft.AspNetCore.Mvc.Razor.Internal
{
    /// <summary>
    /// Accessor to the <see cref="IFileProvider"/> used by <see cref="RazorViewEngine"/>.
    /// </summary>
    public interface IRazorViewEngineFileProviderAccessor
    {
        /// <summary>
        /// Gets the <see cref="IFileProvider"/> used to look up Razor files.
        /// </summary>
        IFileProvider FileProvider { get; }
    }
}
