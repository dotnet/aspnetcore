// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc.Razor.Compilation;
using Microsoft.AspNetCore.Mvc.Razor.Internal;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;
using Microsoft.AspNetCore.Razor.Evolution;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Internal
{
    public class DefaultPageLoader : IPageLoader
    {
        private readonly IRazorCompilationService _razorCompilationService;
        private readonly RazorProject _project;

        public DefaultPageLoader(
            IRazorCompilationService razorCompilationService,
            RazorProject razorProject)
        {
            _razorCompilationService = razorCompilationService;
            _project = razorProject;
        }

        public Type Load(PageActionDescriptor actionDescriptor)
        {
            var item = _project.GetItem(actionDescriptor.RelativePath);
            if (!item.Exists)
            {
                throw new InvalidOperationException($"File {actionDescriptor.RelativePath} was not found.");
            }

            var projectItem = (DefaultRazorProjectItem)item;
            var compilationResult = _razorCompilationService.Compile(new RelativeFileInfo(projectItem.FileInfo, item.Path));
            compilationResult.EnsureSuccessful();

            return compilationResult.CompiledType;
        }
    }
}