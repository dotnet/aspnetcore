// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.AspNet.FileProviders;
using Microsoft.AspNet.Mvc.Razor.Precompilation;
using Microsoft.Extensions.PlatformAbstractions;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNet.Mvc.Razor.Compilation
{
    /// <summary>
    /// An <see cref="ICompilerCacheProvider"/> that provides a <see cref="CompilerCache"/> instance
    /// populated with precompiled views.
    /// </summary>
    public class PrecompiledViewsCompilerCacheProvider : ICompilerCacheProvider
    {
        private static readonly Assembly RazorAssembly = typeof(DefaultCompilerCacheProvider).GetTypeInfo().Assembly;
        private static readonly Type RazorFileInfoCollectionType = typeof(RazorFileInfoCollection);
        private readonly Func<ICompilerCache> _createCache;
        private readonly IAssemblyLoadContextAccessor _loadContextAccessor;
        private readonly IFileProvider _fileProvider;
        private readonly Assembly[] _assemblies;

        private object _cacheLock = new object();
        private bool _cacheInitialized;
        private ICompilerCache _compilerCache;

        /// <summary>
        /// Initializes a new instance of <see cref="DefaultCompilerCacheProvider"/>.
        /// </summary>
        /// <param name="loadContextAccessor">The <see cref="IAssemblyLoadContextAccessor"/>.</param>
        /// <param name="fileProviderAccessor">The <see cref="IRazorViewEngineFileProviderAccessor"/>.</param>
        /// <param name="assemblies"><see cref="Assembly"/> instances to scan for precompiled views.</param>
        public PrecompiledViewsCompilerCacheProvider(
            IAssemblyLoadContextAccessor loadContextAccessor,
            IRazorViewEngineFileProviderAccessor fileProviderAccessor,
            IEnumerable<Assembly> assemblies)
        {
            _loadContextAccessor = loadContextAccessor;
            _fileProvider = fileProviderAccessor.FileProvider;
            _createCache = CreateCache;
            _assemblies = assemblies.ToArray();
        }

        /// <inheritdoc />
        public ICompilerCache Cache
        {
            get
            {
                return LazyInitializer.EnsureInitialized(
                    ref _compilerCache,
                    ref _cacheInitialized,
                    ref _cacheLock,
                    _createCache);
            }
        }

        private ICompilerCache CreateCache()
        {
            var razorFileInfoCollections = GetFileInfoCollections(_assemblies);
            var loadContext = _loadContextAccessor.GetLoadContext(RazorAssembly);
            var precompiledViews = GetPrecompiledViews(razorFileInfoCollections, loadContext);

            return new CompilerCache(_fileProvider, precompiledViews);
        }

        // Internal for unit testing
        internal static Dictionary<string, Type> GetPrecompiledViews(
            IEnumerable<RazorFileInfoCollection> razorFileInfoCollections, 
            IAssemblyLoadContext loadContext)
        {
            var precompiledViews = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);

            foreach (var viewCollection in razorFileInfoCollections)
            {
                var containingAssembly = viewCollection.LoadAssembly(loadContext);
                foreach (var fileInfo in viewCollection.FileInfos)
                {
                    var viewType = containingAssembly.GetType(fileInfo.FullTypeName);
                    precompiledViews[fileInfo.RelativePath] = viewType;
                }
            }

            return precompiledViews;
        }

        private static IEnumerable<RazorFileInfoCollection> GetFileInfoCollections(IEnumerable<Assembly> assemblies)
        {
            return assemblies
                .SelectMany(assembly => assembly.ExportedTypes)
                .Where(IsValidRazorFileInfoCollection)
                .Select(Activator.CreateInstance)
                .Cast<RazorFileInfoCollection>();
        }

        internal static bool IsValidRazorFileInfoCollection(Type type)
        {
            return
                RazorFileInfoCollectionType.IsAssignableFrom(type) &&
                !type.GetTypeInfo().IsAbstract &&
                !type.GetTypeInfo().ContainsGenericParameters;
        }
    }
}