// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Components.Routing
{
    /// <summary>
    /// Interface implemented by objects that want to react to a Location change in <see cref="NavigationManager"/>
    /// </summary>
    public interface IHandleLocationChanging
    {
        /// <summary>
        /// This function is called whenever the <see cref="NavigationManager"/> wants to change it's Location
        /// </summary>
        /// <param name="context"></param>
        /// <returns>A <see cref="ValueTask"/> whose result is a boolean, which if true will cancel the current Location change</returns>
        ValueTask<bool> OnLocationChanging(LocationChangingContext context);
    }
}
