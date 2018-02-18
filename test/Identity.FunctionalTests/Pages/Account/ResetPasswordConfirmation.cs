// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net.Http;
using AngleSharp.Dom.Html;

namespace Microsoft.AspNetCore.Identity.FunctionalTests.Account
{
    public class ResetPasswordConfirmation : DefaultUIPage
    {
        public ResetPasswordConfirmation(HttpClient client, IHtmlDocument resetPasswordConfirmation, DefaultUIContext context)
            : base(client, resetPasswordConfirmation, context)
        {
        }
    }
}