// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

#if ActivatorUtilities_In_DependencyInjection
namespace Microsoft.Extensions.DependencyInjection
#else
namespace Microsoft.Extensions.Internal
#endif
{
    /// <summary>
    /// Marks the constructor to be used when activating type using <see cref="ActivatorUtilities"/>.
    /// </summary>

#if ActivatorUtilities_In_DependencyInjection
    public
#else
    // Do not take a dependency on this class unless you are explicitly trying to avoid taking a
    // dependency on Microsoft.AspNetCore.DependencyInjection.Abstractions.
    internal
#endif
    class ActivatorUtilitiesConstructorAttribute: Attribute
    {
    }
}
