// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Mvc.Localization
{
    /// <summary>
    /// A factory that creates <see cref="IHtmlLocalizer"/> instances.
    /// </summary>
    public interface IHtmlLocalizerFactory
    {
        /// <summary>
        /// Creates an <see cref="IHtmlLocalizer"/> using the <see cref="System.Reflection.Assembly"/> and
        /// <see cref="Type.FullName"/> of the specified <see cref="Type"/>.
        /// </summary>
        /// <param name="resourceSource">The <see cref="Type"/>.</param>
        /// <returns>The <see cref="IHtmlLocalizer"/>.</returns>
        IHtmlLocalizer Create(Type resourceSource);

        /// <summary>
        /// Creates an <see cref="IHtmlLocalizer"/>.
        /// </summary>
        /// <param name="baseName">The base name of the resource to load strings from.</param>
        /// <param name="location">The location to load resources from.</param>
        /// <returns>The <see cref="IHtmlLocalizer"/>.</returns>
        IHtmlLocalizer Create(string baseName, string location);
    }
}