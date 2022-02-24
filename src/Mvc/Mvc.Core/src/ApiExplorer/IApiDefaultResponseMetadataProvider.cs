// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.ApiExplorer;

/// <summary>
/// Provides a return type for all HTTP status codes that are not covered by other <see cref="IApiResponseMetadataProvider"/> instances.
/// </summary>
public interface IApiDefaultResponseMetadataProvider : IApiResponseMetadataProvider
{
}
