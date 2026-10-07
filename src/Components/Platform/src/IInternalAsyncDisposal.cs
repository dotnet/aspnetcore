// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Platform;

// Implement explicitly. A public DisposeAsync would let application code dispose an instance that Window shares.
internal interface IInternalAsyncDisposal
{
    ValueTask InternalDisposeAsync();
}
