// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using AngleSharp.Dom.Html;

namespace Microsoft.AspNetCore.Identity.FunctionalTests.Account.Manage;

internal class ShowRecoveryCodes : DefaultUIPage
{
    private readonly IEnumerable<IHtmlElement> _recoveryCodeElements;

    public ShowRecoveryCodes(HttpClient client, IHtmlDocument showRecoveryCodes, DefaultUIContext context)
        : base(client, showRecoveryCodes, context)
    {
        _recoveryCodeElements = HtmlAssert.HasElements(".recovery-code", showRecoveryCodes);
        Context.RecoveryCodes = Codes.ToArray();
    }

    public IEnumerable<string> Codes => _recoveryCodeElements.Select(rc => rc.TextContent);
}
