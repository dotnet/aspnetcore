// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Components.Routing
{
    /// <summary>
    /// Contract to setup navigation interception on the client.
    /// </summary>
    public interface INavigationInterception
    {
        /// <summary>
        /// Enables navigation interception on the client.
        /// </summary>
        /// <returns>A <see cref="Task" /> that represents the asynchronous operation.</returns>
        Task EnableNavigationInterceptionAsync();
    }
}
