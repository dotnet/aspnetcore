// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net.Http;
using AngleSharp.Dom.Html;

namespace Microsoft.AspNetCore.Identity.FunctionalTests.Account
{
    public class ForgotPasswordConfirmation : DefaultUIPage
    {
        public ForgotPasswordConfirmation(HttpClient client, IHtmlDocument document, DefaultUIContext context) : base(client, document, context)
        {
        }
    }
}
