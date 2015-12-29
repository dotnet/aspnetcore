// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.FileProviders;
using Microsoft.AspNet.Mvc.Razor.Compilation;
using Microsoft.CodeAnalysis.CSharp;

namespace Microsoft.AspNet.Mvc.Razor
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
        /// Get a <see cref="IList{IViewLocationExpander}"/> used by the <see cref="RazorViewEngine"/>.
        /// </summary>
        public IList<IViewLocationExpander> ViewLocationExpanders { get; }
            = new List<IViewLocationExpander>();

        /// <summary>
        /// Gets the sequence of <see cref="IFileProvider" /> instances used by <see cref="RazorViewEngine"/> to
        /// locate Razor files.
        /// </summary>
        /// <remarks>
        /// At startup, this is initialized to include an instance of <see cref="PhysicalFileProvider"/> that is
        /// rooted at the application root.
        /// </remarks>
        public IList<IFileProvider> FileProviders { get; } = new List<IFileProvider>();

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
