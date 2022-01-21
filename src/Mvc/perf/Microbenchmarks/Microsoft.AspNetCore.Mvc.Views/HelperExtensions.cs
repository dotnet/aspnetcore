// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Razor;

public static class HelperExtensions
{
    public static Func<T1, IHtmlContent> Helper<T1>(
      this RazorPageBase page,
      Func<T1, Func<object, IHtmlContent>> helper
    ) => p1 => helper(p1)(null);

    public static Func<T1, T2, IHtmlContent> Helper<T1, T2>(
      this RazorPageBase page,
      Func<T1, T2, Func<object, IHtmlContent>> helper
    ) => (p1, p2) => helper(p1, p2)(null);

    public static Func<T1, T2, T3, IHtmlContent> Helper<T1, T2, T3>(
      this RazorPageBase page,
      Func<T1, T2, T3, Func<object, IHtmlContent>> helper
    ) => (p1, p2, p3) => helper(p1, p2, p3)(null);
}
