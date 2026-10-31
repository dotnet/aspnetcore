// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation;

internal sealed class RuntimeCompilationFileProvider
{
#pragma warning disable ASPDEPR003 // Type or member is obsolete
    private readonly MvcRazorRuntimeCompilationOptions _options;
#pragma warning restore ASPDEPR003 // Type or member is obsolete
    private IFileProvider? _compositeFileProvider;

#pragma warning disable ASPDEPR003 // Type or member is obsolete
    public RuntimeCompilationFileProvider(IOptions<MvcRazorRuntimeCompilationOptions> options)
#pragma warning restore ASPDEPR003 // Type or member is obsolete
    {
        ArgumentNullException.ThrowIfNull(options);

        _options = options.Value;
    }

    public IFileProvider FileProvider
    {
        get
        {
            if (_compositeFileProvider == null)
            {
                _compositeFileProvider = GetCompositeFileProvider(_options);
            }

            return _compositeFileProvider;
        }
    }

#pragma warning disable ASPDEPR003 // Type or member is obsolete
    private static IFileProvider GetCompositeFileProvider(MvcRazorRuntimeCompilationOptions options)
#pragma warning restore ASPDEPR003 // Type or member is obsolete
    {
        var fileProviders = options.FileProviders;
        if (fileProviders.Count == 0)
        {
#pragma warning disable ASPDEPR003 // Type or member is obsolete
            var message = Resources.FormatFileProvidersAreRequired(
                typeof(MvcRazorRuntimeCompilationOptions).FullName,
                nameof(MvcRazorRuntimeCompilationOptions.FileProviders),
                typeof(IFileProvider).FullName);
#pragma warning restore ASPDEPR003 // Type or member is obsolete
            throw new InvalidOperationException(message);
        }
        else if (fileProviders.Count == 1)
        {
            return fileProviders[0];
        }

        return new CompositeFileProvider(fileProviders);
    }
}
