// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.QuickGrid;

internal sealed class InternalQuickGridLocalizer
{
    private readonly IQuickGridLocalizationInterceptor _interceptor;

    public InternalQuickGridLocalizer(IQuickGridLocalizationInterceptor interceptor)
    {
        _interceptor = interceptor ?? throw new ArgumentNullException(nameof(interceptor));
    }

    public Microsoft.Extensions.Localization.LocalizedString this[string key]
        => _interceptor.Handle(key, Array.Empty<object?>());

    public Microsoft.Extensions.Localization.LocalizedString this[string key, params object?[]? arguments]
        => _interceptor.Handle(key, arguments ?? Array.Empty<object?>());

    public string GetString(string key)
        => this[key].Value;

    public string GetString(string key, params object?[]? arguments)
        => this[key, arguments].Value;
}
