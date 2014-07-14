// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using Microsoft.AspNet.FileSystems;
using Microsoft.AspNet.Razor;
using Microsoft.Framework.Runtime;

namespace Microsoft.AspNet.Mvc.Razor
{
    public class RazorCompilationService : IRazorCompilationService
    {
        // This class must be registered as a singleton service for the caching to work.
        private readonly CompilerCache _cache = new CompilerCache();
        private readonly IApplicationEnvironment _environment;
        private readonly ICompilationService _baseCompilationService;
        private readonly IMvcRazorHost _razorHost;
        private readonly string _appRoot;

        public RazorCompilationService(IApplicationEnvironment environment,
                                       ICompilationService compilationService, 
                                       IMvcRazorHost razorHost)
        {
            _environment = environment;
            _baseCompilationService = compilationService;
            _razorHost = razorHost;
            _appRoot = EnsureTrailingSlash(environment.ApplicationBasePath);
        }

        public CompilationResult Compile([NotNull] IFileInfo file)
        {
            return _cache.GetOrAdd(file, () => CompileCore(file));
        }

        // TODO: Make this internal
        public CompilationResult CompileCore(IFileInfo file)
        {
            GeneratorResults results;
            using (var inputStream = file.CreateReadStream())
            {
                Contract.Assert(file.PhysicalPath.StartsWith(_appRoot, StringComparison.OrdinalIgnoreCase));
                var rootRelativePath = file.PhysicalPath.Substring(_appRoot.Length);
                results = _razorHost.GenerateCode(rootRelativePath, inputStream);
            }

            if (!results.Success)
            {
                var messages = results.ParserErrors.Select(e => new CompilationMessage(e.Message));
                throw new CompilationFailedException(messages, results.GeneratedCode);
            }

            return _baseCompilationService.Compile(results.GeneratedCode);
        }

        private static string EnsureTrailingSlash([NotNull]string path)
        {
            if (!path.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                path += Path.DirectorySeparatorChar;
            }
            return path;
        }
    }
}
