// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq.Expressions;

namespace Microsoft.AspNetCore.Mvc.Razor.Compilation;

/// <summary>
/// Represents a <see cref="IRazorPageFactoryProvider"/> that creates <see cref="RazorPage"/> instances
/// from razor files in the file system.
/// </summary>
internal sealed class DefaultRazorPageFactoryProvider : IRazorPageFactoryProvider
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
        ArgumentNullException.ThrowIfNull(relativePath);

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
            var pathProperty = viewType.GetProperty(nameof(IRazorPage.Path))!;

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
