// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc.ApiExplorer
{
    /// <summary>
    /// Provides a return type for all HTTP status codes that are not covered by other <see cref="IApiResponseMetadataProvider"/> instances.
    /// </summary>
    public interface IApiDefaultResponseMetadataProvider : IApiResponseMetadataProvider
    {
    }
}
