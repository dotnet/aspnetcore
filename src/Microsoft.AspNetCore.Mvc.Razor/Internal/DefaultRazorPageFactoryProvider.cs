// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.Razor.Compilation;
using Microsoft.AspNetCore.Mvc.Razor.Extensions;
using Microsoft.AspNetCore.Razor.Evolution;

namespace Microsoft.AspNetCore.Mvc.Razor.Internal
{
    /// <summary>
    /// Represents a <see cref="IRazorPageFactoryProvider"/> that creates <see cref="RazorPage"/> instances
    /// from razor files in the file system.
    /// </summary>
    public class DefaultRazorPageFactoryProvider : IRazorPageFactoryProvider
    {
        private const string ViewImportsFileName = "_ViewImports.cshtml";
        private readonly RazorCompiler _razorCompiler;

        /// <summary>
        /// Initializes a new instance of <see cref="DefaultRazorPageFactoryProvider"/>.
        /// </summary>
        /// <param name="razorEngine">The <see cref="RazorEngine"/>.</param>
        /// <param name="razorProject">The <see cref="RazorProject" />.</param>
        /// <param name="compilationService">The <see cref="ICompilationService"/>.</param>
        /// <param name="compilerCacheProvider">The <see cref="ICompilerCacheProvider"/>.</param>
        public DefaultRazorPageFactoryProvider(
            RazorEngine razorEngine,
            RazorProject razorProject,
            ICompilationService compilationService,
            ICompilerCacheProvider compilerCacheProvider)
        {
            var templateEngine = new MvcRazorTemplateEngine(razorEngine, razorProject);
            templateEngine.Options.ImportsFileName = ViewImportsFileName;

            _razorCompiler = new RazorCompiler(compilationService, compilerCacheProvider, templateEngine);
        }

        /// <inheritdoc />
        public RazorPageFactoryResult CreateFactory(string relativePath)
        {
            if (relativePath == null)
            {
                throw new ArgumentNullException(nameof(relativePath));
            }

            if (relativePath.StartsWith("~/", StringComparison.Ordinal))
            {
                // For tilde slash paths, drop the leading ~ to make it work with the underlying IFileProvider.
                relativePath = relativePath.Substring(1);
            }

            var result = _razorCompiler.Compile(relativePath);
            if (result.Success)
            {
                var compiledType = result.CompiledType;

                var newExpression = Expression.New(compiledType);
                var pathProperty = compiledType.GetTypeInfo().GetProperty(nameof(IRazorPage.Path));

                // Generate: page.Path = relativePath;
                // Use the normalized path specified from the result.
                var propertyBindExpression = Expression.Bind(pathProperty, Expression.Constant(result.RelativePath));
                var objectInitializeExpression = Expression.MemberInit(newExpression, propertyBindExpression);
                var pageFactory = Expression
                    .Lambda<Func<IRazorPage>>(objectInitializeExpression)
                    .Compile();
                return new RazorPageFactoryResult(pageFactory, result.ExpirationTokens, result.IsPrecompiled);
            }
            else
            {
                return new RazorPageFactoryResult(result.ExpirationTokens);
            }
        }
    }
}