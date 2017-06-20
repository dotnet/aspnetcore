// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages.Internal;

namespace Microsoft.AspNetCore.Mvc.RazorPages
{
    /// <summary>
    /// Provides configuration for RazorPages.
    /// </summary>
    public class RazorPagesOptions
    {
        private string _root = "/Pages";

        /// <summary>
        /// Gets a list of <see cref="IPageRouteModelConvention"/> instances that will be applied to
        /// the <see cref="PageModel"/> when discovering Razor Pages.
        /// </summary>
        public IList<IPageRouteModelConvention> RouteModelConventions { get; } = new List<IPageRouteModelConvention>();

        /// <summary>
        /// Gets a list of <see cref="IPageRouteModelConvention"/> instances that will be applied to
        /// the <see cref="PageModel"/> when discovering Razor Pages.
        /// </summary>
        public IList<IPageApplicationModelConvention> ApplicationModelConventions { get; } = new List<IPageApplicationModelConvention>();

        /// <summary>
        /// Application relative path used as the root of discovery for Razor Page files.
        /// Defaults to the <c>/Pages</c> directory under application root.
        /// </summary>
        public string RootDirectory
        {
            get => _root;
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentException(Resources.ArgumentCannotBeNullOrEmpty, nameof(value));
                }

                if (value[0] != '/')
                {
                    throw new ArgumentException(Resources.PathMustBeAnAppRelativePath, nameof(value));
                }

                _root = value;
            }
        }
    }
}