// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// A marker class used to determine if all the MVC services were added
    /// to the <see cref="IServiceCollection"/> before MVC is configured.
    /// </summary>
    internal class MvcMarkerService
    {
    }
}
