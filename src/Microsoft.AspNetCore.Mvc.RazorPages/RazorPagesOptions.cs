// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Microsoft.AspNetCore.Mvc.RazorPages
{
    /// <summary>
    /// Provides configuration for RazorPages.
    /// </summary>
    public class RazorPagesOptions
    {
        private string _root = "/Pages";
        private string _areasRoot = "/Areas";

        /// <summary>
        /// Gets a collection of <see cref="IPageConvention"/> instances that are applied during
        /// route and page model construction.
        /// </summary>
        public PageConventionCollection Conventions { get; } = new PageConventionCollection();

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
                    throw new ArgumentException(Resources.PathMustBeRootRelativePath, nameof(value));
                }

                _root = value;
            }
        }

        /// <summary>
        /// Gets or sets a value that determines if areas are enabled for Razor Pages.
        /// Defaults to <c>true</c>.
        /// <para>
        /// When enabled, any Razor Page under the directory structure <c>/{AreaRootDirectory}/AreaName/{RootDirectory}/</c>
        /// will be associated with an area with the name <c>AreaName</c>.
        /// <seealso cref="AreaRootDirectory"/>
        /// <seealso cref="RootDirectory"/>
        /// </para>
        /// </summary>
        public bool EnableAreas { get; set; }

        /// <summary>
        /// Application relative path used as the root of discovery for Razor Page files associated with areas.
        /// Defaults to the <c>/Areas</c> directory under application root.
        /// <seealso cref="EnableAreas" />
        /// </summary>
        public string AreaRootDirectory
        {
            get => _areasRoot;
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentException(Resources.ArgumentCannotBeNullOrEmpty, nameof(value));
                }

                if (value[0] != '/')
                {
                    throw new ArgumentException(Resources.PathMustBeRootRelativePath, nameof(value));
                }

                _areasRoot = value;
            }
        }
    }
}