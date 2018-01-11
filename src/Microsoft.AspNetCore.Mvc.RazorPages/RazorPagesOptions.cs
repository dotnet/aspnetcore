// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace Microsoft.AspNetCore.Mvc.RazorPages
{
    /// <summary>
    /// Provides configuration for RazorPages.
    /// </summary>
    public class RazorPagesOptions : IEnumerable<ICompatibilitySwitch>
    {
        private readonly CompatibilitySwitch<bool> _allowAreas;
        private readonly ICompatibilitySwitch[] _switches;

        private string _root = "/Pages";
        private string _areasRoot = "/Areas";

        public RazorPagesOptions()
        {
            _allowAreas = new CompatibilitySwitch<bool>(nameof(AllowAreas));

            _switches = new ICompatibilitySwitch[]
            {
                _allowAreas,
            };
        }

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
        /// Defaults to <c>false</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// When enabled, any Razor Page under the directory structure <c>/{AreaRootDirectory}/AreaName/{RootDirectory}/</c>
        /// will be associated with an area with the name <c>AreaName</c>.
        /// <seealso cref="AreaRootDirectory"/>
        /// <seealso cref="RootDirectory"/>
        /// </para>
        /// <para>
        /// This property is associated with a compatibility switch and can provide a different behavior depending on 
        /// the configured compatibility version for the application. See <see cref="CompatibilityVersion"/> for 
        /// guidance and examples of setting the application's compatibility version.
        /// </para>
        /// <para>
        /// Configuring the desired of the value compatibility switch by calling this property's setter will take precedence
        /// over the value implied by the application's <see cref="CompatibilityVersion"/>.
        /// </para>
        /// <para>
        /// If the application's compatibility version is set to <see cref="CompatibilityVersion.Version_2_0"/> then
        /// this setting will have value <c>false</c> unless explicitly configured.
        /// </para>
        /// <para>
        /// If the application's compatibility version is set to <see cref="CompatibilityVersion.Version_2_1"/> or
        /// higher then this setting will have value <c>true</c> unless explicitly configured.
        /// </para>
        /// </remarks>
        public bool AllowAreas
        {
            get => _allowAreas.Value;
            set => _allowAreas.Value = value;
        }

        /// <summary>
        /// Application relative path used as the root of discovery for Razor Page files associated with areas.
        /// Defaults to the <c>/Areas</c> directory under application root.
        /// <seealso cref="AllowAreas" />
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

        IEnumerator<ICompatibilitySwitch> IEnumerable<ICompatibilitySwitch>.GetEnumerator()
        {
            return ((IEnumerable<ICompatibilitySwitch>)_switches).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => _switches.GetEnumerator();
    }
}