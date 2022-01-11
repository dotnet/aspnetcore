// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

/// <summary>
/// A middleware that ensures web sites run in a consistent culture. Currently useful for tests that format dates,
/// times, or numbers. Will be more useful when we have localized resources.
/// </summary>
public class CultureReplacerMiddleware
{
    private readonly RequestDelegate _next;

    private CultureInfo _originalCulture;
    private CultureInfo _originalUICulture;

    public CultureReplacerMiddleware(RequestDelegate next, TestCulture culture)
    {
        Culture = new CultureInfo(culture.Culture);
        UICulture = new CultureInfo(culture.UICulture);
        _next = next;
    }

    public CultureInfo UICulture { get; }
    public CultureInfo Culture { get; }

    public async Task Invoke(HttpContext context)
    {
        // Use ReplaceCultureAttribute to avoid thread consistency checks in CultureReplacer. await doesn't
        // necessarily end on the original thread. For this case, problems arise when next middleware throws. Can
        // remove the thread consistency checks once culture is (at least for .NET 4.6) handled using
        // AsyncLocal<CultureInfo>.
        try
        {
            _originalCulture = CultureInfo.CurrentCulture;
            _originalUICulture = CultureInfo.CurrentUICulture;

            CultureInfo.CurrentCulture = Culture;
            CultureInfo.CurrentUICulture = UICulture;

            await _next(context);
        }
        finally
        {
            CultureInfo.CurrentCulture = _originalCulture;
            CultureInfo.CurrentUICulture = _originalUICulture;
        }
    }
}
