// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using System;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using Microsoft.AspNet.FileSystems;
using Microsoft.AspNet.Razor;
using Microsoft.Net.Runtime;

namespace Microsoft.AspNet.Mvc.Razor
{
    public class RazorCompilationService : IRazorCompilationService
    {
        private static readonly CompilerCache _cache = new CompilerCache();
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

        public CompilationResult Compile([NotNull]IFileInfo file)
        {
            return _cache.GetOrAdd(file, () => CompileCore(file));
        }

        // TODO: Make this internal
        public CompilationResult CompileCore(IFileInfo file)
        {
            GeneratorResults results;
            using (Stream inputStream = file.CreateReadStream())
            {
                Contract.Assert(file.PhysicalPath.StartsWith(_appRoot, StringComparison.OrdinalIgnoreCase));
                var rootRelativePath = file.PhysicalPath.Substring(_appRoot.Length);
                results = _razorHost.GenerateCode(_environment.ApplicationName, rootRelativePath, inputStream);
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
