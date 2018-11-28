// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc.Filters;

namespace Microsoft.AspNetCore.Mvc
{
    /// <summary>
    /// An <see cref="IFilterMetadata"/> interface for <see cref="ApiControllerAttribute"/>. See 
    /// <see cref="ApiControllerAttribute"/> for details.
    /// </summary>
    internal interface IApiBehaviorMetadata : IFilterMetadata
    {
    }
}
