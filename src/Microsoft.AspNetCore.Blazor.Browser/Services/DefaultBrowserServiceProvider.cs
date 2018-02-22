// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Blazor.Browser.Services
{
    /// <summary>
    /// An implementation of <see cref="IServiceProvider"/> configured with
    /// default services suitable for use in a browser environment.
    /// </summary>
    public class DefaultBrowserServiceProvider : IServiceProvider
    {
        /// <inheritdoc />
        public object GetService(Type serviceType)
        {
            throw new NotImplementedException();
        }
    }
}
