// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.OutputCaching;

internal interface IOutputCachingKeyProvider
{
    /// <summary>
    /// Create a key for storing cached responses.
    /// </summary>
    /// <param name="context">The <see cref="OutputCachingContext"/>.</param>
    /// <returns>The created key.</returns>
    string CreateStorageKey(OutputCachingContext context);
}
