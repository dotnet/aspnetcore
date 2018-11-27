// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Microsoft.AspNetCore.Mvc.Razor.Compilation
{
    /// <summary>
    /// Represents a <see cref="IRazorPageFactoryProvider"/> that creates <see cref="RazorPage"/> instances
    /// from razor files in the file system.
    /// </summary>
    internal class DefaultRazorPageFactoryProvider : IRazorPageFactoryProvider
    {
        private readonly IViewCompilerProvider _viewCompilerProvider;

        /// <summary>
        /// Initializes a new instance of <see cref="DefaultRazorPageFactoryProvider"/>.
        /// </summary>
        /// <param name="viewCompilerProvider">The <see cref="IViewCompilerProvider"/>.</param>
        public DefaultRazorPageFactoryProvider(IViewCompilerProvider viewCompilerProvider)
        {
            _viewCompilerProvider = viewCompilerProvider;
        }

        private IViewCompiler Compiler => _viewCompilerProvider.GetCompiler();

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

            var compileTask = Compiler.CompileAsync(relativePath);
            var viewDescriptor = compileTask.GetAwaiter().GetResult();

            var viewType = viewDescriptor.Type;
            if (viewType != null)
            {
                var newExpression = Expression.New(viewType);
                var pathProperty = viewType.GetTypeInfo().GetProperty(nameof(IRazorPage.Path));

                // Generate: page.Path = relativePath;
                // Use the normalized path specified from the result.
                var propertyBindExpression = Expression.Bind(pathProperty, Expression.Constant(viewDescriptor.RelativePath));
                var objectInitializeExpression = Expression.MemberInit(newExpression, propertyBindExpression);
                var pageFactory = Expression
                    .Lambda<Func<IRazorPage>>(objectInitializeExpression)
                    .Compile();
                return new RazorPageFactoryResult(viewDescriptor, pageFactory);
            }
            else
            {
                return new RazorPageFactoryResult(viewDescriptor, razorPageFactory: null);
            }
        }
    }
}