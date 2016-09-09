// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Mvc.ApplicationParts
{
    /// <summary>
    /// Provides information for precompiled views.
    /// </summary>
    public class ViewInfo
    {
        /// <summary>
        /// Creates a new instance of <see cref="ViewInfo" />.
        /// </summary>
        /// <param name="path">The path of the view.</param>
        /// <param name="type">The view <see cref="System.Type"/>.</param>
        public ViewInfo(string path, Type type)
        {
            Path = path;
            Type = type;
        }

        /// <summary>
        /// The path of the view.
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// The view <see cref="System.Type"/>.
        /// </summary>
        public Type Type { get; }
    }
}
