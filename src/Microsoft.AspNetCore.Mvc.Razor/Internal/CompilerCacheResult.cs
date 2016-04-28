// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.Razor.Compilation;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Mvc.Razor.Internal
{
    /// <summary>
    /// Result of <see cref="ICompilerCache"/>.
    /// </summary>
    public struct CompilerCacheResult
    {
        /// <summary>
        /// Initializes a new instance of <see cref="CompilerCacheResult"/> with the specified
        /// <see cref="Compilation.CompilationResult"/>.
        /// </summary>
        /// <param name="relativePath">Path of the view file relative to the application base.</param>
        /// <param name="compilationResult">The <see cref="Compilation.CompilationResult"/>.</param>
        public CompilerCacheResult(string relativePath, CompilationResult compilationResult)
            : this(relativePath, compilationResult, new IChangeToken[0])
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="CompilerCacheResult"/> with the specified
        /// <see cref="Compilation.CompilationResult"/>.
        /// </summary>
        /// <param name="relativePath">Path of the view file relative to the application base.</param>
        /// <param name="compilationResult">The <see cref="Compilation.CompilationResult"/>.</param>
        /// <param name="expirationTokens">One or more <see cref="IChangeToken"/> instances that indicate when
        /// this result has expired.</param>
        public CompilerCacheResult(string relativePath, CompilationResult compilationResult, IList<IChangeToken> expirationTokens)
        {
            if (expirationTokens == null)
            {
                throw new ArgumentNullException(nameof(expirationTokens));
            }

            ExpirationTokens = expirationTokens;
            var compiledType = compilationResult.CompiledType;

            var newExpression = Expression.New(compiledType);

            var pathProperty = compiledType.GetProperty(nameof(IRazorPage.Path));

            var propertyBindExpression = Expression.Bind(pathProperty, Expression.Constant(relativePath));
            var objectInitializeExpression = Expression.MemberInit(newExpression, propertyBindExpression);
            PageFactory =  Expression
                .Lambda<Func<IRazorPage>>(objectInitializeExpression)
                .Compile();
        }

        /// <summary>
        /// Initializes a new instance of <see cref="CompilerCacheResult"/> for a file that could not be
        /// found in the file system.
        /// </summary>
        /// <param name="expirationTokens">One or more <see cref="IChangeToken"/> instances that indicate when
        /// this result has expired.</param>
        public CompilerCacheResult(IList<IChangeToken> expirationTokens)
        {
            if (expirationTokens == null)
            {
                throw new ArgumentNullException(nameof(expirationTokens));
            }

            ExpirationTokens = expirationTokens;
            PageFactory = null;
        }

        /// <summary>
        /// <see cref="IChangeToken"/> instances that indicate when this result has expired.
        /// </summary>
        public IList<IChangeToken> ExpirationTokens { get; }

        /// <summary>
        /// Gets a value that determines if the view was successfully found and compiled.
        /// </summary>
        public bool Success => PageFactory != null;

        /// <summary>
        /// Gets a delegate that creates an instance of the <see cref="IRazorPage"/>.
        /// </summary>
        public Func<IRazorPage> PageFactory { get; }

    }
}