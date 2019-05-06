// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Razor.Compilation;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.AspNetCore.Mvc.Razor
{
    /// <summary>
    /// Provides programmatic configuration for the <see cref="RazorViewEngine"/>.
    /// </summary>
    public class RazorViewEngineOptions : IEnumerable<ICompatibilitySwitch>
    {
        private readonly ICompatibilitySwitch[] _switches;
        private readonly CompatibilitySwitch<bool> _allowRecompilingViewsOnFileChange;
        private Action<RoslynCompilationContext> _compilationCallback = c => { };

        public RazorViewEngineOptions()
        {
            _allowRecompilingViewsOnFileChange = new CompatibilitySwitch<bool>(nameof(AllowRecompilingViewsOnFileChange));
            _switches = new[]
            {
                _allowRecompilingViewsOnFileChange,
            };
        }

        /// <summary>
        /// Gets a <see cref="IList{IViewLocationExpander}"/> used by the <see cref="RazorViewEngine"/>.
        /// </summary>
        public IList<IViewLocationExpander> ViewLocationExpanders { get; } = new List<IViewLocationExpander>();

        /// <summary>
        /// Gets the sequence of <see cref="IFileProvider" /> instances used by <see cref="RazorViewEngine"/> to
        /// locate Razor files.
        /// </summary>
        /// <remarks>
        /// At startup, this is initialized to include an instance of
        /// <see cref="IHostingEnvironment.ContentRootFileProvider"/> that is rooted at the application root.
        /// </remarks>
        public IList<IFileProvider> FileProviders { get; } = new List<IFileProvider>();

        /// <summary>
        /// Gets the locations where <see cref="RazorViewEngine"/> will search for views.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The locations of the views returned from controllers that do not belong to an area.
        /// Locations are format strings (see https://msdn.microsoft.com/en-us/library/txafckwd.aspx) which may contain
        /// the following format items:
        /// </para>
        /// <list type="bullet">
        /// <item>
        /// <description>{0} - Action Name</description>
        /// </item>
        /// <item>
        /// <description>{1} - Controller Name</description>
        /// </item>
        /// </list>
        /// <para>
        /// The values for these locations are case-sensitive on case-sensitive file systems.
        /// For example, the view for the <c>Test</c> action of <c>HomeController</c> should be located at
        /// <c>/Views/Home/Test.cshtml</c>. Locations such as <c>/views/home/test.cshtml</c> would not be discovered.
        /// </para>
        /// </remarks>
        public IList<string> ViewLocationFormats { get; } = new List<string>();

        /// <summary>
        /// Gets the locations where <see cref="RazorViewEngine"/> will search for views within an
        /// area.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The locations of the views returned from controllers that belong to an area.
        /// Locations are format strings (see https://msdn.microsoft.com/en-us/library/txafckwd.aspx) which may contain
        /// the following format items:
        /// </para>
        /// <list type="bullet">
        /// <item>
        /// <description>{0} - Action Name</description>
        /// </item>
        /// <item>
        /// <description>{1} - Controller Name</description>
        /// </item>
        /// <item>
        /// <description>{2} - Area Name</description>
        /// </item>
        /// </list>
        /// <para>
        /// The values for these locations are case-sensitive on case-sensitive file systems.
        /// For example, the view for the <c>Test</c> action of <c>HomeController</c> under <c>Admin</c> area should
        /// be located at <c>/Areas/Admin/Views/Home/Test.cshtml</c>.
        /// Locations such as <c>/areas/admin/views/home/test.cshtml</c> would not be discovered.
        /// </para>
        /// </remarks>
        public IList<string> AreaViewLocationFormats { get; } = new List<string>();

        /// <summary>
        /// Gets the locations where <see cref="RazorViewEngine"/> will search for views (such as layouts and partials)
        /// when searched from the context of rendering a Razor Page.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Locations are format strings (see https://msdn.microsoft.com/en-us/library/txafckwd.aspx) which may contain
        /// the following format items:
        /// </para>
        /// <list type="bullet">
        /// <item>
        /// <description>{0} - View Name</description>
        /// </item>
        /// <item>
        /// <description>{1} - Page Name</description>
        /// </item>
        /// </list>
        /// <para>
        /// <see cref="PageViewLocationFormats"/> work in tandem with a view location expander to perform hierarchical
        /// path lookups. For instance, given a Page like /Account/Manage/Index using /Pages as the root, the view engine
        /// will search for views in the following locations:
        ///
        ///  /Pages/Account/Manage/{0}.cshtml
        ///  /Pages/Account/{0}.cshtml
        ///  /Pages/{0}.cshtml
        ///  /Pages/Shared/{0}.cshtml
        ///  /Views/Shared/{0}.cshtml
        /// </para>
        /// </remarks>
        public IList<string> PageViewLocationFormats { get; } = new List<string>();

        /// <summary>
        /// Gets the locations where <see cref="RazorViewEngine"/> will search for views (such as layouts and partials)
        /// when searched from the context of rendering a Razor Page within an area.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Locations are format strings (see https://msdn.microsoft.com/en-us/library/txafckwd.aspx) which may contain
        /// the following format items:
        /// </para>
        /// <list type="bullet">
        /// <item>
        /// <description>{0} - View Name</description>
        /// </item>
        /// <item>
        /// <description>{1} - Page Name</description>
        /// </item>
        /// <item>
        /// <description>{2} - Area Name</description>
        /// </item>
        /// </list>
        /// <para>
        /// <see cref="AreaPageViewLocationFormats"/> work in tandem with a view location expander to perform hierarchical
        /// path lookups. For instance, given a Page like /Areas/Account/Pages/Manage/User.cshtml using /Areas as the area pages root and
        /// /Pages as the root, the view engine will search for views in the following locations:
        ///
        ///  /Areas/Accounts/Pages/Manage/{0}.cshtml
        ///  /Areas/Accounts/Pages/{0}.cshtml
        ///  /Areas/Accounts/Pages/Shared/{0}.cshtml
        ///  /Areas/Accounts/Views/Shared/{0}.cshtml
        ///  /Pages/Shared/{0}.cshtml
        ///  /Views/Shared/{0}.cshtml
        /// </para>
        /// </remarks>
        public IList<string> AreaPageViewLocationFormats { get; } = new List<string>();

        /// <summary>
        /// Gets the <see cref="MetadataReference" /> instances that should be included in Razor compilation, along with
        /// those discovered by <see cref="MetadataReferenceFeatureProvider" />s.
        /// </summary>
        [Obsolete("This property is obsolete and will be removed in a future version. See https://aka.ms/AA1x4gg for details.")]
        public IList<MetadataReference> AdditionalCompilationReferences { get; } = new List<MetadataReference>();

        /// <summary>
        /// Gets or sets the callback that is used to customize Razor compilation
        /// to change compilation settings you can update <see cref="RoslynCompilationContext.Compilation"/> property.
        /// </summary>
        /// <remarks>
        /// Customizations made here would not reflect in tooling (Intellisense).
        /// </remarks>
        [Obsolete("This property is obsolete and will be removed in a future version. See https://aka.ms/AA1x4gg for details.")]
        public Action<RoslynCompilationContext> CompilationCallback
        {
            get => _compilationCallback;
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                _compilationCallback = value;
            }
        }

        /// <summary>
        /// Gets or sets a value that determines if Razor files (Razor Views and Razor Pages) are recompiled and updated 
        /// if files change on disk.
        /// <para>
        /// When <see langword="true"/>, MVC will use <see cref="IFileProvider.Watch(string)"/> to watch for changes to 
        /// Razor files in configured <see cref="IFileProvider"/> instances.
        /// </para>
        /// </summary>
        /// <value>
        /// The default value is <see langword="true"/> if the version is <see cref = "CompatibilityVersion.Version_2_1" />
        /// or earlier. If the version is later and <see cref= "IHostingEnvironment.EnvironmentName" /> is <c>Development</c>,
        /// the default value is <see langword="true"/>. Otherwise, the default value is <see langword="false" />.
        /// </value>
        /// <remarks>
        /// <para>
        /// This property is associated with a compatibility switch and can provide a different behavior depending on
        /// the configured compatibility version for the application. See <see cref="CompatibilityVersion"/> for
        /// guidance and examples of setting the application's compatibility version.
        /// </para>
        /// <para>
        /// Configuring the desired value of the compatibility switch by calling this property's setter will take
        /// precedence over the value implied by the application's <see cref="CompatibilityVersion"/>.
        /// </para>
        /// <para>
        /// If the application's compatibility version is set to <see cref="CompatibilityVersion.Version_2_1"/> or
        /// lower then this setting will have the value <see langword="true"/> unless explicitly configured.
        /// </para>
        /// <para>
        /// If the application's compatibility version is set to <see cref="CompatibilityVersion.Version_2_2"/> or
        /// higher then this setting will have the value <see langword="false"/> unless
        /// <see cref="IHostingEnvironment.EnvironmentName"/>  is <c>Development</c> or the value is explicitly configured.
        /// </para>
        /// </remarks>
        public bool AllowRecompilingViewsOnFileChange
        {
            // Note: When compatibility switches are removed in 3.0, this property should be retained as a regular boolean property.
            get => _allowRecompilingViewsOnFileChange.Value;
            set => _allowRecompilingViewsOnFileChange.Value = value;
        }

        IEnumerator<ICompatibilitySwitch> IEnumerable<ICompatibilitySwitch>.GetEnumerator()
        {
            return ((IEnumerable<ICompatibilitySwitch>)_switches).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => _switches.GetEnumerator();
    }
}
