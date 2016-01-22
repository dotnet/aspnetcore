// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.PlatformAbstractions;

namespace Microsoft.AspNetCore.Mvc.Razor.Compilation
{
    /// <summary>
    /// A type that uses Roslyn to compile C# content and <see cref="DependencyContext"/> to locate references.
    /// </summary>
    public class DependencyContextCompilationService : RoslynCompilationService
    {
        private readonly DependencyContext _dependencyContext;

        /// <summary>
        /// Initalizes a new instance of the <see cref="DependencyContextCompilationService"/> class.
        /// </summary>
        /// <param name="environment">The environment for the executing application.</param>
        /// <param name="host">The <see cref="IMvcRazorHost"/> that was used to generate the code.</param>
        /// <param name="optionsAccessor">Accessor to <see cref="RazorViewEngineOptions"/>.</param>
        /// <param name="fileProviderAccessor">The <see cref="IRazorViewEngineFileProviderAccessor"/>.</param>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
        public DependencyContextCompilationService(
            IApplicationEnvironment environment,
            IMvcRazorHost host,
            IOptions<RazorViewEngineOptions> optionsAccessor,
            IRazorViewEngineFileProviderAccessor fileProviderAccessor,
            ILoggerFactory loggerFactory)
            : this(DependencyContext.Default, environment, host, optionsAccessor, fileProviderAccessor, loggerFactory)
        {
        }

        /// <summary>
        /// Initalizes a new instance of the <see cref="DependencyContextCompilationService"/> class.
        /// </summary>
        /// <param name="dependencyContext"><see cref="DependencyContext"/> to use for reference resolution.</param>
        /// <param name="environment">The environment for the executing application.</param>
        /// <param name="host">The <see cref="IMvcRazorHost"/> that was used to generate the code.</param>
        /// <param name="optionsAccessor">Accessor to <see cref="RazorViewEngineOptions"/>.</param>
        /// <param name="fileProviderAccessor">The <see cref="IRazorViewEngineFileProviderAccessor"/>.</param>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
        public DependencyContextCompilationService(
            DependencyContext dependencyContext,
            IApplicationEnvironment environment,
            IMvcRazorHost host,
            IOptions<RazorViewEngineOptions> optionsAccessor,
            IRazorViewEngineFileProviderAccessor fileProviderAccessor,
            ILoggerFactory loggerFactory)
            : base(environment, host, optionsAccessor, fileProviderAccessor, loggerFactory)
        {
            _dependencyContext = dependencyContext;
        }

        protected override List<MetadataReference> GetApplicationReferences()
        {
            return _dependencyContext.CompileLibraries
                .SelectMany(library => library.ResolveReferencePaths())
                .Select(CreateMetadataFileReference)
                .ToList();
        }
    }
}