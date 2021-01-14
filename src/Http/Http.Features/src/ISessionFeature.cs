// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Http.Features
{
    /// <summary>
    /// Provides access to the <see cref="ISession"/> for the current request.
    /// </summary>
    public interface ISessionFeature
    {
        /// <summary>
        /// The <see cref="ISession"/> for the current request.
        /// </summary>
        ISession Session { get; set; }
    }
}