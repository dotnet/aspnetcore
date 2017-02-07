// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Razor.Compilation;
using Microsoft.AspNetCore.Mvc.Razor.Internal;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;
using Microsoft.AspNetCore.Razor.Evolution;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Internal
{
    public class DefaultPageLoader : IPageLoader
    {
        private readonly RazorCompilationService _razorCompilationService;
        private readonly ICompilationService _compilationService;
        private readonly RazorProject _project;
        private readonly ILogger _logger;

        public DefaultPageLoader(
            IRazorCompilationService razorCompilationService,
            ICompilationService compilationService,
            RazorProject razorProject,
            ILogger<DefaultPageLoader> logger)
        {
            _razorCompilationService = (RazorCompilationService)razorCompilationService;
            _compilationService = compilationService;
            _project = razorProject;
            _logger = logger;
        }

        public Type Load(PageActionDescriptor actionDescriptor)
        {
            var item = _project.GetItem(actionDescriptor.RelativePath);
            if (!item.Exists)
            {
                throw new InvalidOperationException($"File {actionDescriptor.RelativePath} was not found.");
            }

            RazorCodeDocument codeDocument;
            RazorCSharpDocument cSharpDocument;
            _logger.RazorFileToCodeCompilationStart(item.Path);

            var startTimestamp = _logger.IsEnabled(LogLevel.Debug) ? Stopwatch.GetTimestamp() : 0;

            codeDocument = CreateCodeDocument(item);
            cSharpDocument = _razorCompilationService.ProcessCodeDocument(codeDocument);

            _logger.RazorFileToCodeCompilationEnd(item.Path, startTimestamp);

            CompilationResult compilationResult;
            if (cSharpDocument.Diagnostics.Count > 0)
            {
                compilationResult = _razorCompilationService.GetCompilationFailedResult(item.Path, cSharpDocument.Diagnostics);
            }
            else
            {
                compilationResult = _compilationService.Compile(codeDocument, cSharpDocument);
            }

            compilationResult.EnsureSuccessful();
            return compilationResult.CompiledType;
        }

        private RazorCodeDocument CreateCodeDocument(RazorProjectItem item)
        {
            var absolutePath = GetItemPath(item);

            RazorSourceDocument source;
            using (var inputStream = item.Read())
            {
                source = RazorSourceDocument.ReadFrom(inputStream, absolutePath);
            }

            var imports = new List<RazorSourceDocument>()
            {
                _razorCompilationService.GlobalImports,
            };

            var pageImports = _project.FindHierarchicalItems(item.Path, "_PageImports.cshtml");
            foreach (var pageImport in pageImports.Reverse())
            {
                if (pageImport.Exists)
                {
                    using (var stream = pageImport.Read())
                    {
                        imports.Add(RazorSourceDocument.ReadFrom(stream, GetItemPath(item)));
                    }
                }
            }

            return RazorCodeDocument.Create(source, imports);
        }

        private static string GetItemPath(RazorProjectItem item)
        {
            var absolutePath = item.Path;
            if (item.Exists && string.IsNullOrEmpty(item.PhysicalPath))
            {
                absolutePath = item.PhysicalPath;
            }

            return absolutePath;
        }
    }
}