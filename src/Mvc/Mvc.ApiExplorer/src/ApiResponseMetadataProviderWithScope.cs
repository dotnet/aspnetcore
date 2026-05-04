// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.ApiExplorer;

internal readonly struct ApiResponseMetadataProviderWithScope(IApiResponseMetadataProvider provider, int scope)
{
    public IApiResponseMetadataProvider Provider { get; } = provider;
    public int Scope { get; } = scope;
}
