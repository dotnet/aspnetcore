// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.FileProviders;
using Microsoft.AspNet.Mvc.Razor.Compilation;

namespace Microsoft.AspNet.Mvc.Razor
{
    /// <summary>
    /// Provides programmatic configuration for the <see cref="RazorViewEngine"/>.
    /// </summary>
    public class RazorViewEngineOptions
    {
        private IFileProvider _fileProvider;

        private string _configuration;

        private Action<RoslynCompilationContext> _compilationCallback = c => { };

        /// <summary>
        /// Get a <see cref="IList{IViewLocationExpander}"/> used by the <see cref="RazorViewEngine"/>.
        /// </summary>
        public IList<IViewLocationExpander> ViewLocationExpanders { get; }
            = new List<IViewLocationExpander>();

        /// <summary>
        /// Gets or sets the <see cref="IFileProvider" /> used by <see cref="RazorViewEngine"/> to locate Razor files on
        /// disk.
        /// </summary>
        /// <remarks>
        /// At startup, this is initialized to an instance of <see cref="PhysicalFileProvider"/> that is rooted at the
        /// application root.
        /// </remarks>
        public IFileProvider FileProvider
        {
            get { return _fileProvider; }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                _fileProvider = value;
            }
        }

        /// <summary>
        /// Gets or sets the configuration name used by <see cref="RazorViewEngine"/> to compile razor views.
        /// </summary>
        /// <remarks>
        /// At startup, this is initialized to "Debug" if <see cref="Microsoft.AspNet.Hosting.IHostingEnvironment"/> service is
        /// registred and environment is development (<see cref="Microsoft.AspNet.Hosting.HostingEnvironmentExtensions.IsDevelopment"/>) else it is set to
        /// "Release".
        /// </remarks>
        public string Configuration
        {
            get { return _configuration; }
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentException(Resources.ArgumentCannotBeNullOrEmpty, nameof(value));
                }
                _configuration = value;
            }
        }

        /// <summary>
        /// Gets or sets the callback that is used to customize Razor compilation
        /// to change compilation settings you can update <see cref="RoslynCompilationContext.Compilation"/> property.
        /// Customizations made here would not reflect in tooling (Intellisense).
        /// </summary>
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
    }
}
