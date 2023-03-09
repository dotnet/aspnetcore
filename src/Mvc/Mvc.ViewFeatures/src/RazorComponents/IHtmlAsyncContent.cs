// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Html;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures;

// For prerendered components, we can't use IHtmlComponent directly because it has no asynchrony and
// hence can't dispatch to the renderer's sync context.
internal interface IHtmlAsyncContent : IHtmlContent
{
    ValueTask WriteToAsync(TextWriter writer);
}
