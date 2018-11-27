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
        private readonly CompatibilitySwitch<bool> _allowMappingHeadRequestsToGetHandler;
        private readonly CompatibilitySwitch<bool> _allowsDefaultHandlingForOptionsRequests;
        private readonly ICompatibilitySwitch[] _switches;

        private string _root = "/Pages";

        public RazorPagesOptions()
        {
            _allowAreas = new CompatibilitySwitch<bool>(nameof(AllowAreas));
            _allowMappingHeadRequestsToGetHandler = new CompatibilitySwitch<bool>(nameof(AllowMappingHeadRequestsToGetHandler));
            _allowsDefaultHandlingForOptionsRequests = new CompatibilitySwitch<bool>(nameof(AllowDefaultHandlingForOptionsRequests));

            _switches = new ICompatibilitySwitch[]
            {
                _allowAreas,
                _allowMappingHeadRequestsToGetHandler,
                _allowsDefaultHandlingForOptionsRequests,
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
        /// When enabled, any Razor Page under the directory structure <c>/Area/AreaName/Pages/</c>
        /// will be associated with an area with the name <c>AreaName</c>.
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
        /// Gets or sets a value that determines if HTTP method matching for Razor Pages handler methods will use
        /// fuzzy matching.
        /// Defaults to <c>false</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// When enabled, Razor Pages handler methods will be more flexible in which HTTP methods will be accepted
        /// by GET and POST handler methods. This allows a GET handler methods to accept the HEAD HTTP methods in 
        /// addition to GET. A more specific handler method can still be defined to accept HEAD, and the most 
        /// specific handler will be invoked.
        /// </para>
        /// <para>
        /// This setting reduces the number of handler methods that must be written to correctly respond to typical
        /// web traffic including requests from internet infrastructure such as web crawlers.
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
        public bool AllowMappingHeadRequestsToGetHandler
        {
            get => _allowMappingHeadRequestsToGetHandler.Value;
            set => _allowMappingHeadRequestsToGetHandler.Value = value;
        }

        /// <summary>
        /// Gets or sets a value that determines if HTTP requests with the OPTIONS method are handled by default, if
        /// no handler is available.
        /// </summary>
        /// <value>
        /// The default value is <see langword="true"/> if the version is
        /// <see cref="CompatibilityVersion.Version_2_2"/> or later; <see langword="false"/> otherwise.
        /// </value>
        /// <remarks>
        /// <para>
        /// Razor Pages uses the current request's HTTP method to select a handler method. When no handler is available or selected,
        /// the page is immediately executed. This may cause runtime errors if the page relies on the handler method to execute
        /// and initialize some state. This setting attempts to avoid this class of error for HTTP <c>OPTIONS</c> requests by
        /// returning a <c>200 OK</c> response.
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
        /// If the application's compatibility version is set to <see cref="CompatibilityVersion.Version_2_2"/> then
        /// this setting will have value <c>true</c> unless explicitly configured.
        /// </para>
        /// <para>
        /// If the application's compatibility version is set to <see cref="CompatibilityVersion.Version_2_1"/> or
        /// lower then this setting will have value <c>true</c> unless explicitly configured.
        /// </para>
        /// </remarks>
        public bool AllowDefaultHandlingForOptionsRequests
        {
            get => _allowsDefaultHandlingForOptionsRequests.Value;
            set => _allowsDefaultHandlingForOptionsRequests.Value = value;
        }

        IEnumerator<ICompatibilitySwitch> IEnumerable<ICompatibilitySwitch>.GetEnumerator()
        {
            return ((IEnumerable<ICompatibilitySwitch>)_switches).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => _switches.GetEnumerator();
    }
}