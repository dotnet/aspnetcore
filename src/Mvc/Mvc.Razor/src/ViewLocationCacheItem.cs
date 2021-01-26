// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Mvc.Razor
{
    /// <summary>
    /// An item in <see cref="ViewLocationCacheResult"/>.
    /// </summary>
    internal readonly struct ViewLocationCacheItem
    {
        /// <summary>
        /// Initializes a new instance of <see cref="ViewLocationCacheItem"/>.
        /// </summary>
        /// <param name="razorPageFactory">The <see cref="IRazorPage"/> factory.</param>
        /// <param name="location">The application relative path of the <see cref="IRazorPage"/>.</param>
        public ViewLocationCacheItem(Func<IRazorPage> razorPageFactory, string location)
        {
            PageFactory = razorPageFactory;
            Location = location;
        }

        /// <summary>
        /// Gets the application relative path of the <see cref="IRazorPage"/>
        /// </summary>
        public string Location { get; }

        /// <summary>
        /// Gets the <see cref="IRazorPage"/> factory.
        /// </summary>
        public Func<IRazorPage> PageFactory { get; }
    }
}
