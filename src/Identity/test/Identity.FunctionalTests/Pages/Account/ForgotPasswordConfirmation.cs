// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using AngleSharp.Dom.Html;

namespace Microsoft.AspNetCore.Identity.FunctionalTests.Account;

public class ForgotPasswordConfirmation : DefaultUIPage
{
    public ForgotPasswordConfirmation(HttpClient client, IHtmlDocument document, DefaultUIContext context) : base(client, document, context)
    {
    }
}
