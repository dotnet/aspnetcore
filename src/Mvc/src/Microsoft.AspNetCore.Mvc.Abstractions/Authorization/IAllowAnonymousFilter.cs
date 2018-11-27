// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc.Filters;

namespace Microsoft.AspNetCore.Mvc.Authorization
{
    /// <summary>
    /// A filter that allows anonymous requests, disabling some <see cref="IAuthorizationFilter"/>s.
    /// </summary>
    public interface IAllowAnonymousFilter : IFilterMetadata
    {
    }
}
