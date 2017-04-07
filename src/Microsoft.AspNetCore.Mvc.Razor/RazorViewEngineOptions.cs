// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Razor.Compilation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.AspNetCore.Mvc.Razor
{
    /// <summary>
    /// Provides programmatic configuration for the <see cref="RazorViewEngine"/>.
    /// </summary>
    public class RazorViewEngineOptions
    {
        private CSharpParseOptions _parseOptions = new CSharpParseOptions(LanguageVersion.CSharp6);
        private CSharpCompilationOptions _compilationOptions =
            new CSharpCompilationOptions(CodeAnalysis.OutputKind.DynamicallyLinkedLibrary);
        private Action<RoslynCompilationContext> _compilationCallback = c => { };

        /// <summary>
        /// Gets a <see cref="IList{IViewLocationExpander}"/> used by the <see cref="RazorViewEngine"/>.
        /// </summary>
        public IList<IViewLocationExpander> ViewLocationExpanders { get; }
            = new List<IViewLocationExpander>();

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

        public IList<string> PageViewLocationFormats { get; } = new List<string>();

        /// <summary>
        /// Gets the <see cref="MetadataReference" /> instances that should be included in Razor compilation, along with
        /// those discovered by <see cref="MetadataReferenceFeatureProvider" />s.
        /// </summary>
        public IList<MetadataReference> AdditionalCompilationReferences { get; } = new List<MetadataReference>();

        /// <summary>
        /// Gets or sets the callback that is used to customize Razor compilation
        /// to change compilation settings you can update <see cref="RoslynCompilationContext.Compilation"/> property.
        /// </summary>
        /// <remarks>
        /// Customizations made here would not reflect in tooling (Intellisense).
        /// </remarks>
        public Action<RoslynCompilationContext> CompilationCallback
        {
            get { return _compilationCallback; }
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
        /// Gets or sets the <see cref="CSharpParseOptions"/> options used by Razor view compilation.
        /// </summary>
        public CSharpParseOptions ParseOptions
        {
            get { return _parseOptions; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                _parseOptions = value;
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="CSharpCompilationOptions"/> used by Razor view compilation.
        /// </summary>
        public CSharpCompilationOptions CompilationOptions
        {
            get { return _compilationOptions; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                _compilationOptions = value;
            }
        }
    }
}
